use crate::mqtt_message::{Batch, RoundTrip};
use amiquip::{Connection, ConsumerMessage, ConsumerOptions, QueueDeclareOptions, Result};
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
) -> Result<()> {
    let queue = channel.queue_declare(queue_name, QueueDeclareOptions::default())?;

    println!("Queue '{}' was declared!", queue_name);

    let consumer = queue.consume(ConsumerOptions::default())?;

    println!("Waiting for messages in queue '{}'...", queue_name);

    for (_, message) in consumer.receiver().iter().enumerate() {
        match message {
            ConsumerMessage::Delivery(delivery) => {
                let body = String::from_utf8_lossy(&delivery.body);

                match message_kind {
                    MessageKind::ROUNDTRIP => {
                        match serde_json::from_str::<RoundTrip>(&body){
                            Ok(roundtrip) => {
                                println!(
                                    "('{}' {:>3}) Received [{:?}]",
                                    queue_name,
                                    delivery.delivery_tag(),
                                    roundtrip
                                );
                            },
                            Err(_) => {
                                println!("Failed to parse roundtrip value: {:?}", &body);
                                consumer.reject(delivery, false)?;
                                continue;
                            }
                        }
                        
                    }
                    MessageKind::BATCH => {
                        match serde_json::from_str::<Batch>(&body){
                            Ok(batch) => {
                                println!(
                                    "('{}' {:>3}) Received [{:?}]",
                                    queue_name,
                                    delivery.delivery_tag(),
                                    batch
                                );
                            },
                            Err(_) => {
                                println!("Failed to parse batch value: {:?}", &body);
                                consumer.reject(delivery, false)?;
                                continue;
                            }
                        }
                    }
                }

                consumer.ack(delivery)?;
            }
            ConsumerMessage::ClientCancelled => {
                println!("Client cancelled");
            }
            other => {
                println!("Consumer ended: {:?}", other);
                break;
            }
        }
    }

    return Ok(());
}

pub fn try_connect(amqp_url: String) -> Result<amiquip::Connection> {
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
