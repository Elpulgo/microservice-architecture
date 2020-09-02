use crate::mqtt_message::Batch;
use std::collections::hash_map::{Entry, HashMap};

pub struct BatchProcessor {
    batch_handler_map: HashMap<String, BatchHandler>,
}

#[derive(Debug)]
pub struct BatchHandler {
    pub key: String,
    pub status: BatchStatus,
    pub batches: Vec<Batch>,
}

#[derive(Debug, PartialEq)]
pub enum BatchStatus {
    Pending,
    TimeoutExceeded,
    Success,
}

pub fn build_batchprocessor() -> BatchProcessor {
    BatchProcessor {
        batch_handler_map: HashMap::<String, BatchHandler>::new(),
    }
}

impl BatchProcessor {
    // FOR DEBUGGING PURPOSES!!
    pub fn print(&mut self) {
        for (key, value) in self.batch_handler_map.iter_mut() {
            println!("{} / {:?}", key, value);
        }
    }
    // END DEBUG!!!

    pub fn add_batch(&mut self, batch: Batch) {

        self.batch_handler_map
            .retain(|_, value| value.status == BatchStatus::Pending);

        match self.batch_handler_map.entry(batch.hash_key().to_string()) {
            Entry::Vacant(entry) => {
                let mut batch_handler = BatchHandler {
                    key: batch.hash_key().to_owned(),
                    status: BatchStatus::Pending,
                    batches: Vec::<Batch>::new(),
                };
                batch_handler.add_batch_value(batch);

                // TODO:
                // batchHandler.StartTimeoutCounter ??

                entry.insert(batch_handler);
            }

            // TODO: If batch has value "LastOfBatch" or similar, we should update the status of the batchhandler
            // and perform the transactions to Redis, and publish a new Ok message in a new queue
            Entry::Occupied(mut e) => e.get_mut().add_batch_value(batch),
        };
    }
}

impl BatchHandler {
    pub fn add_batch_value(&mut self, batch: Batch) {
        match &self.batches.is_empty() {
            true => self.batches = vec![batch],
            false => self.batches.push(batch),
        }
    }
}
