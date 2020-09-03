pub mod batch_processor;
pub mod mqtt_consumer;
pub mod mqtt_message;
pub mod mqtt_publisher;
pub mod redis_manager;

use crate::mqtt_message::Batch;
use amiquip::{Connection, Result};
use std::{env, thread};

fn main() -> Result<()> {
    let batch_1 = Batch {
        hash_key: String::from("1"),
        key: String::from("key_1"),
        value: String::from("value_1"),
        is_last_in_batch: false,
    };

    let batch_2 = Batch {
        hash_key: String::from("1"),
        key: String::from("key_12"),
        value: String::from("value_2"),
        is_last_in_batch: true,
    };

    let batch_11 = Batch {
        hash_key: String::from("2"),
        key: String::from("key_11"),
        value: String::from("value_1"),
        is_last_in_batch: false,
    };

    let batch_33 = Batch {
        hash_key: String::from("3"),
        key: String::from("key_33"),
        value: String::from("value_1"),
        is_last_in_batch: false,
    };

    let mut batch_processor = batch_processor::build_batchprocessor();

    println!("add_batch_1");
    batch_processor.add_batch(batch_1);

    thread::sleep(std::time::Duration::from_secs(2));
    println!("add_batch_2");
    batch_processor.add_batch(batch_2);

    thread::sleep(std::time::Duration::from_secs(2));
    println!("add_batch_3");
    batch_processor.add_batch(batch_33);

    thread::sleep(std::time::Duration::from_secs(2));
    println!("add_batch_4");
    batch_processor.add_batch(batch_11);
    
    thread::sleep(std::time::Duration::from_secs(2));
    println!("print");
    batch_processor.print();

    thread::sleep(std::time::Duration::from_secs(5));
    println!("Should have time exceeded now! \n\n");
    batch_processor.print();

    return Ok(());
    // // FOR DEBUG
    // env::set_var("REDIS_URL", "redis://localhost:6379");
    // env::set_var("AMQP_URL", "amqp://guest:guest@localhost:5673");
    // // END DEBUG

    // // Read amqp url from docker env
    // let amqp_url = env::var("AMQP_URL").unwrap();

    // let mut connection: Connection = match mqtt_consumer::try_connect(amqp_url) {
    //     Ok(con) => con,
    //     Err(err) => return Result::Err(err),
    // };

    // // Open a channel - None says let the library choose the channel ID.
    // let channel_roundtrip = connection.open_channel(None)?;
    // let channel_forward_roundtrip = connection.open_channel(None)?;
    // let channel_batch = connection.open_channel(None)?;

    // let roundtrip_thread = thread::spawn(move || {
    //     let exchange_forward = match mqtt_publisher::bind_exchange_queue(&channel_forward_roundtrip)
    //     {
    //         Ok(exchange) => exchange,
    //         Err(err) => {
    //             println!(
    //                 "Failed to bind forward publish exchange to queue: {:?}",
    //                 err
    //             );
    //             return;
    //         }
    //     };

    //     match mqtt_consumer::consume_roundtrip(
    //         &channel_roundtrip, // consume roundtrip channel
    //         &exchange_forward,  // publish forward roundtip exchange
    //     ) {
    //         Ok(_) => {}
    //         Err(err) => {
    //             println!("Failed to consume queue 'events', err: {}", err);
    //         }
    //     }
    // });

    // let batch_thread = thread::spawn(move || match mqtt_consumer::consume_batch(&channel_batch) {
    //     Ok(_) => {}
    //     Err(err) => {
    //         println!("Failed to consume queue 'batch', err: {}", err);
    //     }
    // });

    // roundtrip_thread.join().unwrap();
    // batch_thread.join().unwrap();

    // println!("Service will exit, consumers are done!");

    // return Ok(());
}
