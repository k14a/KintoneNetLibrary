using System.Collections.Generic;
using System.Linq;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneWriteResult<T> where T : KintoneModelBase
{
    public IList<T> Succeeded { get; init; } = [];
    public IList<KintoneWriteFailure<T>> Failed { get; init; } = [];

    public bool HasFailures => Failed.Count > 0;

    public void ThrowIfAnyFailed()
    {
        if (HasFailures)
        {
            throw new KintoneWriteException<T>(Failed);
        }
    }
}

public class KintoneWriteFailure<T>
{
    public T Record { get; init; } = default!;
    public string ErrorMessage { get; init; } = string.Empty;
    public KintoneError? Error { get; init; }
}
