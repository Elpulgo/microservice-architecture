use crate::batch_processor::BatchProcessor;
use crate::mqtt_message::{Batch, KeyValue, RoundTrip};
use crate::mqtt_publisher::publish;
use crate::redis_manager;
use amiquip::{Connection, ConsumerMessage, ConsumerOptions, Delivery, QueueDeclareOptions};
use std::io::{Error, ErrorKind};
use std::{thread, time};
use url::Url;

const MAX_CONNECTION_TRIES: i32 = 20;
const CONSUME_ROUNDTRIP_QUEUE_NAME: &str = "events";
const CONSUME_BATCH_QUEUE_NAME: &str = "batch";

pub fn consume_roundtrip(
    consume_channel: &amiquip::Channel,
    publish_forward_exchange: &amiquip::Exchange,
) -> amiquip::Result<()> {
    let queue = consume_channel
        .queue_declare(CONSUME_ROUNDTRIP_QUEUE_NAME, QueueDeclareOptions::default())?;

    println!("Queue '{}' was declared!", CONSUME_ROUNDTRIP_QUEUE_NAME);

    let consumer = queue.consume(ConsumerOptions::default())?;

    println!(
        "Waiting for messages in queue '{}'...",
        CONSUME_ROUNDTRIP_QUEUE_NAME
    );

    for (_, message) in consumer.receiver().iter().enumerate() {
        match message {
            ConsumerMessage::Delivery(delivery) => {
                let body = String::from_utf8_lossy(&delivery.body);

                match handle_roundtrip(&body, &delivery) {
                    Ok(roundtrip) => {
                        consumer.ack(delivery)?;
                        let bytes = serde_json::to_vec(&roundtrip).unwrap();
                        match publish(&publish_forward_exchange, "forward_roundtrip", bytes) {
                            Ok(_) => continue,
                            Err(err) => println!("{}", err),
                        }
                    }
                    Err(_) => consumer.reject(delivery, false)?,
                }
            }
            ConsumerMessage::ClientCancelled => {
                println!("Client cancelled");
                break;
            }
            other => {
                println!("Consumer ended: {:?}", other);
                break;
            }
        }
    }

    return Ok(());
}

pub fn consume_batch(
    channel: &amiquip::Channel,
    reply_exchange: &amiquip::Exchange,
    mut batch_processor: BatchProcessor,
) -> amiquip::Result<()> {
    let queue = channel.queue_declare(CONSUME_BATCH_QUEUE_NAME, QueueDeclareOptions::default())?;
    println!("Queue '{}' was declared!", CONSUME_BATCH_QUEUE_NAME);

    let consumer = queue.consume(ConsumerOptions::default())?;

    println!(
        "Waiting for messages in queue '{}'...",
        CONSUME_BATCH_QUEUE_NAME
    );

    for (_, message) in consumer.receiver().iter().enumerate() {
        match message {
            ConsumerMessage::Delivery(delivery) => {
                let body = String::from_utf8_lossy(&delivery.body);
                let batch = serde_json::from_str::<Batch>(&body).unwrap();

                batch_processor.add_batch(batch, reply_exchange);
                consumer.ack(delivery)?
            }
            ConsumerMessage::ClientCancelled => {
                println!("Client cancelled");
                break;
            }
            other => {
                println!("Consumer ended: {:?}", other);
                break;
            }
        }
    }

    return Ok(());
}

pub fn try_connect(amqp_url: String) -> amiquip::Result<amiquip::Connection> {
    let mut connection_retries = 0;

    // Open connection.
    loop {
        connection_retries = connection_retries + 1;
        let connection = match Connection::insecure_open(&amqp_url) {
            Ok(con) => con,
            Err(_) => {
                println!(
                    "Failed to connect to MQTT broker, will retry, {} / {}",
                    connection_retries, MAX_CONNECTION_TRIES
                );

                if connection_retries >= MAX_CONNECTION_TRIES {
                    return Result::Err(amiquip::Error::FailedToConnect {
                        url: Url::parse(&amqp_url).unwrap(),
                        source: Error::new(
                            ErrorKind::ConnectionRefused,
                            "Failed to connect to MQTT broker!",
                        ),
                    });
                }

                thread::sleep(time::Duration::from_secs(1));
                continue;
            }
        };

        println!("Successfully connected to MQTT broker!");

        return Ok(connection);
    }
}

fn handle_roundtrip(body: &str, delivery: &Delivery) -> Result<RoundTrip, String> {
    match serde_json::from_str::<RoundTrip>(&body) {
        Ok(roundtrip) => match redis_manager::set(&roundtrip.key(), &roundtrip.value()) {
            Ok(_) => {
                println!(
                    "('{}') Received ROUNDTRIP and stored in redis [{:?}]",
                    delivery.delivery_tag(),
                    roundtrip
                );
                return Ok(roundtrip);
            }
            Err(err) => {
                println!("Failed to store ROUNDTRIP key/value in redis: {}", err);
                return Err(String::from("Failed to set value in redis!"));
            }
        },
        Err(err) => {
            println!(
                "Failed to parse roundtrip value: {:?}, error: {}",
                &body, err
            );
            return Err(String::from("Failed to parse roundtrip value!"));
        }
    }
}