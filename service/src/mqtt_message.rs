use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "PascalCase")]
pub struct RoundTrip {
    key: String,
    value: String,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "PascalCase")]
pub struct Batch {
    hash_key: String,
    key: String,
    value: String,
}

pub trait KeyValue {
    fn key(&self) -> &String;
    fn value(&self) -> &String;
}

macro_rules! impl_KeyValue {
    (for $($t:ty),+) => {
        $(impl KeyValue for $t {
            fn key(&self) -> &String {
                return &self.key;
            }

            fn value(&self) -> &String {
                return &self.value;
            }
        })*
    }
}

impl_KeyValue!(for Batch, RoundTrip);

impl Batch {
    pub fn hash_key(&self) -> &String {
        return &self.hash_key;
    }
}