# Microservice-architecture
An example of microservice architecture in docker containers

## Scenarios

+ A key/value is sent as a roundtrip from the client, is transported through a range of services, persisted, and returned to the client.
+ A batch(customable size) is sent, and persisted to a store and returned with the status of the transaction. The batch is then read from the store.

## Architecture


## Flow
 - **Roundtrip**
    1. Send a key/value from client via HTTP
    2. Receive key/value in roundtrip-service and publish to MQTT broker (queue *event*)
    3. Consume key/value in redis-service from queue *event*
    4. Persist key/value in Redis from redis-service
    5. Publish key/value to MQTT broker (queue *forward_roundtrip*)
    6. Consume key/value in websocket-service from queue *forward_roundtrip*
    7. Fire key/value via websocket
    8. Receive key/value in client via websocket
- **Batch**
    1. Send a batch command from client via HTTP
    2. Receive batch command in batch-service, create batch and publish as multiple single messages to
    to MQTT broker (queue *batch*)
    3. Wait for reply on separate MQTT channel in queue *batch_reply*. Return bad request if timeout limit is exceeded.
    4. Consume batches in redis-service from queue *batch*
    5. Once the last message in the batch arrives, store batch in Redis as hashset
    6. Publish batchstatus to queue *batch_reply*, Done if all succeded, otherwise failure reason
    7. Return reply from batch-service once it arrives (checking against batch-key)
    8. Return status from HTTP to client
    9. Show response from batch-service in client, and reload stored batches

## Technologies / services

    - Client                    / Blazor WASM
    - API-Gateway               / NetCore WebApi / Ocelot
    - Service discovery         / Consul
    - Websocket service         / Go
    - Roundtrip service         / Go
    - Batch service             / NetCore WebApi
    - Redis service             / Rust
    - Database(batch/roundtrip) / Redis
    - Message broker/MQTT       / RabbitMQ

## Description

Serves as an example of how to compose a microservice architecture.

Focus in this example has been on providing a transaction between services in a microservice architecture where no data is lost, following ACID principles.

Obviously there are lots of improvements to be done. But this serves as a skeleton how one can setup an architecture and work in a docker environment.

I have also used several different languages/frameworks/tools to showcase and explore the possibilities of working with a microservice architecture. 

In a monolitihic architecture one choice of language is made, but here we can leverage the best tool for the job.

Images provided by me can be found on docker hub, and in the root directory there is a docker-compose.production.yml to be used if you 
don't want to use the source code.
