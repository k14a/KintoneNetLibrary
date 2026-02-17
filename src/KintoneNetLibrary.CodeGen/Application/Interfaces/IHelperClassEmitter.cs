using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 補助クラスエミッターインターフェース
/// </summary>
public interface IHelperClassEmitter {
    /// <summary>
    /// ヘルパークラス群を生成する
    /// </summary>
    /// <param name="options">コード生成オプション</param>
    /// <returns>生成されたヘルパークラスのコレクション</returns>
    IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options);
}
