import asyncio
from math import ceil
from typing import AsyncGenerator
from google.adk.agents import BaseAgent, LlmAgent
from google.adk.agents.invocation_context import InvocationContext
from google.adk.events.event import Event
from google.genai.types import Content, Part
from pydantic import BaseModel, Field, PrivateAttr
from ..tools import get_database_data_count, get_database_data, self_execute_script, conn_str_to_url
import logging
import re
import psycopg2
logger = logging.getLogger(__name__)

class DataPipelineAgent(BaseAgent):
    
    # --- Field Declarations for Pydantic ---
    # Declare the agents passed during initialization as class attributes with type hints
    data_type_agent: LlmAgent
    data_cleaning_agent: LlmAgent
    data_view_agent: LlmAgent
    _semaphore: asyncio.Semaphore = PrivateAttr()

    model_config = {"arbitrary_types_allowed": True}

    def __init__(
        self,
        name: str,
        description: str,
        data_type_agent: LlmAgent,
        data_cleaning_agent: LlmAgent,
        data_view_agent: LlmAgent
    ):
        sub_agents_list: list[BaseAgent] = [
            data_type_agent,
            data_cleaning_agent,
            data_view_agent
        ]

        super().__init__(
            name=name,
            description=description,
            data_type_agent=data_type_agent,
            data_cleaning_agent=data_cleaning_agent,
            data_view_agent=data_view_agent,
            # sub_agents=sub_agents_list
        )
    
        self._semaphore = asyncio.Semaphore(10)

    async def _run_async_impl(self, ctx: InvocationContext) -> AsyncGenerator[Event, None]:
        logger.warning(f"[{self.name}] Starting story generation workflow.")
        
        logger.warning(f"[{self.name}] Running Data Pipeline...")
        logger.warning(f"[{self.name}] Running Data Type Agent...")
        bronze_data = get_database_data("bronze", 0, ctx)
        ctx.session.state['bronze_data'] = bronze_data
        print(f"bronze_data", bronze_data)

        while self.check_silver_table_exists(ctx) == 0:
            try:
                logger.warning(f"[{self.name}] Silver table doesnt exist. Trying to create it...")
                async for event in self.data_type_agent.run_async(ctx):
                    logger.info(f"[{self.name}] Event from {self.data_type_agent.name}: {event.model_dump_json(indent=2, exclude_none=True)}")
                    if not event.is_final_response():
                        yield event
                    else:
                        if event.content and event.content.parts:
                            logger.warning(f"[{self.name}] final response parts = {event.content.parts}")
                        break        
            except Exception as e:
                logger.warning(f"[{self.name}] Exception Occured. Exception is: {e}. Trying again...")

        logger.warning(f"[{self.name}] Running Data Type Agent Done")
        # Add retry logic etc later

        row_count = get_database_data_count('bronze', ctx)
        batches = ceil(row_count / 10)
        scripts = []
        # Prepare tasks for all batchessemaphore = asyncio.Semaphore(2)  # Only 2 concurrent batches
        tasks = [self.limited_process_batch(batch, ctx) for batch in range(batches)]
        scripts = await asyncio.gather(*tasks)

        print(f'Amount of scripts generated: {len(scripts)}; Amount of batches: {batches}')
        for script in scripts:
            self_execute_script(self.clean_sql_for_postgres(script), ctx)

        logger.warning(f"[{self.name}] Running Data Cleaning Agent Done")
        cleaned_batch_data = get_database_data("silver", 0, ctx)
        ctx.session.state['cleaned_batch_data'] = cleaned_batch_data

        # Optionally clear unrelated output keys
        ctx.session.state.pop('silver_table_created', None)
        ctx.session.state.pop('silver_table_bas', None)

        while self.check_views_exists(ctx) == 0:
            logger.warning(f"[{self.name}] Running Data View Agent...")
            async for event in self.data_view_agent.run_async(ctx):
                logger.info(f"[{self.name}] Event from {self.data_view_agent.name}: {event.model_dump_json(indent=2, exclude_none=True)}")
                if not event.is_final_response():
                    yield event
                else:
                    ctx.session.state.pop('silver_table_bas_bas', None)
                    if event.content and event.content.parts:
                        logger.warning(f"[{self.name}] final response parts = {event.content.parts}")
                    break

        logger.warning(f"[{self.name}] Running Data View Agent Done")
        custom_message = "✅ Data pipeline completed successfully! All steps finished."
        yield Event(
            content=Content(parts=[Part.from_text(text=custom_message)]),
            author=self.name
        )
        return

    def check_silver_table_exists(self, ctx: InvocationContext) -> int:
        schema = ctx.session.state.get("schema")
    
        with psycopg2.connect(conn_str_to_url(str(ctx.session.state["user:conn_string"]).lower())) as conn:
            with conn.cursor() as cur:
                cur.execute(f"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = 'silver'")
                result = cur.fetchone()
                return result[0] if result is not None else 0
    
    def check_views_exists(self, ctx: InvocationContext) -> int:
        schema = ctx.session.state.get("schema")
    
        with psycopg2.connect(conn_str_to_url(str(ctx.session.state["user:conn_string"]).lower())) as conn:
            with conn.cursor() as cur:
                cur.execute(f"SELECT table_schema, table_name FROM information_schema.views WHERE table_schema = '{schema}';")
                result = cur.fetchone()
                return result[0] if result is not None else 0

    async def limited_process_batch(self, batch, ctx):
        async with self._semaphore:
            return await self.process_batch(batch, ctx)
        
    async def process_batch(self, batch: int, ctx: InvocationContext) -> str:
        logger.warning(f"[{self.name}] Getting Data for batch {batch}...")
        data = get_database_data("bronze", batch * 10, ctx)
        logger.warning(f"[{self.name}] Batch {batch}: {len(data.split('[|]'))-1} data rows (excluding header)")
        ctx.session.state['batch_data'] = data
        logger.warning(f"[{self.name}] Running Data Cleaning Agent for batch {batch}...")
        script = ""
        async for event in self.data_cleaning_agent.run_async(ctx):
            logger.info(f"[{self.name}] Event from {self.data_cleaning_agent.name}: {event.model_dump_json(indent=2, exclude_none=True)}")
            if event.is_final_response() and event.content and event.content.parts:
                script = event.content.parts[0].text or ""
                break
        return script
    
    def clean_sql_for_postgres(self, selection: str) -> str:
        """
        Cleans up a SQL selection by removing Markdown code block markers and
        double quotes from schema, table, and column names.
        """
        # Remove Markdown code block markers
        lines = selection.strip().splitlines()
        lines = [line for line in lines if not line.strip().lower().startswith('```')]
        sql = '\n'.join(lines)

        # Remove quotes around schema.table in INSERT INTO
        sql = re.sub(r'INSERT INTO\s+"([^"]+)"\."([^"]+)"', r'INSERT INTO \1.\2', sql)
        # Remove quotes from column names in the first parenthesis group after INSERT INTO
        sql = re.sub(
            r'(\([^)]+\))',
            lambda m: '(' + ', '.join(col.strip().replace('"', '') for col in m.group(1)[1:-1].split(',')) + ')',
            sql,
            count=1
        )
        return sql

