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
        PendingConsume = 0,
        PendingDatabase = 1,
        DatabaseOperationFailed = 2,
        Invalid = 3,
        TimeoutExceeded = 4,
        Done = 5,
    }
}