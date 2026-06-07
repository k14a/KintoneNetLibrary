# KintoneNetLibrary

本ドキュメントは、本ライブラリの導入・活用の入口を示す README である。開発方針・主要な特徴・導入手順・代表的な利用例（サンプルコード）を短時間で理解できるように整理している。

---

## 1. はじめに（Overview）

**KintoneNetLibrary** は .NET 6 以降（.NET 8 / .NET 10 を含む）のモダンな .NET 環境で動作する Kintone API クライアントである。本ライブラリは、既存の kintoneDotNET（.NET Framework 4.7.2 で開発が止まっている）に見られる制約を解消することを目的に設計された。複数の Kintone アプリを単一プログラムで扱える柔軟性と、Kintone の推奨する cursor を自動的に運用して大量データを簡潔に取得できる点を主要な特徴とする。

---

## 2. 特徴（Features） ✅

- **複数アプリを 1 プログラムで扱える柔軟な設計**
  - App 設定を app.config に固定しない。1 つの実行ファイルで複数の Kintone アプリと連携可能であり、アプリ間のデータ連携を単一プログラム内で完結できる。 

- **cursor を自動運用する Find 実装**
  - Kintone が推奨する cursor を内部で自動的に運用する。開発者は cursor の存在を意識せずに大量データ取得が可能である。
  - 低レベル API（`IKintoneApi`）経由の呼び出しでも自動 cursor 利用に対応している。
  - ※注意: cursor を細かく制御したいユースケース（特殊なページングや長期保持など）には適さない場合がある。

- **選べる API スタイル（利用者レベルに応じた 3 段階）**
  - 初心者向け：モデル継承（`KintoneModelBase<TSelf>`）
  - 中級者向け：`IKintoneTypedCrudService<T>` を使用するサービス層
  - 上級者向け：低レベル API（`IKintoneApi`）を直接利用

### 利用スタイルのサンプル

- 初心者向け：モデル継承 (KintoneModelBase)

```csharp
public class CustomerModel : KintoneModelBase<CustomerModel>
{
    public override int AppId { get; init; } = 100;
    public override KintoneAccessBase Access { get; init; }
        = new ApiTokenAccess("your-domain.cybozu.com", "YOUR_API_TOKEN");

    [KintoneItem("CustomerName", KintoneFieldType.SingleLineText)]
    public string Name { get; set; } = string.Empty;
}

// service は IKintoneModelCrudService を DI で取得
var record = await CustomerModel.FindByIdAsync(service, "123");
```

- 中級者向け：IKintoneTypedCrudService

```csharp
var service = provider.GetRequiredService<IKintoneTypedCrudService<CustomerModel>>();
var list = await service.FindAsync(x => x.Status == "Active");
```

- 上級者向け：IKintoneApi を直接利用

```csharp
var api = provider.GetRequiredService<IKintoneApiFactory>()
                  .Create(access, appId);
var result = await api.FindByQueryAsync<CustomerModel>("Status = \"Active\"");
```

---

## 3. インストール方法（Installation）

NuGet でパッケージをインストールする。

```bash
dotnet add package KintoneNetLibrary
```

対応ランタイム: **.NET 6 / .NET 7 / .NET 8 / .NET 10**。

---

## 4. クイックスタート（Quick Start） ⚡

### 設定例（`appsettings.json`）

```json
{
  "Kintone": {
    "BaseUrl": "https://{your-domain}.cybozu.com",
    "ApiToken": "XXXXXXXXXXXXXXXX",
    "TimeoutSeconds": 30
  }
}
```

### 最小限のレコード取得（`KintoneModelBase` を利用）

```csharp
public class CustomerModel : KintoneModelBase<CustomerModel>
{
    public override int AppId { get; init; } = 100;
    public override KintoneAccessBase Access { get; init; }
        = new ApiTokenAccess("your-domain.cybozu.com", "YOUR_API_TOKEN");

    [KintoneItem("CustomerName", KintoneFieldType.SingleLineText)]
    public string Name { get; set; } = string.Empty;
}

// service は IKintoneModelCrudService を DI で取得
var record = await CustomerModel.FindByIdAsync(service, "123");
```

### DI コンテナの登録例

```csharp
services.AddHttpClient();
services.AddScoped<IKintoneApiFactory, KintoneApiFactory>();
services.AddScoped<IKintoneRepository, KintoneRepository>();
services.AddScoped<IKintoneModelCrudService, KintoneModelCrudService>();
services.AddScoped(typeof(IKintoneTypedCrudService<>), typeof(KintoneTypedCrudService<>));
```

### 低レベル API の最小例

```csharp
var factory = provider.GetRequiredService<IKintoneApiFactory>();
var api = factory.Create(new ApiTokenAccess("your-domain.cybozu.com", "YOUR_API_TOKEN"), appId);

var result = await api.FindByQueryAsync<CustomerModel>("order by $id asc");
```

---

## 5. 基本的な使い方（Usage Examples） 🔧

### 5.1 レコード取得（モデル継承）

```csharp
public class CustomerModel : KintoneModelBase<CustomerModel>
{
    public override int AppId { get; init; } = 100;
    public override KintoneAccessBase Access { get; init; }
        = new ApiTokenAccess("your-domain.cybozu.com", "YOUR_API_TOKEN");

    [KintoneItem("CustomerName", KintoneFieldType.SingleLineText)]
    public string Name { get; set; } = string.Empty;
}

// Id 指定で単一レコードを取得
var item = await CustomerModel.FindByIdAsync(service, "123");

// クエリで複数レコードを取得
var list = await CustomerModel.FindByQueryAsync(service, "Status = \"Active\"");
```

### 5.2 レコード登録

```csharp
var model = new CustomerModel { Name = "Acme" };
var result = await model.CreateAsync(service);
```

### 5.3 クエリ検索

```csharp
var result = await CustomerModel.FindByQueryAsync(service, "Status = \"Active\" order by $id asc");
foreach (var r in result) Console.WriteLine(r.RecordId + ": " + r.Name);
```

> 注：`FindByQueryAsync` は内部で cursor を自動運用するため、大量データも透過的に取得できる。細かい cursor 制御が必要な場合は `IKintoneApi` の直接利用を検討すること。

### 5.4 添付ファイル操作

```csharp
var api = factory.Create(access, appId);

// ファイルアップロード
var fileKey = await api.UploadAsync("document.pdf", fileStream);

// レコードに紐づけて登録
var rec = new CustomerModel { Name = "WithFile" };
rec.AddAttachment("fileField", fileKey);
await rec.CreateAsync(service);
```

### 5.5 バルク操作

```csharp
var result = await CustomerModel.CreateBulkAsync(service, items);
```

### 5.6 DI 登録例

```csharp
public static IServiceCollection AddKintoneConfig(this IServiceCollection services) {
    // HttpClient の登録
    services.AddHttpClient();
    // ApiFactory の登録
    services.AddScoped<IKintoneApiFactory, KintoneApiFactory>();
    // Repository の登録
    services.AddScoped<IKintoneRepository, KintoneRepository>();
    // CRUD Service の登録（非ジェネリック版 → static API 用）
    services.AddScoped<IKintoneModelCrudService, KintoneModelCrudService>();
    // CRUD Service の登録（ジェネリック版 → 型付き CRUD）
    services.AddScoped(typeof(IKintoneTypedCrudService<>), typeof(KintoneTypedCrudService<>));
    return services;
}
```

### 5.7 DI を使った利用例

```csharp
// 型付き CRUD サービスを使う場合
var typed = serviceProvider.GetRequiredService<IKintoneTypedCrudService<CustomerModel>>();
var active = await typed.FindAsync(x => x.Status == "Active");

// モデルの static メソッドを使う場合
var crud = serviceProvider.GetRequiredService<IKintoneModelCrudService>();
var record = await CustomerModel.FindByIdAsync(crud, "123");
```

---

## 6. 設計思想（Design Philosophy） 💡

- **拡張性を重視**：Clean Architecture に基づき、Domain / Application / Infrastructure を明確に分離している。
- **疎結合**：インターフェースを多用し、DI による差し替えが容易で単体テストが行いやすい。
- **明示的な依存**：Service Locator パターンを廃止し、CRUD メソッドは `IKintoneModelCrudService` を明示的に受け取る設計としている。
- **インターフェース分離**：`IKintoneApi` は `IKintoneRecordReadApi` / `IKintoneRecordWriteApi` / `IKintoneFileApi` / `IKintoneCursorApi` に分割され、必要な機能のみに依存できる。
- **カーソルの隠蔽**：大量データ取得における cursor の煩雑さを隠蔽し、利用者がビジネスロジックに集中できるように設計している。

---

## 7. サンプルプロジェクト（Sample Project）

リポジトリには `samples/` を用意しており、コンソールサンプルや ASP.NET Core 統合例を参照できる（設定ファイル・DI 構成例を含む）。

---

## 8. 今後のロードマップ（Roadmap） 🛣️

- CodeGen によるモデル自動生成機能の提供
- CLI（コマンドラインツール）の提供
- 多言語対応可能な型変換辞書の整備
- サンプルプロジェクトの拡充
- コントリビューションしやすいガイドやテンプレートの整備

---

## 9. 貢献方法（Contributing） 🤝

- Issue を立てて議論のうえ、PR を送ってください。
- コーディング規約と単体テスト、ドキュメント更新を含めた PR を歓迎します。
- 詳細は `CONTRIBUTING.md` を参照してください。

---

## 10. ライセンス（License） 📄

本ライブラリは **MIT License** の下で公開します。詳細は `LICENSE` ファイルを参照してください。

---

### 補足

本 README に不足する具体的な利用例や API リファレンスは、リポジトリの `docs/` または XML コメントから生成されるドキュメントを参照してください。
