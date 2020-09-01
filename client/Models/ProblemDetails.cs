using System.Text.Json.Serialization;

namespace client
{
    // This class is a helper to make use of our custom error handler
    // which returns a ProblemDetail object, but can't leverage that since
    // we don't use Microsoft.Core.Mvc in our Blazor project.
    public class ProblemDetails
    {
        public ProblemDetails()
        {

        }

        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("detail")]
        public string Detail { get; set; }
        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}