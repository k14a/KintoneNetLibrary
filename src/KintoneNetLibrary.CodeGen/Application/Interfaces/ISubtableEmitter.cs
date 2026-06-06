using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// サブテーブルエミッターインターフェース
/// </summary>
public interface ISubTableEmitter {
    /// <summary>
    /// サブテーブルモデルを生成する
    /// </summary>
    /// <param name="name">サブテーブルの名前</param>
    /// <param name="subTable">サブテーブルのスキーマ情報</param>
    /// <param name="options">コード生成オプション</param>
    /// <returns>生成されたサブテーブルモデル</returns>
    GeneratedSubTableModel EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options, INameConverter nameConverter);
}
