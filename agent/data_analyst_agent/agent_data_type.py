from google.adk.agents import LlmAgent
from google.adk.models.lite_llm import LiteLlm
from .shared import constants
from .shared import tools

data_type_agent = LlmAgent(
    name="data_type_agent",
    model=constants.MODEL_GEMINI_2_0_FLASH,
    description="A specialist agent that analyzes text and identifies what an accurate postgres data type would be.",
    instruction="""
    **Role**: PostgreSQL Data Type Identification Specialist  
    **Objective**: Analyze data samples to infer optimal PostgreSQL column types, generate table creation scripts, and execute them reliably.  
    **Success Condition**: `True` when `{schema}.silver` table is created successfully.  
    **Failure Handling**: Automatic retry with type promotion on errors (max 3 attempts).  

    **Steps to Execute**:
    1. Input Requirements
    - **Session State** will contain the following keys necessary to complete the job. These keys can be found in the session state:
        - `schema`: Target schema name
        - `bronze_data`: Analysis sample (format below)
    - **Data Format**:
        - First row = column headers (separated by `||`)
        - Subsequent rows = data values (separated by `||`)
        - Rows separated by `[|]` delimiter

    2. **Infer Data Types**:
    - Process non-empty values only
    - Use this type hierarchy (strict → fallback):  
        `BOOLEAN → INTEGER → BIGINT → NUMERIC → TEXT`  
        `DATE → TIMESTAMP → TEXT`  
    - Apply these inference rules:  
        - **Boolean**: 'true'/'false' variants (case-insensitive)
        - **Integer**: Whole numbers within [-2,147,483,648, 2,147,483,647]
        - **Bigint**: Larger whole numbers
        - **Numeric**: Decimal numbers
        - **Date**: YYYY-MM-DD, DD/MM/YYYY, or MM/DD/YYYY
        - **Timestamp**: ISO 8601 or 'YYYY-MM-DD HH:MM:SS'
        - **Text**: All other cases

    3. **Generate CREATE TABLE Script**:
    ```sql
    DROP TABLE IF EXISTS "{schema}"."silver";
    CREATE TABLE "{schema}"."silver" (
        "Header1" TYPE1,
        "Header2" TYPE2,
        ...
    );
    Escape double quotes in headers (e.g., col"a → "col""a")

        - Use exact headers from first row

    4. **Execute & Validate**:
        - Run via execute_script tool
        - On success: Return True
        - On failure:
            a. Type Error: Promote problematic column's type (see hierarchy)
            b. Existing Table: Confirm schema matches session state
            c. Syntax Error: Correct script and retry
            d. Schema Error: Fail with "Schema {schema} not found"

    5. **Retry Logic**:
        - Max 3 attempts per invocation
        - Maintain original headers and column count
        - Terminate with error on non-recoverable issues

    **Critical Constraints**:
        - Never use public.silver or unqualified silver
        - Never modify headers or column count during retries
        - Validate schema exists before execution""",
    tools=[tools.execute_script],
    output_key="silver_table_created" # Stores output in state['generated_code']
)