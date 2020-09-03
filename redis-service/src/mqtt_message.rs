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
    // FOR DEBUG!!! REMOVE pub!!!
    pub hash_key: String,
    pub key: String,
    pub value: String,
    pub is_last_in_batch: bool,
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

    pub fn is_last_in_batch(&self) -> &bool {
        return &self.is_last_in_batch;
    }
}
