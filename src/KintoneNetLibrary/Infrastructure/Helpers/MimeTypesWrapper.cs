namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// MIMEタイプを扱うためのラッパークラス
/// </summary>
public static class MimeTypeWrapper {
    /// <summary>
    /// 許容されるContent-Typeのセット
    /// </summary>
    private static readonly HashSet<string> AcceptableContentTypes = [
        "application/octet-stream",
        "text/plain",
        "application/pdf",
        "application/zip",
        "image/png",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/x-zip-compressed",
    ];

    /// <summary>
    /// 指定されたContent-Typeが許容されるかどうかを判定します
    /// </summary>
    /// <param name="contentType">判定対象のContent-Type</param>
    /// <returns>許容されるContent-Typeであればtrue、それ以外はfalse</returns>
    public static bool IsAcceptableContentType(string? contentType) {
        return contentType != null && AcceptableContentTypes.Contains(contentType);
    }
}
