# Microservice-architecture
An example of microservice architecture in docker containers.
Use **docker-compose.production.yml** to test it out. Images are available at **https://hub.docker.com/u/elpulgo**.

## Scenarios

+ A key/value is sent as a roundtrip from the client, is transported through a range of services, persisted, and returned to the client.
+ A batch(customable size) is sent, and persisted to a store and returned with the status of the transaction. The batch is then read from the store.

## Architecture

<img src="https://github.com/Elpulgo/microservice-architecture/blob/master/screens/microservice-architecture.png" width="640">

## Flow
 - **Roundtrip**
    - Send a key/value from client via HTTP
    - Receive key/value in roundtrip-service and publish to MQTT broker (queue *event*)
    - Consume key/value in redis-service from queue *event*
    - Persist key/value in Redis from redis-service
    - Publish key/value to MQTT broker (queue *forward_roundtrip*)
    - Consume key/value in websocket-service from queue *forward_roundtrip*
    - Fire key/value via websocket
    - Receive key/value in client via websocket
- **Batch**
    - Send a batch command from client via HTTP
    - Receive batch command in batch-service, create batch and publish as multiple single messages to
    to MQTT broker (queue *batch*)
    - Wait for reply on separate MQTT channel in queue *batch_reply*. Return bad request if timeout limit is exceeded.
    - Consume batches in redis-service from queue *batch*
    - Once the last message in the batch arrives, store batch in Redis as hashset
    - Publish batchstatus to queue *batch_reply*, Done if all succeded, otherwise failure reason
    - Return reply from batch-service once it arrives (checking against batch-key)
    - Return status from HTTP to client
    - Show response from batch-service in client, and reload stored batches

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
