using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneWriteResult<T> where T : KintoneModelBase<T>, new() {
    public IList<T> Succeeded { get; init; } = [];
    public IList<KintoneWriteFailure<T>> Failed { get; init; } = [];

    public bool HasFailures => this.Failed.Count > 0;

    public void ThrowIfAnyFailed() {
        if (this.HasFailures) {
            throw new KintoneWriteException<T>(this.Failed);
        }
    }
    public void Merge(KintoneWriteResult<T> other) {
        this.Succeeded.AddRange(other.Succeeded);
        this.Failed.AddRange(other.Failed);
    }
}

public class KintoneWriteFailure<T> : IJsonSerializable {
    public T Record { get; init; } = default!;
    public string ErrorMessage { get; init; } = string.Empty;
    public KintoneError? Error { get; init; }

    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, indented);
        return JsonSerializer.Serialize(new { this.Record, this.ErrorMessage, this.Error }, options);
    }
}
