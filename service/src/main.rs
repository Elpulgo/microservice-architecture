pub mod mqtt_consumer;
use amiquip::Result;
use mqtt_consumer::consume;

fn main() -> Result<()> {
    match consume() {
        Ok(_) => Ok(()),
        Err(err) => {
            println!("Failed to consume messages from MQTT! {}", err);
            Ok(())
        } 
    }
}
