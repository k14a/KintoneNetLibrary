using System.Text;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone レコード削除に失敗した場合の例外
/// </summary>
/// <remarks>
/// KintoneDeleteExceptionのコンストラクタ
/// </remarks>
/// <param name="failures">削除に失敗したレコードの情報</param>
/// <exception cref="ArgumentNullException">failuresがnullの場合</exception>
/// <exception cref="ArgumentException">failuresが空のリストの場合</exception>
/// <exception cref="InvalidOperationException">failuresに無効なデータが含まれている場合</exception>
/// <exception cref="Exception">その他の例外</exception>
public class KintoneDeleteException(IList<KintoneDeleteFailure> failures) : Exception(BuildMessage(failures)) {
    /// <summary>
    /// 削除に失敗したレコードの情報を保持するリスト
    /// </summary>
    public IList<KintoneDeleteFailure> Failures { get; } = failures;

    /// <summary>
    /// KintoneDeleteExceptionのコンストラクタ
    /// </summary>
    /// <param name="failures">削除に失敗したレコードの情報</param>
    /// <returns>新しいKintoneDeleteExceptionのインスタンス</returns>
    /// <exception cref="ArgumentNullException">failuresがnullの場合</exception>
    /// <exception cref="ArgumentException">failuresが空のリストの場合</exception>
    /// <exception cref="InvalidOperationException">failuresに無効なデータが含まれている場合</exception>
    /// <exception cref="Exception">その他の例外</exception>
    private static string BuildMessage(IList<KintoneDeleteFailure> failures) {
        var sb = new StringBuilder();
        sb.AppendLine("一部のレコードの削除に失敗しました:");
        foreach (var failure in failures) {
            sb.AppendLine($"- Id: {failure.Id}, Error: {failure.ErrorMessage}");
        }
        return sb.ToString();
    }
}
