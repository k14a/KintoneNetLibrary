using System.Text;
using System.Text.Json;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述
/// <summary>
/// Kintoneへの書き込み処理で発生した例外を表します。
/// </summary>
/// <typeparam name="T">Kintoneモデルの型</typeparam>
public class KintoneWriteException<T> : Exception where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// 書き込みに失敗したレコードの一覧を取得します。
    /// </summary>
    public IList<KintoneWriteFailure<T>> Failures { get; }

    /// <summary>
    /// 新しいKintoneWriteExceptionのインスタンスを初期化します。
    /// </summary>
    /// <param name="failures"></param>
    public KintoneWriteException(IList<KintoneWriteFailure<T>> failures) : base(BuildMessage(failures)) {
        this.Failures = failures;
        this.Data["Json"] = this.ToJson();
    }

    /// <summary>
    /// 例外メッセージを構築します。
    /// </summary>
    /// <param name="failures">書き込みに失敗したレコードの一覧</param>
    /// <returns>構築された例外メッセージ</returns>
    private static string BuildMessage(IList<KintoneWriteFailure<T>> failures) {
        var sb = new StringBuilder();
        sb.AppendLine("One or more records failed to be written to Kintone:");
        foreach (var fail in failures) {
            sb.AppendLine($"- ID: {fail.Record.ID}, Error: {fail.ErrorMessage}");
        }
        return sb.ToString();
    }
    /// <summary>
    /// 例外情報をJSON形式で取得します。
    /// </summary>
    /// <param name="indented">インデントを有効にするかどうか</param>
    /// <returns>JSON形式の例外情報</returns>
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
