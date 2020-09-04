use crate::mqtt_message::Batch;
use crate::redis_manager;
use std::collections::hash_map::{Entry, HashMap};

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

#[derive(Debug, PartialEq, Copy, Clone)]
pub enum BatchStatus {
    PendingConsume,
    PendingDatabase,
    Invalid,
    Done,
}

impl BatchProcessor {
    pub fn new() -> BatchProcessor {
        BatchProcessor {
            batch_handler_map: HashMap::<String, BatchHandler>::new(),
        }
    }

    // FOR DEBUGGING PURPOSES!!
    pub fn print(&mut self) {
        for (key, value) in self.batch_handler_map.iter_mut() {
            println!("{} / {:?}", key, value);
        }
    }
    // END DEBUG!!!

    pub fn add_batch(&mut self, batch: Batch) {
        self.dispose_non_pending_batch_handlers();

        let key = &batch.hash_key().to_string();
        let mut timeout_exceeded = false;

        match self.batch_handler_map.entry(String::from(key)) {
            Entry::Vacant(entry) => {
                let mut batch_handler = BatchHandler::new(batch.hash_key().to_owned());
                batch_handler.add_batch_value(batch);
                entry.insert(batch_handler);
            }
            Entry::Occupied(mut entry) => {
                match entry.get_mut().is_timeout_exceeded() {
                    true => timeout_exceeded = true,
                    false => {
                        match batch.is_last_in_batch() {
                            true => {
                                let batch_size = batch.batch_size;
                                entry.get_mut().add_batch_value(batch);
                                match batch_size == entry.get().batches.len() {
                                    true => {
                                        println!("Should update status to PendingDatabase and do redis insert..");
                                        entry.get_mut().change_status(BatchStatus::PendingDatabase);
                                        let ref_batches = &entry.into_mut().batches;
                                        match redis_manager::set_hash_all(
                                            String::from(key),
                                            ref_batches,
                                        ) {
                                            Ok(res) => {}
                                            Err(err) => {}
                                        }
                                        // perform transaction to Redis and publish success to mqtt!
                                    }
                                    false => {
                                        println!("Last in batch with key '{}' but not correct batch size of '{}'. Will dispose batch!", key, batch_size);
                                        entry.get_mut().change_status(BatchStatus::Invalid);
                                        // publish to mqtt that this is wrong!
                                    }
                                }
                            }
                            false => {
                                entry.get_mut().add_batch_value(batch);
                            }
                        }
                    }
                }
            }
        };

        if !timeout_exceeded {
            return;
        }
        
        // Publish to MQTT that timeout happened..
        self.dispose_batch_handler(key);
    }

    fn dispose_batch_handler(&mut self, key: &String) {
        println!(
            "Batch with key '{}' exceeded timeout limit, and will be disposed!",
            key
        );
        self.batch_handler_map.remove_entry(key);
    }

    fn dispose_non_pending_batch_handlers(&mut self) {
        self.batch_handler_map.retain(|_, value| {
            value.status == BatchStatus::PendingConsume
                || value.status == BatchStatus::PendingDatabase
        });
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
