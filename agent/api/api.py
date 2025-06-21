from fastapi import FastAPI, Request
from data_analyst_agent.agent import runner_root, APP_NAME
from google.genai import types
from dotenv import load_dotenv
import os

load_dotenv()

app = FastAPI()

@app.post("/api/agent/start-session")
async def start_session(request: Request):
    print("Got request")
    data = await request.json()
    user_id = data.get("userId")
    data_session_id = data.get("dataSessionId")
    user_conn_string = data.get("userConnString")
    data_session_schema = data.get("dataSessionSchema")

    session = await runner_root.session_service.get_session(app_name=APP_NAME, user_id=str(user_id), session_id=data_session_id)

    if session:
        print("Session already exists. Not creating new one")
        response = {"sessionId": session.id}
        return response
    else:
        print("Session not created. Creating now")
        state = {"user:conn_string": user_conn_string, "schema": data_session_schema}
        created_session = await runner_root.session_service.create_session(app_name=APP_NAME, user_id=str(user_id), session_id=data_session_id, state=state)
        print("Session successfully created")
        response = {"sessionId": created_session.id}
        return response
    
@app.post("/api/agent/get-session-name")
async def get_session_name(request: Request):
    data = await request.json()
    user_id = data.get("userId")
    data_session_id = data.get("dataSessionId")

    content = types.Content(role="user", parts=[types.Part(text="Generate a Data Session Name")]) 

    final_response_text = "Agent did not produce a final response"

    async for event in runner_root.run_async(user_id=user_id, session_id=data_session_id, new_message=content):
        if event.is_final_response():
            if event.content and event.content.parts:
                print('event.content.parts', event.content.parts)
                final_response_text = event.content.parts[0].text
            elif event.actions and event.actions.escalate:
                final_response_text = f"Agent escalated: {event.error_message or 'No specific message.'}"
            break 
    
    print('final_response_text', final_response_text)
    response = {"dataSessionName": final_response_text}
    print('response', response)

    return response

@app.post("/api/agent/start-data-session-processing")
async def start_data_session_processing(request: Request):
    data = await request.json()
    user_id = data.get("userId")
    data_session_id = data.get("dataSessionId")

    content = types.Content(role="user", parts=[types.Part(text="Analyze the data in the Data Session")]) 

    final_response_text = "Agent did not produce a final response"

    async for event in runner_root.run_async(user_id=user_id, session_id=data_session_id, new_message=content):
        if event.is_final_response():
            if event.content and event.content.parts:
                print('event.content.parts', event.content.parts)
                final_response_text = event.content.parts[0].text
            elif event.actions and event.actions.escalate:
                final_response_text = f"Agent escalated: {event.error_message or 'No specific message.'}"
            break 
    
    print('final_response_text', final_response_text)
    response = {"dataSessionName": final_response_text}
    print('response', response)

    return response
