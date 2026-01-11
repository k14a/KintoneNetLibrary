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
    /// <param name="name"></param>
    /// <param name="subTable"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    GeneratedSubTableModel EmitSubTable(string name, KintoneSubTableSchema subTable, CodeEmitterOptions options);
}
