from google.adk.agents import Agent
from .shared import constants
from .shared import tools

session_naming_agent = Agent(
    name="naming_agent",
    model=constants.MODEL_GEMINI_2_0_FLASH,
    description="A specialist agent that generates unique, descriptive session names for data processing jobs.",
    instruction="You are a specialist Naming Agent. Your only function is to create a unique and descriptive session name for a data processing job."
                "When invoked by the coordinator, use your 'get_database_data' tool to get the data from the bronze table called 'bronze'."
                "Analyze the data that is returned by the 'get_database_data' tool and generate a name that is max 5 words long that aptly descibes the data set."
                "Return only the generated name as your output. Do not perform any other actions.",
    tools=[tools.get_database_data]
)