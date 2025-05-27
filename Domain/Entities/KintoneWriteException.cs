using System;
using System.Collections.Generic;
using System.Text;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneWriteException<T> : Exception where T : KintoneModelBase {
    public IList<KintoneWriteFailure<T>> Failures { get; }

    public KintoneWriteException(IList<KintoneWriteFailure<T>> failures) : base(BuildMessage(failures)) {
        this.Failures = failures;
    }

    private static string BuildMessage(IList<KintoneWriteFailure<T>> failures) {
        var sb = new StringBuilder();
        sb.AppendLine("One or more records failed to be written to Kintone:");
        foreach (var fail in failures) {
            sb.AppendLine($"- ID: {fail.Record.ID}, Error: {fail.ErrorMessage}");
        }
        return sb.ToString();
    }
}
