using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneへの書き込み結果を表すクラス
/// </summary>
/// <typeparam name="T"></typeparam>
public class KintoneWriteResult<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// 正常に書き込みが完了したレコードの一覧
    /// /// </summary>
    public IList<T> Succeeded { get; init; } = [];
    /// <summary>
    /// 書き込みに失敗したレコードの一覧
    /// </summary>
    public IList<KintoneWriteFailure<T>> Failed { get; init; } = [];

    /// <summary>
    /// 書き込みに失敗したレコードが存在するかどうか
    /// </summary>
    public bool HasFailures => this.Failed.Count > 0;
    /// <summary>
    /// 書き込みに失敗したレコードが存在する場合、例外をスローします
    /// </summary>
    /// <exception cref="KintoneWriteException{T}">書き込みに失敗したレコードが存在する場合にスローされる例外</exception>
    public void ThrowIfAnyFailed() {
        if (this.HasFailures) {
            throw new KintoneWriteException<T>(this.Failed);
        }
    }
    /// <summary>
    /// 他のKintoneWriteResultの内容をマージします
    /// </summary>
    /// <param name="other"></param>
    public void Merge(KintoneWriteResult<T> other) {
        this.Succeeded.AddRange(other.Succeeded);
        this.Failed.AddRange(other.Failed);
    }
}

/// <summary>
/// Kintoneへの書き込みに失敗したレコードの情報を表すクラス
/// </summary>
/// <typeparam name="T">Kintoneモデルの型</typeparam>
public class KintoneWriteFailure<T> : IJsonSerializable {
    /// <summary>
    /// 書き込みに失敗したレコードの情報
    /// </summary>
    public T Record { get; init; } = default!;
    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    /// <summary>
    /// Kintoneのエラー情報
    /// </summary>
    public KintoneError? Error { get; init; }

    /// <summary>
    /// 例外情報をJSON形式で取得します。
    /// </summary>
    /// <param name="indented">インデントを有効にするかどうか</param>
    /// <returns>JSON形式の例外情報</returns>
    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, indented);
        return JsonSerializer.Serialize(new { this.Record, this.ErrorMessage, this.Error }, options);
    }
}
