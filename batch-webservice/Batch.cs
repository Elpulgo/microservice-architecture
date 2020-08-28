using System;

namespace batch_webservice
{
    public class Batch
    {
        public Batch()
        {

        }

        public Batch(Guid hashKey, string key, string value)
        {
            HashKey = hashKey;
            Key = key;
            Value = value;
        }

        public Guid HashKey { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
