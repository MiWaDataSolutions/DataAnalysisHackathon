import asyncio
from math import ceil
from typing import AsyncGenerator
from google.adk.agents import BaseAgent, LlmAgent
from google.adk.agents.invocation_context import InvocationContext
from google.adk.events.event import Event
from google.genai.types import Content, Part
from .data_pipeline_agent import DataPipelineAgent
from pydantic import BaseModel, Field, PrivateAttr
import logging
logger = logging.getLogger(__name__)

class DataRoutingAgent(BaseAgent):
    
    # --- Field Declarations for Pydantic ---
    # Declare the agents passed during initialization as class attributes with type hints
    data_naming_agent: LlmAgent
    data_processing_agent: DataPipelineAgent
    _semaphore: asyncio.Semaphore = PrivateAttr()

    model_config = {"arbitrary_types_allowed": True}

    def __init__(
        self,
        name: str,
        description: str,
        data_naming_agent: LlmAgent,
        data_processing_agent: DataPipelineAgent
    ):
        super().__init__(
            name=name,
            description=description,
            data_naming_agent=data_naming_agent,
            data_processing_agent=data_processing_agent,
            # sub_agents=sub_agents_list
        )

    async def _run_async_impl(self, ctx: InvocationContext) -> AsyncGenerator[Event, None]:
        user_message = ""
        if ctx.user_content and ctx.user_content.parts:
            user_message = str(ctx.user_content.parts[0].text).lower()

        if "name" in user_message:
            logger.info(f"[{self.name}] Routing to data_naming_agent")
            async for event in self.data_naming_agent.run_async(ctx):
                yield event
        elif "analyze" in user_message:
            logger.info(f"[{self.name}] Routing to data_processing_agent")
            async for event in self.data_processing_agent.run_async(ctx):
                yield event
        else:
            # Default/fallback: yield an error or help message
            logger.warning(f"[{self.name}] Could not route request: {user_message}")
            custom_message = "❌ Routing Failed"
            yield Event(
                content=Content(parts=[Part.from_text(text=custom_message)]),
                author=self.name
            )

