namespace KintoneNetLibrary.Domain.Entities {
    /// <summary>
    /// Kintoneモデルのフック基底クラス。
    /// </summary>
    public abstract class KintoneModelHookBase {
        /// <summary>
        /// レコード作成前に呼ばれるフック。
        /// 必要に応じてプロパティの補正や検証を実装可能。
        /// </summary>
        public virtual Task OnBeforeCreateAsync() => Task.CompletedTask;
        /// <summary>
        /// レコード作成後に呼ばれるフック。
        /// 作成された内容に対する処理を実装可能。
        /// </summary>
        public virtual Task OnAfterCreateAsync() => Task.CompletedTask;
        /// <summary>
        /// レコード更新前に呼ばれるフック。
        /// 更新データのバリデーションや事前処理を実装可能。
        /// </summary>
        public virtual Task OnBeforeUpdateAsync() => Task.CompletedTask;
        /// <summary>
        /// レコード更新後に呼ばれるフック。
        /// ロギングなどの事後処理に使用可能。
        /// </summary>
        public virtual Task OnAfterUpdateAsync() => Task.CompletedTask;
        /// <summary>
        /// レコード削除前に呼ばれるフック。
        /// 削除確認や関連データ処理に使用可能。
        /// </summary>
        public virtual Task OnBeforeDeleteAsync() => Task.CompletedTask;
        /// <summary>
        /// レコード削除後に呼ばれるフック。
        /// ログ出力や通知処理に使用可能。
        /// </summary>
        public virtual Task OnAfterDeleteAsync() => Task.CompletedTask;
    }
}
