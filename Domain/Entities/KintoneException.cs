namespace KintoneNetLibrary.Domain.Entities;

public class KintoneException : Exception
{
    public KintoneError? Error { get; set; }
    public override string Message => Error?.Summary ?? base.Message;
    public string Detail => Error?.ToString() ?? base.Message;

    public KintoneException() { }
    public KintoneException(KintoneError error) : base(error.Summary) { this.Error = error; }
    public KintoneException(string message) : base(message) { }
    public KintoneException(string message, Exception innerException) : base(message, innerException) { }
    public KintoneException(KintoneError error, Exception innerException) : base(error.Summary, innerException) { this.Error = error; }

    public override string ToString() {
        if (Error != null) {
            return $"KintoneException: {Error.Summary}\nDetails: {Error}";
        }
        return base.ToString();
    }
}
