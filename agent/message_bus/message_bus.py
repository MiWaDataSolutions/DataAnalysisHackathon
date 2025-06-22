import os, sys, json, aio_pika, asyncio
from dotenv import load_dotenv
from data_analyst_agent.agent import runner_root, APP_NAME
from google.genai import types

load_dotenv()

async def main():
    connection = await aio_pika.connect_robust(host=os.environ.get("RABBITMQ_HOST") or "amqp://guest:guest@localhost/")
    channel = await connection.channel()
    prefix = os.environ.get("RABBITMQ_PREFIX")

    # Request Queues
    data_session_generate_name_queue_name = f"{prefix}-data_session_generate_name"
    data_session_start_session = f"{prefix}-data_session_start_session"
    data_session_process_data = f"{prefix}-data_session_process_data"

    await channel.declare_queue(data_session_generate_name_queue_name)
    await channel.declare_queue(data_session_start_session)
    await channel.declare_queue(data_session_process_data)

    async def data_session_start_session_callback(body):
        print(f"Received Start Session Request")
        async with body.process():
            data = json.loads(body.body)
            print("Received data:", data)
            user_id = data.get("userId")
            data_session_id = data.get("dataSessionId")
            user_conn_string = data.get("userConnString")
            data_session_schema = data.get("dataSessionSchema")

            session = await runner_root.session_service.get_session(app_name=APP_NAME, user_id=str(user_id), session_id=data_session_id)

            if session:
                print("Session already exists. Not creating new one")
                response = {"sessionId": session.id}
                await channel.declare_queue(f"{data_session_start_session}_response")
                message = aio_pika.Message(body=json.dumps(data).encode())
                await channel.default_exchange.publish(routing_key=f"{data_session_start_session}_response", message=message)
            else:
                print("Session not created. Creating now")
                state = {"user:conn_string": user_conn_string, "schema": data_session_schema}
                created_session = await runner_root.session_service.create_session(app_name=APP_NAME, user_id=str(user_id), session_id=data_session_id, state=state)
                print("Session successfully created")
                await channel.declare_queue(f"{data_session_start_session}_response")
                message = aio_pika.Message(body=json.dumps(data).encode())
                await channel.default_exchange.publish(routing_key=f"{data_session_start_session}_response", message=message)
            pass

    async def data_session_generate_name_callback(body):
        print(f"Received Generate Name Request")
        async with body.process():
            data = json.loads(body.body)
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
            
            print('Generate Name Request: final_response_text', final_response_text)
            response = {"dataSessionName": final_response_text, 'dataSessionId': data_session_id, 'userId': user_id}
            print('Generate Name Request: response', response)
            await channel.declare_queue(f"{data_session_generate_name_queue_name}_response")
            message = aio_pika.Message(body=json.dumps(response).encode())
            await channel.default_exchange.publish(routing_key=f"{data_session_generate_name_queue_name}_response", message=message)
            pass
    
    async def data_session_process_data_callback(body):
        print(f"Received Process Data Request")
        async with body.process():
            data = json.loads(body.body)
            user_id = data.get("userId")
            data_session_id = data.get("dataSessionId")

            content = types.Content(role="user", parts=[types.Part(text="Analyze the data in the Data Session")]) 

            final_response_text = "Agent did not produce a final response"
            while final_response_text != "✅ Data pipeline completed successfully! All steps finished.":
                async for event in runner_root.run_async(user_id=user_id, session_id=data_session_id, new_message=content):
                    if event.is_final_response():
                        if event.content and event.content.parts:
                            print('event.content.parts', event.content.parts)
                            final_response_text = event.content.parts[0].text
                        elif event.actions and event.actions.escalate:
                            final_response_text = f"Agent escalated: {event.error_message or 'No specific message.'}"
                        break 
            
            print('final_response_text', final_response_text)
            response = {"processed": True, "userId": user_id, "dataSessionId": data_session_id}
            print('response', response)

            await channel.declare_queue(f"{data_session_process_data}_response")

            message = aio_pika.Message(body=json.dumps(response).encode())
            await channel.default_exchange.publish(routing_key=f"{data_session_process_data}_response", message=message)
            pass


    await (await channel.declare_queue(data_session_generate_name_queue_name)).consume(data_session_generate_name_callback)
    await (await channel.declare_queue(data_session_start_session)).consume(data_session_start_session_callback)
    await (await channel.declare_queue(data_session_process_data)).consume(data_session_process_data_callback)

    print(' [*] Waiting for messages. To exit press CTRL+C')
    await asyncio.Future()
    

if __name__ == '__main__':
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print('Interrupted')
        try:
            sys.exit(0)
        except SystemExit:
            os._exit(0)