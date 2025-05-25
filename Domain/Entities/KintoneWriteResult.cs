using System.Collections.Generic;
using System.Linq;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneWriteResult<T> where T : KintoneModelBase
{
    public IList<T> Succeeded { get; init; } = new List<T>();
    public IList<KintoneWriteFailure<T>> Failed { get; init; } = new List<KintoneWriteFailure<T>>();

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
