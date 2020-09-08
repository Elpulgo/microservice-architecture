pub mod batch_processor;
pub mod mqtt_consumer;
pub mod mqtt_message;
pub mod mqtt_publisher;
pub mod redis_manager;
pub mod variables;

use amiquip::{Connection, Result};
use std::thread;

const FORWARD_ROUNDTRIP_EXCHANGE_NAME: &str = "exchange_forward_roundtrip";
const FORWARD_ROUNDTRIP_QUEUE_NAME: &str = "forward_roundtrip";
const BATCH_EXCHANGE_NAME: &str = "exchange_batch_reply";
const BATCH_QUEUE_NAME: &str = "batch_reply";

fn main() -> Result<()> {
    let mut connection: Connection =
        match mqtt_consumer::try_connect(variables::get_amqp_connection()) {
            Ok(con) => con,
            Err(err) => return Result::Err(err),
        };

    // Open a channel - None says let the library choose the channel ID.
    let channel_roundtrip = match connection.open_channel(None) {
        Ok(channel) => channel,
        Err(err) => panic!("Failed to open roundtrip channel {}!", err),
    };

    let channel_forward_roundtrip = match connection.open_channel(None) {
        Ok(channel) => channel,
        Err(err) => panic!("Failed to open forward roundtrip channel {}!", err),
    };

    let channel_batch = match connection.open_channel(None) {
        Ok(channel) => channel,
        Err(err) => panic!("Failed to open batch channel {}!", err),
    };

    let channel_batch_reply = match connection.open_channel(None) {
        Ok(channel) => channel,
        Err(err) => panic!("Failed to open batch reply channel {}!", err),
    };

    let roundtrip_thread = create_roundtrip_thread(channel_roundtrip, channel_forward_roundtrip);
    let batch_thread = create_batch_thread(channel_batch, channel_batch_reply);

    roundtrip_thread.join().unwrap();
    batch_thread.join().unwrap();

    println!("Service will exit, consumers are done!");

    return Ok(());
}

fn create_roundtrip_thread(
    channel_roundtrip: amiquip::Channel,
    channel_forward_roundtrip: amiquip::Channel,
) -> thread::JoinHandle<()> {
    return thread::spawn(move || {
        let exchange_forward = match mqtt_publisher::bind_exchange_queue(
            &channel_forward_roundtrip,
            FORWARD_ROUNDTRIP_EXCHANGE_NAME,
            FORWARD_ROUNDTRIP_QUEUE_NAME,
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
}

fn create_batch_thread(
    channel_batch: amiquip::Channel,
    channel_batch_reply: amiquip::Channel,
) -> thread::JoinHandle<()> {
    return thread::spawn(move || {
        let exchange_batch_reply = match mqtt_publisher::bind_exchange_queue(
            &channel_batch_reply,
            BATCH_EXCHANGE_NAME,
            BATCH_QUEUE_NAME,
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
}
