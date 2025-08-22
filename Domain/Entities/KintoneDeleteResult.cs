using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Utils;
using KintoneNetLibrary.Domain.Common;

namespace KintoneNetLibrary.Domain.Entities; 

/// <summary>
/// Kintone削除APIのレスポンス結果を表すクラス
/// 成功したレコードIDの一覧と、失敗したレコードの詳細を保持する
/// </summary>
public class KintoneDeleteResult {
    /// <summary>
    /// 成功したレコードIDの一覧
    /// このIDはKintoneからのレスポンスに含まれるもの
    /// </summary>
    public IList<string> Succeeded { get; init; } = new List<string>();

    /// <summary>
    /// 失敗したレコードの詳細情報
    /// 失敗したIDとエラーメッセージを含む
    /// </summary>
    public IList<KintoneDeleteFailure> Failed { get; init; } = [];

    /// <summary>
    /// 成功したレコードが1件もない場合はtrue
    /// </summary>
    public bool HasFailures => this.Failed.Count > 0;

    /// <summary>
    /// 成功したレコードが1件もない場合は例外を投げる
    /// </summary>
    /// <exception cref="KintoneDeleteException">失敗した場合にスローされる</exception>
    public void ThrowIfAnyFailed() {
        if (this.HasFailures) {
            throw new KintoneDeleteException(this.Failed);
        }
    }

    /// <summary>
    /// Kintone削除APIのレスポンスJSONを解析してDeleteResultに変換する
    /// </summary>
    /// <param name="json">Kintone削除APIの生JSONレスポンス</param>
    /// <param name="requestedIDs">削除リクエストしたレコードID一覧</param>
    /// <returns></returns>
    public static KintoneDeleteResult Parse(string json, IEnumerable<string?> requestedIDs) {
        var result = new KintoneDeleteResult();

        if (string.IsNullOrWhiteSpace(json)) {
            // 空なら全て失敗扱いにするか検討
            if (requestedIDs != null) {
                foreach (var id in requestedIDs) {
                    if (!string.IsNullOrEmpty(id)) {
                        result.Failed.Add(new KintoneDeleteFailure {
                            ID = id!,
                            ErrorMessage = "No response from Kintone API"
                        });
                    }
                }
            }
            return result;
        }

        try {
            using var doc = JsonDocument.Parse(json);

            // "ids" : [ "1", "2", ... ]
            if (doc.RootElement.TryGetProperty("ids", out var idsProp) && idsProp.ValueKind == JsonValueKind.Array) {
                foreach (var idEl in idsProp.EnumerateArray()) {
                    if (idEl.ValueKind == JsonValueKind.String) {
                        result.Succeeded.Add(idEl.GetString()!);
                    }
                }
            }

            // "errors" : { "id1": {"message": "...", "id": "id1"}, "id2": {...} }
            if (doc.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object) {
                foreach (var errorProp in errorsProp.EnumerateObject()) {
                    var id = errorProp.Name;
                    var message = string.Empty;

                    if (errorProp.Value.TryGetProperty("message", out var msgProp)) {
                        message = msgProp.GetString() ?? string.Empty;
                    }

                    result.Failed.Add(new KintoneDeleteFailure {
                        ID = id,
                        ErrorMessage = message
                    });
                }
            }

            // 念のため、削除依頼IDのうち結果にも失敗にも含まれないものを失敗に追加（通信エラーなど不明な場合）
            if (requestedIDs != null) {
                var knownIDs = new HashSet<string>(result.Succeeded.Concat(result.Failed.Select(f => f.ID)));
                foreach (var id in requestedIDs) {
                    if (!string.IsNullOrEmpty(id) && !knownIDs.Contains(id)) {
                        result.Failed.Add(new KintoneDeleteFailure {
                            ID = id,
                            ErrorMessage = "No deletion result returned from Kintone API"
                        });
                    }
                }
            }
        } catch (JsonException jex) {
            // JSON解析エラー時は全部失敗扱いに
            if (requestedIDs != null) {
                foreach (var id in requestedIDs) {
                    if (!string.IsNullOrEmpty(id)) {
                        result.Failed.Add(new KintoneDeleteFailure {
                            ID = id!,
                            ErrorMessage = $"Invalid JSON response: {jex.Message}"
                        });
                    }
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Kintone削除APIの失敗理由を表す列挙型
/// RecordNotFound: レコードが見つからない
/// DeleteError: 削除処理中にエラーが発生
/// この列挙型は、KintoneDeleteFailureクラスで使用されます。   
/// </summary>
public enum KintoneDeleteFailureReason {
    /// <summary>
    /// レコードが見つからない
    /// この理由は、削除しようとしたレコードがKintone上に存在しない場合に使用されます。
    /// </summary>
    RecordNotFound,
    /// <summary>
    /// 削除処理中にエラーが発生
    /// この理由は、Kintone APIの内部エラーや通信エラーなど、削除処理が正常に完了しなかった場合に使用されます。
    /// </summary>
    DeleteError,
}

/// <summary>
/// Kintone削除APIの失敗情報を表すクラス
/// このクラスは、削除に失敗したレコードのIDとエラーメッセージを保持します。
/// また、失敗理由を示す列挙型KintoneDeleteFailureReasonを使用して、失敗の詳細な理由を提供します。
/// </summary>
public class KintoneDeleteFailure : IJsonSerializable {
    /// <summary>
    /// レコードID
    /// このIDはKintoneからのレスポンスに含まれるもの
    /// </summary>
    public string ID { get; init; } = string.Empty;

    /// <summary>
    /// エラーメッセージ
    /// このメッセージは、削除に失敗した理由を説明します。
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// 失敗理由
    /// この列挙型は、削除に失敗した理由を示します。
    /// RecordNotFound: レコードが見つからない
    /// DeleteError: 削除処理中にエラーが発生
    /// </summary>
    public KintoneDeleteFailureReason Reason { get; set; }

    /// <summary>
    /// オブジェクトをJSON形式の文字列に変換します。
    /// </summary>
    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, indented);
        return JsonSerializer.Serialize(new { this.ID, this.ErrorMessage, this.Reason }, options);
    }
}
