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
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Converters/NameConvertor.cs:7`](src/KintoneNetLibrary/Infrastructure/Converters/NameConvertor.cs#L7)
- **問題**: クラス名が `NameConvertor`（誤り）。他の Converter クラスはすべて `Converter`（正しいスペル）を使用している。
  - 正しい例: `KintoneRecordConverter`, `KintoneValueConverter`, `KintoneContentConverter`, `KintoneErrorConverter`, `TimeOnlyJsonConverter`
- **対応方針**: `NameConvertor` → `NameConverter` にリネームし、ファイル名も合わせて変更する。

### 2-2. `KintoneModelBase.Crud.cs` の変数名スペルミス
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:343`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L343), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:387`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L387)
- **問題**: ローカル変数名が `keyTYpe`（大文字 Y は誤り）。
- **対応方針**: `keyTYpe` → `keyType` に修正する。

---

## 3. 設計上の問題

### 3-1. Service Locator パターンの使用（アンチパターン）
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs`](src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: Domain エンティティ（`KintoneModelBase<TSelf>`）内で `KintoneServiceLocator.Resolve<T>()` を直接呼び出している。依存性が隠蔽され、テスト困難・初期化順序依存が生じる。
- **対応方針**: エンティティから CRUD 操作を分離し、アプリケーションサービス（またはコンストラクタ注入）経由で呼び出す設計に変更する。

### 3-2. `IKintoneApi` インターフェースが大きすぎる（ISP 違反）
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs`](src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs)
- **問題**: 1 つのインターフェースに Find（型付き・Raw・Stream）/CRUD/File/Cursor の全操作が含まれており、インターフェース分離原則に反する。
- **対応方針**: 責任ごとに分割する（例: `IKintoneRecordReadApi`, `IKintoneRecordWriteApi`, `IKintoneFileApi`, `IKintoneCursorApi`）。

### 3-3. `KintoneModelCrudService.cs` のメソッドコメントと実装の不一致
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs:29-34`](src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs#L29)
- **問題**: `DeleteAsync` メソッドのサマリーコメントが「既存のレコードを更新します」になっており、戻り値説明も「削除結果のインデックス情報」と「更新」が混在している。
- **対応方針**: コメントを正しく修正する（「既存のレコードを削除します」）。

### 3-4. `KintoneModelBase` に責任が集中しすぎている
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/`](src/KintoneNetLibrary/Domain/Entities/) （7 ファイルに分割済みだが依然として肥大）
- **問題**: Kintoneアクセス情報の管理・CRUD操作・JSON変換・レコード構築・ユーティリティ機能が 1 つのクラスに集約されており、SRP に反する。
- **対応方針**: 段階的にリファクタリングし、CRUD 操作は外部サービスに委譲する（→ 項目 3-1 と連動）。

---

## 4. 重複コード

### 4-1. `BulkAsync` / `SingleAsync` の実装パターンが重複している
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: `CreateBulkAsync`/`CreateSingleAsync`、`UpdateBulkAsync`/`UpdateSingleAsync`、`DeleteBulkAsync`/`DeleteSingleAsync`、`SaveBulkAsync`/`SaveSingleAsync`、`SaveWithRetryBulkAsync`/`SaveWithRetrySingleAsync` で、「Single 版は Bulk 版をループで呼ぶ」という同じパターンが 5 回繰り返されている。
- **対応方針**: `ExecuteInBatchOrSingle<T>(models, operation, isBulk)` のような共通メソッドに集約する。

### 4-2. 各 `BulkAsync` と `SingleAsync` 内での `KintoneServiceLocator.Resolve` の重複呼び出し
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: 全 static メソッドがそれぞれ `KintoneServiceLocator.Resolve<IKintoneModelCrudService>()` を呼び出している（インスタンスメソッドは `this.Service` プロパティを使用）。
- **対応方針**: 項目 3-1 の対応と合わせて解消する。

---

## 5. その他

### 5-1. `FindByKeyAsync` / `FindByKeysAsync` でリフレクションを多用している
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:333-418`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L333)
- **問題**: キーフィールドへのアクセスにリフレクションを多用しており、型安全性が低く、パフォーマンス・可読性に懸念がある。
- **対応方針**: ジェネリクスと抽象メソッド（例: `abstract KintoneQuery<TSelf> BuildKeyQuery()` を継承クラスで実装）に置き換えることを検討する。

### 5-2. `IKintoneRepository` の戻り値が `string` で型安全でない
- [ ] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs)
- **問題**: CRUD・Find 系メソッドがすべて `Task<string>` / `Task<string?>` を返しており、JSON 文字列をそのまま戻り値としている。型安全性がなく、呼び出し側でのデシリアライズが必要になる。
- **対応方針**: 必要に応じて適切な型（`Task<KintoneWriteResult<T>>`、`Task<IEnumerable<T>>` 等）を返すよう変更する。
