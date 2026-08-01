# XML ドキュメントコメント ガイドライン（日本語版）

## 基本方針

- XML コメントは **日本語で記述する**。
- summary は 1〜2 文で簡潔に書く。
- このメソッドが **直接スローする例外のみ** を記述する。
- 内部メソッドがスローする例外は列挙しない。
- remarks には利用者が理解すべき補足情報を書く。

## **Method（メソッド）用テンプレート**

```csharp
/// <summary>
/// {メソッドの目的を1〜2文で簡潔に記述する}
/// </summary>
/// <param name="{param}">{パラメータの役割を簡潔に記述する}</param>
/// <returns>{戻り値の意味を記述する（void の場合は不要）}</returns>
/// <exception cref="ArgumentNullException">
/// このメソッドが **直接スローする例外のみ** を記述する。
/// 内部メソッドがスローする例外は列挙しない。
/// </exception>
/// <remarks>
/// 必要に応じて補足説明を記述する。
/// 実装詳細ではなく、利用者が理解すべき振る舞いを中心に書く。
/// </remarks>
/// <example>
/// <code>
/// // 使用例
/// var result = service.Execute("path/to/file");
/// </code>
/// </example>
```

---

## **Property（プロパティ）用テンプレート**

```csharp
/// <summary>
/// {プロパティの意味・役割を記述する}
/// </summary>
/// <value>
/// {値の意味や制約を記述する}
/// </value>
```

---

## **Class / Interface 用テンプレート**

```csharp
/// <summary>
/// {型の目的・責務を1〜2文で記述する}
/// </summary>
/// <remarks>
/// 利用者が理解すべき前提条件や制約があれば記述する。
/// 実装詳細は書かない。
/// </remarks>
```

---

## **Enum 用テンプレート**

```csharp
/// <summary>
/// {列挙体の目的を記述する}
/// </summary>
public enum SampleEnum
{
    /// <summary>
    /// {値の意味}
    /// </summary>
    Value1,

    /// <summary>
    /// {値の意味}
    /// </summary>
    Value2
}
```
