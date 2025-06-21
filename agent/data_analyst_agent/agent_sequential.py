from google.adk.agents import SequentialAgent
from .shared import constants
from .shared import tools
from .agent_data_type import data_type_agent
from .agent_data_cleaning import data_cleaning_agent
from .shared.custom_agents.data_pipeline_agent import DataPipelineAgent
from .agent_data_view import data_view_agent

data_pipeline_agent = DataPipelineAgent(
    name="data_pipeline_agent",
    description="Run as created",
    data_type_agent=data_type_agent,
    data_cleaning_agent=data_cleaning_agent,
    data_view_agent=data_view_agent
)