# microservice-architecture
An example of microservice architecture in docker containers

## Scenarios
+ A key/value is sent as a roundtrip from the client, is transported through a range of services and returned to the client.
+ A batch is sent, and persisted to a store and reply the status of the transaction. The batch is then read from the store.

## Description

Serves as an example of how to compose a microservice architecture.

Focus in this example has been on providing a transaction between services in a microservice architecture where no data is lost, following ACID principles, regarding the batch scenario.

Obviously there are lots of improvements to be done. But this serves as a skeleton how one can setup an architecture and work in a docker environment.

I have also used several different languages/frameworks/tools to showcase and explore the possibilities with working with a microservice architecture. 

In a monolitihic architecture one choice of language is made, but here we can leverage the best tool for the job.

Images provided by me can be found on docker hub, and in the root directory there is a docker-compose.production.yml to be used if you 
don't want to use the source code.


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