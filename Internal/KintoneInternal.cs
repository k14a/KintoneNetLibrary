using System.Runtime.CompilerServices;

namespace KintoneNetLibrary.Internal;

[CompilerGenerated] // 明示的にツール生成 or 非推奨用途と示す
internal static class KintoneRequestBuilder {
    internal static Uri BuildRequestUri(Uri baseUri, string path, int appID, string? query = null) {
        var builder = new UriBuilder(new Uri(baseUri, path));
        var parameters = new List<string> { $"app={appID}" };

        if (!string.IsNullOrEmpty(query)) {
            parameters.Add(query);
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }
}
internal static class KintoneApiEndpoints {
    public const string GetSingleRecord = "record.json";
    public const string GetRecords = "records.json";
    public const string AddRecord = "record.json";
    public const string UpdateRecord = "record.json";
    public const string DeleteRecord = "record.json";
    public const string Cursor = "records/cursor.json";
    // 他にも必要に応じて追加
}

