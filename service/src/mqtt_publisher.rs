use crate::mqtt_message::RoundTrip;

const QUEUE_NAME: &str = "forward_roundtrip";
const ROUTING_KEY: &str = "forward_roundtrip";
const EXCHANGE_NAME: &str = "exchange_forward_roundtrip";

pub fn publish(
    exchange: &amiquip::Exchange,
    roundtrip: &RoundTrip,
) -> Result<(), String> {
    let roundtrip_as_bytes = bincode::serialize(roundtrip).unwrap();
    match exchange.publish(amiquip::Publish::new(&roundtrip_as_bytes, ROUTING_KEY)) {
        Ok(result) => {
            println!(
                "Successfully forwarded roundtrip key/value: {:?}",
                &roundtrip
            );
            return Ok(result);
        }
        Err(err) => {
            println!("Failed to forward roundtrip key/value!: {}", err);
            return Err(String::from("Failed to forward roundtrip!"));
        }
    }
}

pub fn bind_exchange_queue<'a>(
    channel: &'a amiquip::Channel,
) -> Result<amiquip::Exchange<'a>, amiquip::Error> {
    let roundtrip_exchange = match channel.exchange_declare(
        amiquip::ExchangeType::Direct,
        EXCHANGE_NAME,
        amiquip::ExchangeDeclareOptions::default(),
    ) {
        Ok(exchange) => exchange,
        Err(err) => {
            println!("Failed to declare roundtrip exchange! {}", err);
            return Err(err);
        }
    };

    match channel.queue_declare(QUEUE_NAME, amiquip::QueueDeclareOptions::default()) {
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
