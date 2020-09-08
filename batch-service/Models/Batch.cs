using System;

namespace batch_webservice
{
    public class Batch
    {
        public Batch()
        {

        }

        public Batch(
            Guid hashKey,
            string key,
            string value,
            bool isLastInBatch = false)
        {
            HashKey = hashKey;
            Key = key;
            Value = value;
            IsLastInBatch = isLastInBatch;
        }

        public Guid HashKey { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsLastInBatch { get; set; }
    }
}
