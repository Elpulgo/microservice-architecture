use std::env;

pub fn get_amqp_connection() -> String {
    let amqp_url = env::var("AMQP_URL").unwrap();
    return amqp_url;
}

pub fn get_redis_connection() -> String {
    let host_and_port = env::var("REDIS_URL").unwrap();
    return format!("redis://{}", host_and_port);
}

pub fn get_batch_size() -> usize {
    let batch_size_env = env::var("BatchSize").unwrap();
    let batch_size: usize = batch_size_env.parse().unwrap_or(100);
    return batch_size;
}
