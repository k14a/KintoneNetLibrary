using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneException : Exception {
    [JsonPropertyName("error")]
    public KintoneError? Error { get; set; }

    [JsonPropertyName("message")]
    public override string Message =>
        !string.IsNullOrEmpty(this.Error?.Summary)
            ? this.Error!.Summary
            : !string.IsNullOrEmpty(this.Error?.Message)
                ? this.Error!.Message
                : base.Message;

    [JsonPropertyName("detail")]
    public string Detail => this.Error?.ToString() ?? base.Message;

    public KintoneException() { }
    public KintoneException(KintoneError error) : base(error.Summary) { this.Error = error; }
    public KintoneException(string message) : base(message) { }
    public KintoneException(string message, Exception innerException) : base(message, innerException) { }
    public KintoneException(KintoneError error, Exception innerException) : base(error.Summary, innerException) {
        this.Error = error;
    }

    public override string ToString() {
        if (this.Error != null) {
            return $"KintoneException: {this.Error.Summary}\nDetails: {this.Error}";
        }
        return base.ToString();
    }
}
