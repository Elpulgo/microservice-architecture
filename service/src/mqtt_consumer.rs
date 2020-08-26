use crate::mqtt_message::{Batch, KeyValue, RoundTrip};
use crate::redis_manager;
use amiquip::{Connection, ConsumerMessage, ConsumerOptions, Delivery, QueueDeclareOptions};
use std::io::{Error, ErrorKind};
use std::{thread, time};
use url::Url;

const MAX_CONNECTION_TRIES: i32 = 20;

pub enum MessageKind {
    ROUNDTRIP,
    BATCH,
}

pub fn consume(
    channel: &amiquip::Channel,
    queue_name: &str,
    message_kind: MessageKind,
) -> amiquip::Result<()> {
    let queue = channel.queue_declare(queue_name, QueueDeclareOptions::default())?;

    println!("Queue '{}' was declared!", queue_name);

    let consumer = queue.consume(ConsumerOptions::default())?;

    println!("Waiting for messages in queue '{}'...", queue_name);

    for (_, message) in consumer.receiver().iter().enumerate() {
        match message {
            ConsumerMessage::Delivery(delivery) => {
                let body = String::from_utf8_lossy(&delivery.body);

                match message_kind {
                    MessageKind::ROUNDTRIP => match handle_roundtrip(&body, &queue_name, &delivery)
                    {
                        Ok(_) => consumer.ack(delivery)?,
                        Err(_) => consumer.reject(delivery, false)?,
                    },
                    MessageKind::BATCH => match handle_batch(&body, &queue_name, &delivery) {
                        Ok(_) => consumer.ack(delivery)?,
                        Err(_) => consumer.reject(delivery, false)?,
                    },
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

fn handle_roundtrip(body: &str, queue_name: &str, delivery: &Delivery) -> Result<(), String> {
    match serde_json::from_str::<RoundTrip>(&body) {
        Ok(roundtrip) => match redis_manager::set(&roundtrip.key(), &roundtrip.value()) {
            Ok(_) => {
                println!(
                    "('{}' {:>3}) Received ROUNDTRIP and stored in redis [{:?}]",
                    queue_name,
                    delivery.delivery_tag(),
                    roundtrip
                );
                return Ok(());
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

fn handle_batch(body: &str, queue_name: &str, delivery: &Delivery) -> Result<(), String> {
    match serde_json::from_str::<Batch>(&body) {
        Ok(batch) => match redis_manager::set_hash(&batch.hash_key(), &batch.key(), &batch.value()) {
            Ok(_) => {
                println!(
                    "('{}' {:>3}) Received BATCH and stored in redis [{:?}]",
                    queue_name,
                    delivery.delivery_tag(),
                    batch
                );
                return Ok(());
            }
            Err(err) => {
                println!("Failed to store BATCH key/value in redis: {}", err);
                return Err(String::from("Failed to set value in redis!"));
            }
        },
        Err(err) => {
            println!("Failed to parse batch value: {:?}, error: {}", &body, err);
            return Err(String::from("Failed to parse batch value!"));
        }
    }
}
