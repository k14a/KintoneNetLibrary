namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    // ----- Hook -----
    public virtual async Task RunBeforeCreateHookAsync() => await OnBeforeCreateAsync();
    public virtual async Task RunAfterCreateHookAsync() => await OnAfterCreateAsync();
    public virtual async Task RunBeforeUpdateHookAsync() => await OnBeforeUpdateAsync();
    public virtual async Task RunAfterUpdateHookAsync() => await OnAfterUpdateAsync();
    public virtual async Task RunBeforeDeleteHookAsync() => await OnBeforeDeleteAsync();
    public virtual async Task RunAfterDeleteHookAsync() => await OnAfterDeleteAsync();
    /// <summary>
    /// 登録・更新前に呼び出されるカスタムバリデーション
    /// </summary>
    public virtual void ValidateBeforeSave() { }

}