namespace batch_webservice
{
    public class BatchReply
    {
        public BatchReply()
        {

        }

        public BatchStatus Status { get; set; }

        public string Key { get; set; }
    }

    public enum BatchStatus
    {
        None = 0,
        PendingConsume = 1,
        PendingDatabase = 2,
        DatabaseOperationFailed = 3,
        Invalid = 4,
        TimeoutExceeded = 5,
        Done = 6,
    }
}