using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
/// <summary>
/// Kintone API ファクトリ インターフェイス
/// </summary>
public interface IKintoneApiFactory {
    /// <summary>
    /// KintoneModelBase のプロパティから API インスタンスを生成します。
    /// </summary>
    // KintoneApi Create(KintoneModelBase model);
    KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new();
}
