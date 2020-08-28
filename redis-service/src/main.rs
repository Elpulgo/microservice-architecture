pub mod mqtt_consumer;
pub mod mqtt_message;
pub mod mqtt_publisher;
pub mod redis_manager;

use amiquip::{Connection, Result};
use std::{env, thread};

fn main() -> Result<()> {
    // Read amqp url from docker env
    let amqp_url = env::var("AMQP_URL").unwrap();

    let mut connection: Connection = match mqtt_consumer::try_connect(amqp_url) {
        Ok(con) => con,
        Err(err) => return Result::Err(err),
    };

    // Open a channel - None says let the library choose the channel ID.
    let channel_roundtrip = connection.open_channel(None)?;
    let channel_forward_roundtrip = connection.open_channel(None)?;
    let channel_batch = connection.open_channel(None)?;

    let roundtrip_thread = thread::spawn(move || {
        let exchange_forward = match mqtt_publisher::bind_exchange_queue(&channel_forward_roundtrip)
        {
            Ok(exchange) => exchange,
            Err(err) => {
                println!(
                    "Failed to bind forward publish exchange to queue: {:?}",
                    err
                );
                return;
            }
        };

        match mqtt_consumer::consume_roundtrip(
            &channel_roundtrip, // consume roundtrip channel
            &exchange_forward,  // publish forward roundtip exchange
        ) {
            Ok(_) => {}
            Err(err) => {
                println!("Failed to consume queue 'events', err: {}", err);
            }
        }
    });

    let batch_thread = thread::spawn(move || match mqtt_consumer::consume_batch(&channel_batch) {
        Ok(_) => {}
        Err(err) => {
            println!("Failed to consume queue 'batch', err: {}", err);
        }
    });

    roundtrip_thread.join().unwrap();
    batch_thread.join().unwrap();

    println!("Service will exit, consumers are done!");

    return Ok(());
}
