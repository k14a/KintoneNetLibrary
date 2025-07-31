namespace KintoneNetLibrary.Domain.Entities;

public abstract class KintoneAccessBase {
    /// <summary>
    /// Kintone 接続情報を生成する
    /// </summary>
    public abstract KintoneAccount ToKintoneAccount();

    /// <summary>
    /// AppID を直接持たせる場合はこちらでもOK
    /// </summary>
    public virtual int AppID => ToKintoneAccount().GuestSpaceId == 0 ? ExtractAppID() : ExtractGuestAppID();

    protected virtual int ExtractAppID() => throw new NotImplementedException();
    protected virtual int ExtractGuestAppID() => throw new NotImplementedException();
}
