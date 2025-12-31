namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのファイルを表すクラス
/// このクラスは、Kintoneのファイルのコンテンツタイプ、ファイルキー、名前、サイズを保持します。
/// </summary>
public class KintoneFile
{
    /// <summary>
    /// ファイルのコンテンツタイプ  
    /// このプロパティは、KintoneのファイルのMIMEタイプを表します。
    /// 例: "image/png", "application/pdf" など
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// ファイルキー
    /// このプロパティは、Kintoneのファイルの一意の識別子を表します。
    /// ファイルをアップロードした後にKintoneから返されるキーです。
    /// </summary>
    public string FileKey { get; set; } = string.Empty;

    /// <summary>
    /// ファイル名
    /// このプロパティは、Kintoneのファイルの名前を表します。
    /// 例: "image.png", "document.pdf" など
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ファイルのサイズ
    /// このプロパティは、Kintoneのファイルのサイズをバイト単位で表します。
    /// 例: 1024 (1KB), 2048000 (2MB) など
    /// </summary>
    public long Size { get; set; }
}
