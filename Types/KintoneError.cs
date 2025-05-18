using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Types;

public class KintoneError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    [JsonPropertyName("details")]
    public IList<KintoneErrorDetail> Details { get; set; } = [];

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    public KintoneError() { }

    public KintoneError(string message)
    {
        this.Message = message;
    }
}

public class KintoneErrorDetail
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("messages")]
    public IList<string> Messages { get; set; } = [];
}
