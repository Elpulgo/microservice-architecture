use crate::mqtt_message::Batch;
use crate::redis_manager;
use crate::mqtt_publisher;
use crate::variables;
use std::collections::hash_map::{Entry, HashMap};
use serde::{Deserialize, Serialize};
use serde_repr::{Serialize_repr, Deserialize_repr};

const BATCH_REPLY_ROUTING_KEY: &str = "batch_reply";

pub struct BatchProcessor {
    batch_handler_map: HashMap<String, BatchHandler>,
}

#[derive(Debug)]
pub struct BatchHandler {
    pub key: String,
    pub init_time: chrono::DateTime<chrono::Utc>,
    pub status: BatchStatus,
    pub batches: Vec<Batch>,
}

#[derive(Debug, PartialEq, Copy, Clone, Serialize_repr, Deserialize_repr)]
#[repr(i32)]
pub enum BatchStatus {
    None = 0,
    PendingConsume = 1,
    PendingDatabase = 2,
    DatabaseOperationFailed = 3,
    Invalid = 4,
    TimeoutExceeded = 5,
    Done = 6,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "PascalCase")]
pub struct BatchReply {
    pub status: BatchStatus,
    pub key: String
}

impl BatchProcessor {
    pub fn new() -> BatchProcessor {
        BatchProcessor {
            batch_handler_map: HashMap::<String, BatchHandler>::new(),
        }
    }

    pub fn add_batch(&mut self, batch: Batch, reply_exchange: &amiquip::Exchange) {
        let key = &batch.hash_key().to_string();

        match self.batch_handler_map.entry(String::from(key)) {
            Entry::Vacant(entry) => {
                let mut batch_handler = BatchHandler::new(batch.hash_key().to_owned());
                batch_handler.add_batch_value(batch);
                entry.insert(batch_handler);
            }
            Entry::Occupied(mut entry) => {
                if entry.get_mut().is_timeout_exceeded() {
                    println!("Batch with key '{}' reached timeout!", key);
                    self.dispose_batch_handler(key);
                    self.publish_batch_reply(reply_exchange, BatchStatus::TimeoutExceeded, key);
                } else {
                    match batch.is_last_in_batch() {
                        true => {
                            entry.get_mut().add_batch_value(batch);
                            let batch_size = variables::get_batch_size();
                            match batch_size == entry.get().batches.len() {
                                true => {
                                    entry.get_mut().change_status(BatchStatus::PendingDatabase);
                                    let ref_batches = &entry.into_mut().batches;
                                    match redis_manager::set_hash_all(
                                        String::from(key),
                                        ref_batches,
                                    ) {
                                        Ok(_res) => {
                                            println!(
                                                "Successfully added batch with key '{}' to Redis!",
                                                key
                                            );
                                            self.publish_batch_reply(reply_exchange, BatchStatus::Done, key);
                                            self.dispose_batch_handler(key);
                                        }
                                        Err(err) => {
                                            println!(
                                                "Failed to add batch with key '{}'to Redis: {}",
                                                key, err
                                            );
                                            self.publish_batch_reply(reply_exchange, BatchStatus::DatabaseOperationFailed, key);
                                        }
                                    }
                                }
                                false => {
                                    println!("Last in batch with key '{}', but not correct batch size, need '{}' but found '{}'!", 
                                        key, 
                                        batch_size, 
                                        entry.get().batches.len());
                                    entry.get_mut().change_status(BatchStatus::Invalid);
                                    self.dispose_batch_handler(key);
                                    self.publish_batch_reply(reply_exchange, BatchStatus::Invalid, key);
                                }
                            }
                        }
                        false => {
                            entry.get_mut().add_batch_value(batch);
                        }
                    }
                }
            }
        };

        self.dispose_non_pending_batch_handlers();
    }

    fn dispose_batch_handler(&mut self, key: &String) {
        println!("Batch with key '{}' will be disposed!", key);
        self.batch_handler_map.remove_entry(key);
    }

    fn dispose_non_pending_batch_handlers(&mut self) {
        self.batch_handler_map.retain(|_, value| {
            let is_timeout_exceeded = value.is_timeout_exceeded();
            if is_timeout_exceeded {
                println!("Will disposed batch '{}' since timeout exceeded ...", value.key);
            }

            (value.status == BatchStatus::PendingConsume
                || value.status == BatchStatus::PendingDatabase)
                && !is_timeout_exceeded
        });
    }

    fn publish_batch_reply(
        &mut self, 
        reply_exchange: &amiquip::Exchange, 
        status: BatchStatus, 
        key: &str) {
        match mqtt_publisher::publish(
            reply_exchange, 
            BATCH_REPLY_ROUTING_KEY,
            BatchReply { 
                status: status, 
                key: String::from(key) 
        }){
            Ok(_) => println!("Succesfully published BatchReply for batch key '{}'", key),
            Err(err) => println!("{}", err)
        }
    }
}

impl BatchHandler {
    pub fn new(key: String) -> BatchHandler {
        BatchHandler {
            key: key,
            init_time: chrono::offset::Utc::now(),
            status: BatchStatus::PendingConsume,
            batches: Vec::<Batch>::new(),
        }
    }

    fn is_timeout_exceeded(&mut self) -> bool {
        return chrono::offset::Utc::now()
            .signed_duration_since(self.init_time)
            .num_seconds()
            >= 10;
    }

    fn add_batch_value(&mut self, batch: Batch) {
        match &self.batches.is_empty() {
            true => self.batches = vec![batch],
            false => self.batches.push(batch),
        }
    }

    fn change_status(&mut self, status: BatchStatus) {
        self.status = status;
    }
}
