pub mod batch_processor;
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
    let channel_batch_reply = connection.open_channel(None)?;

    let roundtrip_thread = thread::spawn(move || {
        let exchange_forward = match mqtt_publisher::bind_exchange_queue(
            &channel_forward_roundtrip,
            "exchange_forward_roundtrip",
            "forward_roundtrip",
        ) {
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

    let batch_thread = thread::spawn(move || {
        let exchange_batch_reply = match mqtt_publisher::bind_exchange_queue(
            &channel_batch_reply,
            "exchange_batch_reply",
            "batch_reply",
        ) {
            Ok(exchange) => exchange,
            Err(err) => {
                panic!(
                    "Failed to bind batch_reply publish exchange to queue: {:?}",
                    err
                );
            }
        };

        let batch_processor = batch_processor::BatchProcessor::new();

        match mqtt_consumer::consume_batch(&channel_batch, &exchange_batch_reply, batch_processor) {
            Ok(_) => {}
            Err(err) => {
                println!("Failed to consume queue 'batch', err: {}", err);
            }
        }
    });

    roundtrip_thread.join().unwrap();
    batch_thread.join().unwrap();

    println!("Service will exit, consumers are done!");

    return Ok(());
}

// fn test() -> Result<()> {
//     // FOR DEBUG
//     env::set_var("REDIS_URL", "redis://localhost:6379");
//     env::set_var("AMQP_URL", "amqp://guest:guest@localhost:5672");
//     // END DEBUG

//     let amqp_url = env::var("AMQP_URL").unwrap();

//     let mut connection: Connection = match mqtt_consumer::try_connect(amqp_url) {
//         Ok(con) => con,
//         Err(err) => return Result::Err(err),
//     };
//     let channel_batch_reply = connection.open_channel(None)?;

//     let exchange_batch_reply = match mqtt_publisher::bind_exchange_queue(
//         &channel_batch_reply,
//         "exchange_batch_reply",
//         "batch_reply",
//     ) {
//         Ok(exchange) => exchange,
//         Err(err) => {
//             panic!(
//                 "Failed to bind batch_reply publish exchange to queue: {:?}",
//                 err
//             );
//         }
//     };

//     let mut batch_processor = batch_processor::BatchProcessor::new();

//     for i in 1..11 {
//         let batch = Batch {
//             hash_key: String::from("hash_key_1"),
//             key: format!("key_{}", i),
//             value: format!("value_{}", i),
//             is_last_in_batch: i == 10,
//             batch_size: 10,
//         };

//         batch_processor.add_batch(batch, &exchange_batch_reply);

//         // println!("\n########################");
//         // batch_processor.print();
//         // println!("\n########################");

//         // thread::sleep(std::time::Duration::from_secs(2));
//     }

//     batch_processor.print();
//     return Ok(());
// }
