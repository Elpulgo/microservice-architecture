use redis::{self, Client, Connection, RedisResult};

use std::cell::RefCell;
use std::env;
use std::{thread, thread_local, time};

const MAX_CONNECTION_TRIES: i32 = 20;

thread_local! {
   static REDIS_CONNECTION: RefCell<Connection> = RefCell::new(connect().unwrap())
}

// /// This is a pretty stupid example that demonstrates how to create a large
// /// set through a pipeline and how to iterate over it through implied
// /// cursors.
// fn do_show_scanning(con: &mut redis::Connection) -> redis::RedisResult<()> {
//     // This makes a large pipeline of commands.  Because the pipeline is
//     // modified in place we can just ignore the return value upon the end
//     // of each iteration.
//     let mut pipe = redis::pipe();
//     for num in 0..1000 {
//         pipe.cmd("SADD").arg("my_set").arg(num).ignore();
//     }

//     // since we don't care about the return value of the pipeline we can
//     // just cast it into the unit type.
//     pipe.query(con)?;

//     // since rust currently does not track temporaries for us, we need to
//     // store it in a local variable.
//     let mut cmd = redis::cmd("SSCAN");
//     cmd.arg("my_set").cursor_arg(0);

//     // as a simple exercise we just sum up the iterator.  Since the fold
//     // method carries an initial value we do not need to define the
//     // type of the iterator, rust will figure "int" out for us.
//     let sum: i32 = cmd.iter::<i32>(con)?.sum();

//     println!("The sum of all numbers in the set 0-1000: {}", sum);

//     Ok(())
// }

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
