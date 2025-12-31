namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class MimeTypeWrapper {
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

    public static bool IsAcceptableContentType(string? contentType) {
        return contentType != null && AcceptableContentTypes.Contains(contentType);
    }
}
