using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace KintoneNetLibrary.Domain.Entities {
    public class KintoneDeleteResult {
        public IList<string> DeletedIDs { get; init; } = new List<string>();
        public IList<KintoneDeleteFailure> FailedIDs { get; init; } = new List<KintoneDeleteFailure>();

        public bool HasFailures => FailedIDs.Count > 0;

        public void ThrowIfAnyFailed() {
            if (HasFailures) {
                throw new KintoneDeleteException(FailedIDs);
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
                            result.FailedIDs.Add(new KintoneDeleteFailure {
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
                            result.DeletedIDs.Add(idEl.GetString()!);
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

                        result.FailedIDs.Add(new KintoneDeleteFailure {
                            ID = id,
                            ErrorMessage = message
                        });
                    }
                }

                // 念のため、削除依頼IDのうち結果にも失敗にも含まれないものを失敗に追加（通信エラーなど不明な場合）
                if (requestedIDs != null) {
                    var knownIDs = new HashSet<string>(result.DeletedIDs.Concat(result.FailedIDs.Select(f => f.ID)));
                    foreach (var id in requestedIDs) {
                        if (!string.IsNullOrEmpty(id) && !knownIDs.Contains(id)) {
                            result.FailedIDs.Add(new KintoneDeleteFailure {
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
                            result.FailedIDs.Add(new KintoneDeleteFailure {
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

    public enum KintoneDeleteFailureReason {
        RecordNotFound,
        DeleteError,
    }

    public class KintoneDeleteFailure {
        public string ID { get; init; } = string.Empty;
        public string ErrorMessage { get; init; } = string.Empty;
        public KintoneDeleteFailureReason Reason { get; set; }
    }
}
