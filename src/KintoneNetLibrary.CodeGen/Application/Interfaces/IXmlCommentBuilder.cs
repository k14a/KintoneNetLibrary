using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// XML コメントビルダーインターフェース
/// </summary>
public interface IXmlCommentBuilder {
    /// <summary>
    /// フィールド用 XML コメントを生成する
    /// </summary>
    /// <param name="field">Kintone フィールドのスキーマ情報</param>
    /// <returns>生成された XML コメント</returns>
    string BuildForField(KintoneFieldSchema field);

    /// <summary>
    /// サブテーブル用 XML コメントを生成する
    /// </summary>
    /// <param name="subTable">Kintone サブテーブルのスキーマ情報</param>
    /// <returns>生成された XML コメント</returns>
    string BuildForSubTable(KintoneSubTableSchema subTable);
}