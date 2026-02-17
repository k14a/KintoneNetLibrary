namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// アップロードされたファイルを表すエンティティ
/// </summary>
/// <param name="filePath">ファイルのパス</param>
public class PostedFile(string filePath) {
    /// <summary>
    /// ファイルのパス
    /// </summary>
    public string FilePath { get; set; } = filePath;

    // 追加のプロパティやメソッドが必要な場合は、ここに実装します。
}
