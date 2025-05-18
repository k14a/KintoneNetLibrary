namespace KintoneNetLibrary.Types;

public class PostedFile
{
    public string FilePath { get; set; } = string.Empty;

    public PostedFile(string filePath)
    {
        this.FilePath = filePath;
    }

    // 追加のプロパティやメソッドが必要な場合は、ここに実装します。
}
