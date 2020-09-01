using System.Text;
using System.Text.Json;

namespace batch_webservice
{
    public static class BatchExtension
    {
        public static byte[] ToByteArray(this Batch batch)
            => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(batch));
    }
}
