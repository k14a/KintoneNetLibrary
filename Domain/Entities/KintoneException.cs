using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone APIの例外を表すクラス
/// Kintoneからのエラー情報を含む
/// </summary>
public class KintoneException : Exception {
    /// <summary>
    /// Kintone APIのエラー情報
    /// このプロパティは、Kintoneからのエラー情報を保持します。
    /// </summary>
    [JsonPropertyName("error")]
    public KintoneError? Error { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// このプロパティは、Kintoneからのエラーメッセージを返します。
    /// もしエラー情報が存在しない場合は、基底クラスのメッセージを返します。
    /// </summary>
    [JsonPropertyName("message")]
    public override string Message =>
        !string.IsNullOrEmpty(this.Error?.Summary)
            ? this.Error!.Summary
            : !string.IsNullOrEmpty(this.Error?.Message)
                ? this.Error!.Message
                : base.Message;

    /// <summary>
    /// エラーの詳細情報
    /// このプロパティは、Kintoneからのエラー詳細情報を返します。
    /// もしエラー情報が存在しない場合は、基底クラスのメッセージを返します。
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail => this.Error?.ToString() ?? base.Message;

    /// <summary>
    /// KintoneExceptionのコンストラクタ
    /// このコンストラクタは、エラー情報を初期化します。
    /// </summary>
    public KintoneException() { }

    /// <summary>
    /// KintoneExceptionのコンストラクタ
    /// このコンストラクタは、エラー情報を指定して初期化します。
    /// </summary>
    /// <param name="error">Kintone APIのエラー情報</param>
    public KintoneException(KintoneError error) : base(error.Summary) { this.Error = error; }

    /// <summary>
    /// KintoneExceptionのコンストラクタ
    /// このコンストラクタは、エラーメッセージを指定して初期化します。  
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    public KintoneException(string message) : base(message) { }

    /// <summary>
    /// KintoneExceptionのコンストラクタ
    /// このコンストラクタは、エラーメッセージと内部例外を指定して初期化します。
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <param name="innerException">内部例外</param>
    public KintoneException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// KintoneExceptionのコンストラクタ
    /// このコンストラクタは、Kintone APIのエラー情報と内部例外を指定して初期化します。
    /// </summary>
    /// <param name="error">Kintone APIのエラー情報</param>
    /// <param name="innerException">内部例外</param>
    public KintoneException(KintoneError error, Exception innerException) : base(error.Summary, innerException) {
        this.Error = error;
    }

    /// <summary>
    /// KintoneExceptionの文字列表現
    /// このメソッドは、エラー情報を文字列として返します。
    /// </summary>
    /// <returns>エラー情報を表す文字列</returns>
    public override string ToString() {
        if (this.Error != null) {
            return $"KintoneException: {this.Error.Summary}\nDetails: {this.Error}";
        }
        return base.ToString();
    }
}
