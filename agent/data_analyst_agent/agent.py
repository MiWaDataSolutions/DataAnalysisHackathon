from google.adk.agents import Agent
from google.adk.sessions import DatabaseSessionService
from google.adk.runners import Runner
from .agent_naming import session_naming_agent
from .shared import constants
from dotenv import load_dotenv
import os

load_dotenv()

db_url = "postgresql://postgres:postgres@host.docker.internal:5432/data_analyst_session_storage"
session_service = DatabaseSessionService(db_url=db_url)

APP_NAME = "data_analyst_app"

main_coordinator_agent = Agent(
    name="main_coordinator_agent",
    model=constants.MODEL_GEMINI_2_0_FLASH,
    description="The main coordinator agent. Manages the entire data pipeline workflow from Bronze to Gold by delegating tasks to specialist sub-agents.",
    instruction="You are the main Data Pipeline Coordinator. Your responsibility is to manage the end-to-end data processing workflow. You will be given a source table name from the Bronze layer."
                "1.  When requested for a name for the Data Session, delegate to the 'session_naming_agent' to get a unique Data Session name."
                "Do not perform any data operations yourself. Your sole purpose is orchestration.",
    sub_agents=[session_naming_agent]
)

runner_root = Runner(
    agent=main_coordinator_agent,
    app_name=APP_NAME,
    session_service=session_service # <<< Use the service from Step 4/5
)

