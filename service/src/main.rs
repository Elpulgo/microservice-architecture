pub mod mqtt_consumer;
use mqtt_consumer::consume;
use amiquip::{Result, Error};


fn main() -> Result<()> {
  consume();
}