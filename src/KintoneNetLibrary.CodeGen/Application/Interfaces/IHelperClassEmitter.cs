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
    /// <param name="options"></param>
    /// <returns></returns>
    IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options);
}
