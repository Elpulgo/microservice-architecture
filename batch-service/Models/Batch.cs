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
            int batchSize,
            bool isLastInBatch = false)
        {
            HashKey = hashKey;
            Key = key;
            Value = value;
            BatchSize = batchSize;
            IsLastInBatch = isLastInBatch;
        }

        public Guid HashKey { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public int BatchSize { get; set; }

        public bool IsLastInBatch { get; set; }
    }
}
