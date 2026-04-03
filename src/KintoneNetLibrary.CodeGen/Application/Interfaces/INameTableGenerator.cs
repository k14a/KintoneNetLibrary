using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// フィールドコードとC#プロパティ名のマッピングテーブルを生成するインターフェース
/// </summary>
public interface INameTableGenerator {
    /// <summary>
    /// Kintoneアプリのスキーマ情報からフィールドコードとC#プロパティ名のマッピングテーブルを生成します。
    /// </summary>
    /// <param name="schema">Kintoneアプリのスキーマ情報</param>
    /// <returns>フィールドコードとC#プロパティ名のマッピングテーブル</returns>
    NameTable Generate(KintoneAppSchema schema);
}