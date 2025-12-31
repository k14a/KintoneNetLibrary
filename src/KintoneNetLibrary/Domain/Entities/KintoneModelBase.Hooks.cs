namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    /// <summary>
    /// 登録前のフックを実行します。
    /// </summary>
    public virtual async Task RunBeforeCreateHookAsync() => await this.OnBeforeCreateAsync();

    /// <summary>
    /// 登録後のフックを実行します。
    /// </summary>
    public virtual async Task RunAfterCreateHookAsync() => await this.OnAfterCreateAsync();

    /// <summary>
    /// 更新前のフックを実行します。
    /// </summary>
    public virtual async Task RunBeforeUpdateHookAsync() => await this.OnBeforeUpdateAsync();
    
    /// <summary>
    /// 更新後のフックを実行します。
    /// </summary>
    public virtual async Task RunAfterUpdateHookAsync() => await this.OnAfterUpdateAsync();

    /// <summary>
    /// 削除前のフックを実行します。
    /// </summary>
    public virtual async Task RunBeforeDeleteHookAsync() => await this.OnBeforeDeleteAsync();
    
    /// <summary>
    /// 削除後のフックを実行します。
    /// </summary>
    public virtual async Task RunAfterDeleteHookAsync() => await this.OnAfterDeleteAsync();

    /// <summary>
    /// 登録・更新前に呼び出されるカスタムバリデーション
    /// </summary>
    public virtual void ValidateBeforeSave() { }

}