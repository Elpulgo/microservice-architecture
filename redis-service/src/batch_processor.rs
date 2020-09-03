use crate::mqtt_message::Batch;
use crate::redis_manager;
use std::borrow::Borrow;
use std::collections::hash_map::{Entry, HashMap};
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::Duration;

pub struct BatchProcessor {
    batch_handler_map: HashMap<String, BatchHandler>,
}

#[derive(Debug)]
pub struct BatchHandler {
    pub key: String,
    pub status: Arc<Mutex<BatchStatus>>,
    pub batches: Vec<Batch>,
    pub timeout_thread: Option<thread::JoinHandle<()>>,
}

#[derive(Debug, PartialEq, Copy, Clone)]
pub enum BatchStatus {
    PendingConsume,
    PendingDatabase,
    TimeoutExceeded,
    Done,
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
        println!("add_batch _before_ retain {}", batch.key);
        println!("length of map is: {}", self.batch_handler_map.len());

        // self.batch_handler_map.retain(|_, value| {
        //     *value.status.clone().lock().unwrap() == BatchStatus::PendingConsume || 
        //     *value.status.clone().lock().unwrap() == BatchStatus::PendingDatabase
        // });

        println!("add_batch _after_ retain {}", batch.key);

        let key = &batch.hash_key().to_string();
        match self.batch_handler_map.entry(String::from(key)) {
            Entry::Vacant(entry) => {
                println!("add_batch entry is _vacant_  {}", key);

                let mut batch_handler = BatchHandler {
                    key: batch.hash_key().to_owned(),
                    status: Arc::from(Mutex::from(BatchStatus::PendingConsume)),
                    batches: Vec::<Batch>::new(),
                    timeout_thread: None,
                };
                println!("add_batch entry is _vacant_add_batch_value  {}", key);

                batch_handler.add_batch_value(batch);
                println!("add_batch entry is _vacant_init_timeout  {}", key);

                batch_handler.init_timeout_watch();
                println!("add_batch entry is _vacant_AFTER_init_timeout  {}", key);

                // TODO:
                // batchHandler.StartTimeoutCounter ??

                entry.insert(batch_handler);
                println!("add_batch entry is _vacant_AFTER_insert  {}", key);

            }

            // TODO: If batch has value "LastOfBatch" or similar, we should update the status of the batchhandler
            // and perform the transactions to Redis, and publish a new Ok message in a new queue
            Entry::Occupied(mut e) => e.get_mut().add_batch_value(batch),
        };

        if let Some(handler) = self.batch_handler_map.get_mut(key) {
            // TODO: Add batches to database..
            // and publish to new queue that we succeded..
            if *handler.status.clone().lock().unwrap() == BatchStatus::PendingDatabase {
                println!(
                    "handler with key should now add batch to database: {}",
                    handler.key
                );
                // redis_manager::set_hash(hash_key: &str, key: &str, value: &str)
            }
        }
    }
}

impl BatchHandler {
    pub fn init_timeout_watch(&mut self) {
        println!("Entering init_timeout_watch for: {}", self.key);

        let mut status = self.status.clone();

        let key = self.key.clone();

        let timeout_limit = 10;

        let timeout_thread = thread::spawn(move || {
            thread::sleep(Duration::from_secs(timeout_limit));

            if *status.lock().unwrap() != BatchStatus::Done {
                println!("Timeout exceeded for: {}", key);
                *status.lock().unwrap() = BatchStatus::TimeoutExceeded;
                
                // *status.lock() = Mutex::from(BatchStatus::TimeoutExceeded)); // BatchStatus::TimeoutExceeded;
                
                println!("Succefffully set timeoutexceeded status! {}", key);
                // self.status.lock().unwrap() = BatchStatus::TimeoutExceeded;
                // self.status = BatchStatus::TimeoutExceeded;
                // self.change_status(BatchStatus::TimeoutExceeded);
            }
        });

        println!("Will return from init_timeout_watch for: {}", self.key);

        self.timeout_thread = Some(timeout_thread);
    }

    pub fn add_batch_value(&mut self, batch: Batch) {
        match batch.is_last_in_batch() {
            true => self.change_status(BatchStatus::PendingDatabase),
            false => self.change_status(BatchStatus::PendingConsume),
        }

        match &self.batches.is_empty() {
            true => self.batches = vec![batch],
            false => self.batches.push(batch),
        }
    }

    fn change_status(&mut self, status: BatchStatus) {
        self.status = Arc::from(Mutex::from(status));
    }
}

impl Drop for BatchHandler {
    fn drop(&mut self) {
        println!("Dropping batchhandler with key: {}", self.key);
        match self.timeout_thread.take().unwrap().join() {
            Ok(_) => println!("Sucessfully aborted thread!"),
            Err(e) => println!("Failed to abort thread: {:?}", e),
        };
    }
}
