from google.adk.tools.tool_context import ToolContext
import psycopg2

def conn_str_to_url(conn_str):
    # Replace semicolons with spaces, then split into key=value pairs
    parts = conn_str.replace(';', ' ').split()
    params = dict(part.split('=') for part in parts)
    return f"postgresql://{params['username']}:{params['password']}@{params['host']}:{params['port']}/{params['database']}"


def get_database_data(table: str, tool_context: ToolContext) -> list[str]:
    """Returns a list of delimited strings of raw data from the requested table and the following will always be true:
        1. The list will always have atleast one item
        2. The first item in the returned list will always be the header
        3. The rows are delimited by a double pipe character ("||").

    Args:
        table (str): The table to get data from
        tool_context (ToolContext): The tool context.

    Returns:
        list[str]: a header with data from the requested table
    """
    conn_str = tool_context.state.get("user:conn_string")
    schema = tool_context.state.get("schema")
    conn = psycopg2.connect(conn_str_to_url(str(conn_str).lower()))

    cur = conn.cursor()

    cur.execute(f"SELECT * FROM {schema}.{table}")

    rows = cur.fetchall()
    joined_rows = ["||".join(str(value) for value in row) for row in rows]

    return joined_rows

