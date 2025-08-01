using System.Runtime.CompilerServices;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

public abstract class KintoneAccessBase {
    /// <summary>
    /// Kintone 接続情報を生成する
    /// </summary>
    public abstract KintoneAccount ToKintoneAccount();

    // /// <summary>
    // /// AppID を直接持たせる場合はこちらでもOK
    // /// </summary>
    // public virtual int AppID => ToKintoneAccount().GuestSpaceId == 0 ? ExtractAppID() : ExtractGuestAppID();
    // public virtual int AppID { get; set; }
    public virtual string Domain { get; set; }
    public virtual string BasicAuthUser { get; set; } = string.Empty;
    public virtual string BasicAuthPassword { get; set; } = string.Empty;
    public virtual string ApiToken { get; set; } = string.Empty;
    public virtual string LoginName { get; set; } = string.Empty;
    public virtual string Password { get; set; } = string.Empty;
    public virtual int GuestSpaceId { get; set; } = 0;
    public KintoneAuthType AuthType { get; protected set; }

    public abstract void ApplyAuthentication(HttpRequestMessage request);
    protected virtual int ExtractAppID() => throw new NotImplementedException();
    protected virtual int ExtractGuestAppID() => throw new NotImplementedException();
}
