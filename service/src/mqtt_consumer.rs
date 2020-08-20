use amiquip::{Connection, ConsumerMessage, ConsumerOptions, QueueDeclareOptions, Result};
use std::env;
use std::io::{Error, ErrorKind};
use std::{thread, time};
use url::Url;

const MAX_CONNECTION_TRIES: i32 = 20;

pub fn consume() -> Result<()> {
    // Read amqp url from docker env
    let amqp_url = env::var("AMQP_URL").unwrap();

    let mut connection: Connection = match try_connect(amqp_url) {
        Ok(con) => con,
        Err(err) => return Result::Err(err),
    };

    // Open a channel - None says let the library choose the channel ID.
    let channel = connection.open_channel(None)?;

    // Declare the queue.
    let queue = channel.queue_declare("events", QueueDeclareOptions::default())?;

    println!("Queue was declared!");

    // Start a consumer.
    let consumer = queue.consume(ConsumerOptions::default())?;

    println!("Waiting for messages...");

    for (_, message) in consumer.receiver().iter().enumerate() {
        match message {
            ConsumerMessage::Delivery(delivery) => {
                let body = String::from_utf8_lossy(&delivery.body);
                println!("({:>3}) Received [{}]", delivery.delivery_tag(), body);
                // consumer.reject(delivery, false)?;
                // consumer.nack(delivery, false)?;
                // consumer.ack_multiple(delivery)?;
                consumer.ack(delivery)?;
                
                // ack(delivery)?;
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

    println!("Will close MQTT connection!");
    connection.close()
}

fn try_connect(amqp_url: String) -> Result<amiquip::Connection> {
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
