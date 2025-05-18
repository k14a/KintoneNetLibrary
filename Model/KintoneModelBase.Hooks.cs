using System.Collections.Generic;

namespace KintoneNetLibrary.Model;

public abstract partial class KintoneModelBase
{
    /* =========================================================
       Hook メソッド
       ---------------------------------------------------------
       派生クラスで必要に応じて override してください。
       返り値に新しいコレクションを返すことで
       Create／Update／Delete 対象を差し替えることも可能です。
       ========================================================= */

    /// <summary>
    /// Create 処理前に呼ばれます。値の事前セットや対象の追加・削除などに使用してください。
    /// </summary>
    public virtual IList<T> CreateHook<T>(IList<T> objs) where T : KintoneModelBase
    {
        return objs;
    }

    /// <summary>
    /// Update 処理前に呼ばれます。<br/>
    /// 例：レコード ID が無い場合、キー項目から ID を取得してセットする等。
    /// </summary>
    public virtual IList<T> UpdateHook<T>(IList<T> objs) where T : KintoneModelBase
    {
        return objs;
    }

    /// <summary>
    /// Delete 処理前に呼ばれます。<br/>
    /// 例：連鎖削除の判定や対象フィルタリングなど。
    /// </summary>
    public virtual IList<string> DeleteHook<T>(IList<string> ids) where T : KintoneModelBase
    {
        return ids;
    }
}
