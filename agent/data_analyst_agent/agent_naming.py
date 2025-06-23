from google.adk.agents import Agent
from google.adk.models.lite_llm import LiteLlm
from .shared import constants
from .shared import tools

session_naming_agent = Agent(
    name="session_naming_agent",
    model=constants.MODEL_GEMINI_2_0_FLASH,
    description="Generates unique and descriptive session names for data pipeline runs. Does NOT analyze or process data.",
    instruction="You are a specialist Naming Agent. Your only function is to create a unique and descriptive session name for a data processing job based on the content of the 'bronze' table."
                "You can get the data by using the tool 'get_database_data_subset'"
                "The name must be a maximum of 5 words."
                "Only provide the name as a response."
                "Do not analyze or process data.",    
    tools=[tools.get_database_data_subset]
)