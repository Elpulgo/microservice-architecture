use chrono::{NaiveDateTime};
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
    #[serde(with = "custom_date_format")]
    timestamp: NaiveDateTime,
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
    pub fn timestamp(&self) -> &NaiveDateTime {
        return &self.timestamp;
    }
}

mod custom_date_format {
    use chrono::{Utc, NaiveDateTime, DateTime};
    use serde::{self, Deserialize, Deserializer, Serializer, de::Error};

    // The signature of a serialize_with function must follow the pattern:
    //
    //    fn serialize<S>(&T, S) -> Result<S::Ok, S::Error>
    //    where
    //        S: Serializer
    //
    // although it may also be generic over the input types T.
    pub fn serialize<S>(date: &NaiveDateTime, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {
        let s = DateTime::<Utc>::from_utc(*date, Utc).to_rfc3339();
        serializer.serialize_str(&s)
    }
    // The signature of a deserialize_with function must follow the pattern:
    //
    //    fn deserialize<'de, D>(D) -> Result<T, D::Error>
    //    where
    //        D: Deserializer<'de>
    //
    // although it may also be generic over the output types T.
    pub fn deserialize<'de, D>(deserializer: D) -> Result<NaiveDateTime, D::Error>
    where
        D: Deserializer<'de>,
    {
        let s = String::deserialize(deserializer)?;
        Ok(DateTime::parse_from_rfc3339(&s).map_err(D::Error::custom)?.naive_utc())
    }
}
