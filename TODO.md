# TODO - コードレビュー指摘事項

コードレビュー（2026-06-04）で洗い出した問題点の一覧です。

---

## 凡例

- `[ ]` 未対応
- `[x]` 対応済み

---

## 1. Clean Architecture の依存方向違反

### 1-1. `IKintoneApiFactory`（Domain）が `KintoneApi`（Infrastructure）を直接参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneApiFactory.cs:1`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneApiFactory.cs#L1)
- **問題**: `using KintoneNetLibrary.Infrastructure.Api;` を参照し、戻り値型に具象クラス `KintoneApi` を使用している。Domain が Infrastructure 実装に依存している。
- **対応方針**: `KintoneApi` の代わりに `IKintoneApi`（Application 層）を返すよう変更する。

### 1-2. `IKintoneRepository`（Domain）が `KintoneQuery`（Application）を参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs:2`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs#L2)
- **問題**: `using KintoneNetLibrary.Application.UseCases;` を参照し、メソッド引数に `KintoneQuery<T>` を使用している。Domain が Application に依存している。
- **対応方針**: `KintoneQuery<T>` をビルドした `string` クエリを引数に取るよう変更するか、`KintoneQuery` を Domain 層に移動する。

### 1-3. `KintoneModelBase`（Domain）が `KintoneServiceLocator`（Infrastructure）を直接呼び出している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:6`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L6), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:21`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L21)
- **問題**: `using KintoneNetLibrary.Infrastructure.Helpers;` を参照し、Service Locator パターンで Infrastructure のサービスを取得している。Domain が Infrastructure に依存している。
- **対応方針**: 下記「設計問題」の Service Locator 廃止と合わせて対応する（→ 項目 3-1）。

### 1-4. `KintoneModelBase`（Domain）が `NameConvertor`（Infrastructure）を参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.cs:3`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.cs#L3)
- **問題**: `using KintoneNetLibrary.Infrastructure.Converters;` を参照している。
- **対応方針**: `NameConvertor` クラスを Domain または共通層に移動するか、Domain 側に抽象化（インターフェース）を置く。

---

## 2. 命名規則の問題

### 2-1. `NameConvertor` のスペルミス
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/NameConverter.cs`](src/KintoneNetLibrary/Domain/Entities/NameConverter.cs)
- **問題**: クラス名が `NameConvertor`（誤り）。他の Converter クラスはすべて `Converter`（正しいスペル）を使用している。
  - 正しい例: `KintoneRecordConverter`, `KintoneValueConverter`, `KintoneContentConverter`, `KintoneErrorConverter`, `TimeOnlyJsonConverter`
- **対応方針**: `NameConvertor` → `NameConverter` にリネームし、ファイル名も合わせて変更する。

### 2-2. `KintoneModelBase.Crud.cs` の変数名スペルミス
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:343`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L343), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:387`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L387)
- **問題**: ローカル変数名が `keyTYpe`（大文字 Y は誤り）。
- **対応方針**: `keyTYpe` → `keyType` に修正する。

---

## 3. 設計上の問題

### 3-1. Service Locator パターンの使用（アンチパターン）
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs`](src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: Domain エンティティ（`KintoneModelBase<TSelf>`）内で `KintoneServiceLocator.Resolve<T>()` を直接呼び出している。依存性が隠蔽され、テスト困難・初期化順序依存が生じる。
- **対応方針**: エンティティから CRUD 操作を分離し、アプリケーションサービス（またはコンストラクタ注入）経由で呼び出す設計に変更する。

#### 設計変更の概要
**問題**: `KintoneModelBase` の CRUD メソッドが `KintoneModelContext`（Domain 層のグローバル静的リゾルバー）経由でサービスを取得しており、依存が隠蔽されている。

**方針**: 全 CRUD メソッドに `IKintoneModelCrudService service` パラメーターを追加し、呼び出し側が明示的にサービスを渡す設計に変更する（Service Locator → Explicit Dependency）。

```csharp
// 変更前
await model.SaveAsync();
await BookModel.FindAllAsync();

// 変更後
await model.SaveAsync(service);
await BookModel.FindAllAsync(service);
```

**変更ファイル**:
- `Domain/Entities/KintoneModelBase.Crud.cs` — 全メソッドに `service` パラメーターを追加、`Service` プロパティ削除
- `Domain/Common/KintoneModelContext.cs` — 削除
- `Infrastructure/Helpers/KintoneServiceLocator.cs` — 削除（`Resolve<T>()` も未使用のため）
- テスト 5 ファイル（75 箇所）— `KintoneServiceLocator.Initialize(...)` の setup を削除し、モックを直接渡す形式に変更

### 3-2. `IKintoneApi` インターフェースが大きすぎる（ISP 違反）
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs`](src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs)
- **問題**: 1 つのインターフェースに Find（型付き・Raw・Stream）/CRUD/File/Cursor の全操作が含まれており、インターフェース分離原則に反する。
- **対応方針**: 責任ごとに分割する（例: `IKintoneRecordReadApi`, `IKintoneRecordWriteApi`, `IKintoneFileApi`, `IKintoneCursorApi`）。

### 3-3. `KintoneModelCrudService.cs` のメソッドコメントと実装の不一致
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs:29-34`](src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs#L29)
- **問題**: `DeleteAsync` メソッドのサマリーコメントが「既存のレコードを更新します」になっており、戻り値説明も「削除結果のインデックス情報」と「更新」が混在している。
- **対応方針**: コメントを正しく修正する（「既存のレコードを削除します」）。

### 3-4. `KintoneModelBase` に責任が集中しすぎている
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/`](src/KintoneNetLibrary/Domain/Entities/) （7 ファイルに分割済みだが依然として肥大）
- **問題**: Kintoneアクセス情報の管理・CRUD操作・JSON変換・レコード構築・ユーティリティ機能が 1 つのクラスに集約されており、SRP に反する。
- **対応方針**: 段階的にリファクタリングし、CRUD 操作は外部サービスに委譲する（→ 項目 3-1 と連動）。

#### 設計変更の概要
**現状**: `KintoneModelBase.JsonLoader.cs` および `KintoneSubTableBase.cs` が
`KintoneValueConverter`（Infrastructure）を参照しており、Clean Architecture の依存方向に違反している。

**方針**: `KintoneValueConverter` を `Infrastructure/Converters/` から `Domain/Entities/` へ移動する
（1-4 での `NameConverter` 移動と同様の対応）。

**変更ファイル**:
- `Domain/Entities/KintoneValueConverter.cs` — 新規作成（移動）、namespace を `Domain.Entities` に変更
- `Infrastructure/Converters/KintoneValueConverter.cs` — 削除
- `Domain/Entities/KintoneModelBase.JsonLoader.cs` — `using` を Infrastructure → Domain に変更
- `Domain/Entities/KintoneSubTableBase.cs` — 同上
- `Infrastructure/Converters/KintoneRecordConverter.cs` — 同上

**備考**: 現在の 7 partial ファイル構成（Crud / Conversion / Hooks / JsonLoader / RecordBuilder / Utility）は
SRP の段階的分離として適切であり、これ以上の責任分離は破壊的変更を伴うため今回は対象外とする。

---

## 4. 重複コード

### 4-1. `BulkAsync` / `SingleAsync` の実装パターンが重複している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: `CreateBulkAsync`/`CreateSingleAsync`、`UpdateBulkAsync`/`UpdateSingleAsync`、`DeleteBulkAsync`/`DeleteSingleAsync`、`SaveBulkAsync`/`SaveSingleAsync`、`SaveWithRetryBulkAsync`/`SaveWithRetrySingleAsync` で、「Single 版は Bulk 版をループで呼ぶ」という同じパターンが 5 回繰り返されている。
- **対応方針**: プライベートヘルパー `RunSingleWriteAsync`（書き込み系4種）と `RunSingleDeleteAsync<TItem>`（削除系2種）に集約した。あわせて `KintoneDeleteResult` に `Merge` メソッドを追加。

### 4-2. 各 `BulkAsync` と `SingleAsync` 内での `KintoneServiceLocator.Resolve` の重複呼び出し
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: 全 static メソッドがそれぞれ `KintoneServiceLocator.Resolve<IKintoneModelCrudService>()` を呼び出している（インスタンスメソッドは `this.Service` プロパティを使用）。
- **対応方針**: 項目 3-1 にて `KintoneServiceLocator` を削除し、明示的サービス引数に変更済みのため解消。

---

## 5. その他

### 5-1. `FindByKeyAsync` / `FindByKeysAsync` でリフレクションを多用している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: キーフィールドへのアクセスにリフレクションを多用しており、型安全性が低く、パフォーマンス・可読性に懸念がある。
- **対応方針**: `abstract BuildKeyQuery()` の代わりに、呼び出し側が明示的にキーセレクターを渡す型安全な形式（`Expression<Func<TSelf, TKey>> keySelector`）に変更した。`CreateFieldSelector`・`ConvertExpression` ヘルパーおよび `using System.Reflection;` を削除。テストも更新し、`IsKey` 属性バリデーション系のテストを削除した。

### 5-2. `IKintoneRepository` の戻り値が `string` で型安全でない
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs)
- **問題**: CRUD・Find 系メソッドがすべて `Task<string>` / `Task<string?>` を返しており、JSON 文字列をそのまま戻り値としている。型安全性がなく、呼び出し側でのデシリアライズが必要になる。
- **対応方針**: 設計上の意図的仕様のため**コード変更なし**。`IKintoneRepository` はローレベルAPIとして生JSONを返し、独自JSONパーサーを使いたい利用者向けに提供する。型付き結果が必要な場合は `KintoneTypedCrudService` 等のハイレベルAPIを使用する。
