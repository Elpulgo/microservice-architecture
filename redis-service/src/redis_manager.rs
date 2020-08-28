use redis::{self, Client, Connection, RedisResult};

use std::cell::RefCell;
use std::env;
use std::{thread, thread_local, time};

const MAX_CONNECTION_TRIES: i32 = 20;

thread_local! {
   static REDIS_CONNECTION: RefCell<Connection> = RefCell::new(connect().unwrap())
}

pub fn set(key: &str, value: &str) -> RedisResult<()> {
    let result: () = REDIS_CONNECTION.with(|redis_connection_cell| {
        let mut con = redis_connection_cell.borrow_mut();
        let result = redis::cmd("SET")
            .arg(key)
            .arg(value)
            .query(&mut *con)
            .unwrap_or_default();
        return result;
    });

    return Ok(result);
}

pub fn set_hash(hash_key: &str, key: &str, value: &str) -> RedisResult<()> {
    let result: () = REDIS_CONNECTION.with(|redis_connection_cell| {
        let mut con = redis_connection_cell.borrow_mut();
        let result = redis::cmd("HSET")
            .arg(hash_key)
            .arg(key)
            .arg(value)
            .query(&mut *con)
            .unwrap_or_default();
        return result;
    });

    return Ok(result);
}

fn connect() -> RedisResult<Connection> {
    let url = env::var("REDIS_URL").unwrap();
    try_connect(url.as_ref())
}

fn try_connect(redis_url: &str) -> RedisResult<Connection> {
    let mut connection_retries = 0;

    // Open connection.
    loop {
        connection_retries = connection_retries + 1;
        let client = match Client::open(redis_url) {
            Ok(con) => con,
            Err(err) => {
                println!(
                    "Failed to connect to Redis, will retry, {} / {}",
                    connection_retries, MAX_CONNECTION_TRIES
                );

                if connection_retries >= MAX_CONNECTION_TRIES {
                    return Err(err);
                }

                thread::sleep(time::Duration::from_secs(1));
                continue;
            }
        };

        println!("Successfully connected to Redis!");
        return Ok(client.get_connection()?);
    }
}
