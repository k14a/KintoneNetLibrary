namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述
/// <summary>
/// アップロードされたファイルを表すエンティティ
/// </summary>
/// <param name="filePath"></param>
public class PostedFile(string filePath) {
    /// <summary>
    /// ファイルのパス
    /// </summary>
    public string FilePath { get; set; } = filePath;

    // 追加のプロパティやメソッドが必要な場合は、ここに実装します。
}
