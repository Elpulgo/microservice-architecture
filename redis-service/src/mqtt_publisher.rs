use serde::export::fmt::Debug;

pub fn publish<T: serde::Serialize + Debug>(
    exchange: &amiquip::Exchange,
    routing_key: &str,
    message: T,
) -> Result<(), String> {
    let bytes = serde_json::to_vec(&message).unwrap();
    match exchange.publish(amiquip::Publish::new(&bytes, routing_key)) {
        Ok(result) => {
            return Ok(result);
        }
        Err(err) => {
            return Err(String::from(format!(
                "Failed to publish message: {:?}, {}",
                message, err
            )));
        }
    }
}

pub fn bind_exchange_queue<'a>(
    channel: &'a amiquip::Channel,
    exchange_name: &str,
    queue_name: &str,
) -> Result<amiquip::Exchange<'a>, amiquip::Error> {
    let roundtrip_exchange = match channel.exchange_declare(
        amiquip::ExchangeType::Direct,
        exchange_name,
        amiquip::ExchangeDeclareOptions::default(),
    ) {
        Ok(exchange) => exchange,
        Err(err) => {
            println!("Failed to declare roundtrip exchange! {}", err);
            return Err(err);
        }
    };

    match channel.queue_declare(queue_name, amiquip::QueueDeclareOptions::default()) {
        Ok(queue) => {
            println!("Queue '{}' was declared!", queue.name());

            match queue.bind(
                &roundtrip_exchange,
                queue.name(),
                amiquip::FieldTable::default(),
            ) {
                Ok(_) => {
                    println!(
                        "Succesfully bind queue '{} with exchange '{}'",
                        &queue.name(),
                        &roundtrip_exchange.name()
                    );
                    return Ok(roundtrip_exchange);
                }
                Err(err) => {
                    println!(
                        "Failed to bind exchange '{}' to queue '{}': {}",
                        &roundtrip_exchange.name(),
                        &queue.name(),
                        err
                    );
                    return Err(err);
                }
            };
        }
        Err(err) => {
            println!("Failed to declare queue 'forward_roundtrip': {}", err);
            return Err(err);
        }
    };
}
