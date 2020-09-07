using NetCoreShared;

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
}