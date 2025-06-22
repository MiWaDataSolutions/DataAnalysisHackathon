from google.adk.tools.tool_context import ToolContext
from google.adk.agents.invocation_context import InvocationContext
import psycopg2

def conn_str_to_url(conn_str):
    # Replace semicolons with spaces, then split into key=value pairs
    parts = conn_str.replace(';', ' ').split()
    params = dict(part.split('=') for part in parts)
    return f"postgresql://{params['username']}:{params['password']}@{params['host']}:{params['port']}/{params['database']}"


def get_database_data(table: str, offset: int, tool_context: InvocationContext) -> str:
    """Returns a list of delimited strings of raw data from the requested table and the following will always be true:
        1. The list will always have atleast one item
        2. The first item in the returned list will always be the header
        3. The rows are delimited by a double pipe character ("||").

    Args:
        table (str): The table to get data from
        offset (int): A value that is increased by the limit of 50 with each call
        tool_context (ToolContext): The tool context.

    Returns:
        list[str]: a header with data from the requested table
    """
    conn_str = tool_context.session.state.get("user:conn_string")
    schema = tool_context.session.state.get("schema")
    print(f'get_database_data_count table: {table} offset: {offset}')

    joined_rows: list[str] = []

    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            schema_script = f"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = '{table}';"
            cur.execute(schema_script)
            rows = cur.fetchall()
            temp = []
            for row in rows:
                temp.append(str(row).replace("('", "").replace("',)", ""))
            joined_rows = ["||".join(temp)]


    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            cur.execute(f"SELECT * FROM {schema}.{table} LIMIT 10 OFFSET {offset}")
            rows = cur.fetchall()
            for joinedValue in ["||".join(str(value) for value in row) for row in rows]:
                joined_rows.append(joinedValue)
            return "[|]".join(joined_rows)
        
def get_database_schema(table: str, tool_context: ToolContext) -> str:
    """Returns a delimited string of column names from the requested table and the following will always be true:
        1. The row will delimited by a double pipe character ("||").

    Args:
        table (str): The table to get schema from
        tool_context (ToolContext): The tool context.

    Returns:
       str: a delimited string representing a header of the requested table
    """
    conn_str = tool_context.state.get("user:conn_string")
    schema = tool_context.state.get("schema")
    print(f'table: {table}')
    
    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            schema_script = f"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = '{table}';"
            cur.execute(schema_script)
            rows = cur.fetchall()
            return "||".join(str(value) for value in rows[0])


def execute_script(script: str, tool_context: ToolContext) -> tuple[bool, str]:
    """Returns whether the script was sucessfully executed or not and if not, an excpetion of why it failed.
    
    Args:
        script (str): The script that needs to be executed
        tool_context (ToolContext): The tool context

    Returns:
        tuple[bool, str]: A True/False value with a reason why it failed if value is False    
    """

    conn_str = tool_context.state.get("user:conn_string")
    try:
        with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
            with conn.cursor() as cur:
                cur.execute(script)
                conn.commit()
                print('commited')
        return True, ""
    except Exception as e:
        print(f'task failed. error: {e}')
        return False, f"{e}"
    
def self_execute_script(script: str, tool_context: InvocationContext) -> tuple[bool, str]:
    """Returns whether the script was sucessfully executed or not and if not, an excpetion of why it failed.
    
    Args:
        script (str): The script that needs to be executed
        tool_context (ToolContext): The tool context

    Returns:
        tuple[bool, str]: A True/False value with a reason why it failed if value is False    
    """

    conn_str = tool_context.session.state.get("user:conn_string")
    try:
        with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
            with conn.cursor() as cur:
                cur.execute(script)
                conn.commit()
                print('commited')
        return True, ""
    except Exception as e:
        print(f'task failed. error: {e}')
        return False, f"{e}"


def get_database_data_subset(table: str, tool_context: ToolContext) -> list[str]:
    """A list of delimited strings of raw data from the requested table and the following will always be true:
        1. The list will always have atleast one item
        2. The first item in the returned list will always be the header
        3. The at most 10 rows are delimited by a double pipe character ("||").

    Args:
        table (str): The table to get data from
        tool_context (ToolContext): The tool context.

    Returns:
        list[str]: a header with data from the requested table
    """
    conn_str = tool_context.state.get("user:conn_string")
    schema = tool_context.state.get("schema")
    print(f'get_database_data_subset table: {table}')

    joined_rows: list[str] = []

    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            schema_script = f"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = '{table}';"
            cur.execute(schema_script)
            rows = cur.fetchall()
            joined_rows = ["||".join(str(value) for value in row) for row in rows]

    schemaTable = ""
    if table.__contains__(schema):
        schemaTable = table
    else:
        schemaTable = f"{schema}.{table}"

    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            cur.execute(f"SELECT * FROM {schemaTable} limit 10")
            rows = cur.fetchall()
            for joinedValue in ["||".join(str(value) for value in row) for row in rows]:
                joined_rows.append(joinedValue)
            return joined_rows

def get_database_data_count(table: str, tool_context: InvocationContext) -> int:
    """Returns the amount of rows to process in the table

    Args:
        table (str): The table to get data from
        tool_context (ToolContext): The tool context.

    Returns:
        int: The number of rows in the requested table
    """
    conn_str = tool_context.session.state.get("user:conn_string")
    schema = tool_context.session.state.get("schema")
    print(f'get_database_data_count table: {table}')


    with psycopg2.connect(conn_str_to_url(str(conn_str).lower())) as conn:
        with conn.cursor() as cur:
            cur.execute(f"SELECT COUNT(*) FROM {schema}.{table}")
            result = cur.fetchone()
            return result[0] if result is not None else 0
            
