using System.Text;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneDeleteException : Exception {
    public IList<KintoneDeleteFailure> Failures { get; }

    public KintoneDeleteException(IList<KintoneDeleteFailure> failures)
        : base(BuildMessage(failures)) {
        this.Failures = failures;
    }

    private static string BuildMessage(IList<KintoneDeleteFailure> failures) {
        var sb = new StringBuilder();
        sb.AppendLine("一部のレコードの削除に失敗しました:");
        foreach (var failure in failures) {
            sb.AppendLine($"- ID: {failure.ID}, Error: {failure.ErrorMessage}");
        }
        return sb.ToString();
    }
}
