using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone APIのエラー情報を表すクラス
/// Kintoneからのエラーメッセージ、コード、Id、詳細情報を保持する
/// </summary>
public class KintoneError {
    /// <summary>
    /// エラーメッセージ
    /// Kintoneからのレスポンスに含まれるメッセージ
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// エラーコード
    /// Kintoneからのレスポンスに含まれるエラーコード
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// レコードId
    /// Kintoneからのレスポンスに含まれるレコードId
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// エラーの詳細情報
    /// Kintoneからのレスポンスに含まれる詳細情報
    /// </summary>
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    /// <summary>
    /// エラーの詳細情報のリスト
    /// Kintoneからのレスポンスに含まれる詳細情報のリスト
    /// </summary>
    [JsonPropertyName("details")]
    public IList<KintoneErrorDetail> Details { get; set; } = [];

    /// <summary>
    /// エラーの概要
    /// Kintoneからのレスポンスに含まれる概要
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// エラーの詳細情報が存在するかどうか
    /// Kintoneからのレスポンスに詳細情報が含まれる場合はtrue
    /// </summary>
    [JsonIgnore]
    public bool HasDetails => this.Details is { Count: > 0 };

    /// <summary>
    /// コンストラクタ
    /// 初期化用の引数を受け取らない
    /// </summary>
    public KintoneError() { }

    /// <summary>
    /// コンストラクタ
    /// エラーメッセージを受け取る
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    public KintoneError(string message) { this.Message = message; }

    /// <summary>
    /// Kintone APIのエラー情報を文字列に変換します。
    /// </summary>
    /// <returns>エラー情報を表す文字列</returns>
    public override string ToString() {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"[KintoneError] Code: {this.Code}, Message: {this.Message}");
        if (!string.IsNullOrEmpty(this.Summary)) { builder.AppendLine($"Summary: {this.Summary}"); }
        if (!string.IsNullOrEmpty(this.Id)) { builder.AppendLine($"Id: {this.Id}"); }
        if (this.HasDetails) {
            builder.AppendLine("Details:");
            foreach (var detail in this.Details) {
                builder.AppendLine($"  - {detail}");
            }
        }
        if (this.Errors != null) { builder.AppendLine($"Errors: {this.Errors}"); }

        return builder.ToString();
    }

    /// <summary>
    /// オブジェクトをJSON形式の文字列に変換します。
    /// </summary>
    /// <param name="indented">インデントを付けるかどうか</param>
    /// <returns>JSON形式の文字列</returns>
    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(JsonSerializerOptions.Default, indented);
        return JsonSerializer.Serialize(this, options);
    }
}

/// <summary>
/// Kintone APIのエラー詳細情報を表すクラス
/// このクラスは、エラーのインデックスとメッセージのリストを保持します。
/// </summary>
public class KintoneErrorDetail {
    /// <summary>
    /// エラーのインデックス
    /// このインデックスは、エラーが発生した位置を示します。
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// エラーメッセージのリスト
    /// このリストは、エラーの詳細なメッセージを保持します。
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<string> Messages { get; set; } = [];

    /// <summary>
    /// エラー詳細情報の文字列表現
    /// このメソッドは、エラーのインデックスとメッセージのリストを文字列として返します。
    /// 例: "Index 0: Error message 1, Error message 2"
    /// </summary>
    /// <returns>エラー詳細情報を表す文字列</returns>
    public override string ToString() {
        return $"Index {this.Index}: {string.Join(", ", this.Messages)}";
    }
}
