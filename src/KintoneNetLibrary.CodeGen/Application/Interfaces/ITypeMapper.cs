using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 型マッパーインターフェース
/// </summary>
public interface ITypeMapper {
    /// <summary>
    /// Kintone フィールドを C# 型にマップする
    /// </summary>
    /// <param name="field">Kintone フィールドのメタデータ</param>
    /// <returns>対応する C# 型の名前</returns>
    string Map(KintoneFieldMetadata field);

    /// <summary>
    /// Kintone フィールドスキーマを C# 型にマップする
    /// </summary>
    /// <param name="field">Kintone フィールドのスキーマ情報</param>
    /// <param name="useLibrary">ライブラリを使用するかどうか</param>
    /// <param name="subTableClassName">サブテーブルのクラス名</param>
    /// <returns>対応する C# 型の名前</returns>
    string MapType(KintoneFieldSchema field, bool useLibrary, string subTableClassName = "");
}
