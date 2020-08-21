pub mod mqtt_consumer;
pub mod redis_manager;
pub mod mqtt_message;

use amiquip::{Connection, Result};
use std::{env, thread};
use mqtt_consumer::MessageKind;

fn main() -> Result<()> {
    // Read amqp url from docker env
    let amqp_url = env::var("AMQP_URL").unwrap();

    let mut connection: Connection = match mqtt_consumer::try_connect(amqp_url) {
        Ok(con) => con,
        Err(err) => return Result::Err(err),
    };

    // Open a channel - None says let the library choose the channel ID.
    let channel_roundtrip = connection.open_channel(None)?;
    let channel_batch = connection.open_channel(None)?;

    let roundtrip_thread =
        thread::spawn(
            move || match mqtt_consumer::consume(&channel_roundtrip, "events", MessageKind::ROUNDTRIP) {
                Ok(_) => {}
                Err(err) => {
                    println!("Failed to consume queue 'events', err: {}", err);
                }
            },
        );

    let batch_thread =
        thread::spawn(
            move || match mqtt_consumer::consume(&channel_batch, "batch", MessageKind::BATCH) {
                Ok(_) => {}
                Err(err) => {
                    println!("Failed to consume queue 'batch', err: {}", err);
                }
            },
        );

    roundtrip_thread.join().unwrap();
    batch_thread.join().unwrap();

    println!("Service will exit, consumers are done!");

    return Ok(());
}
