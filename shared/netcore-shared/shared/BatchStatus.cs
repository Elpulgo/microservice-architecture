using System;

namespace NetCoreShared
{
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
