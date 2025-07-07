using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneApiFactory {
    /// <summary>
    /// KintoneModelBase のプロパティから API インスタンスを生成します。
    /// </summary>
    KintoneApi Create(KintoneModelBase model);
    KintoneApi Create(KintoneAccount account, int AppID);
}
