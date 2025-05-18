using System;
using KintoneNetLibrary.Types;

namespace KintoneNetLibrary;

public class KintoneException : Exception
{
    public KintoneError? Error { get; set; }

    private readonly string _message = string.Empty;

    public override string Message => Error != null ? Error.Summary : _message;

    public string Detail => Error != null ? Error.ToString() : _message;

    public KintoneException() { }

    public KintoneException(KintoneError error) {
        this.Error = error;
    }

    public KintoneException(string message) : base(message) {
        this._message = message;
    }

    public KintoneException(string message, Exception innerException) : base(message, innerException) {
        this._message = message;
    }
}
