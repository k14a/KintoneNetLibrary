using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneWriteException<T> : Exception where T : KintoneModelBase<T>, new() {
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
    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(JsonSerializerOptions.Default, indented);
        return JsonSerializer.Serialize(new {
            Message = this.Message,
            Failures = this.Failures.Select(f => new {
                RecordID = f.Record.ID,
                ErrorMessage = f.ErrorMessage,
                Error = f.Error?.ToJson() // KintoneError に ToJson() がある前提
            })
        }, options);
    }
}
