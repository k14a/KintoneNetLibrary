# Changelog
本ドキュメントは、KintoneNetLibrary の変更履歴を管理するものである。  
各バージョンにおける追加機能、改善、修正内容を明確に記録し、利用者およびコントリビューターが変更点を把握しやすいように整理する。

本プロジェクトは [Semantic Versioning](https://semver.org/) に準拠する。

---

## [Unreleased]
### Added
- Kintone API の追加機能に対応する予定の項目
- CodeGen（モデル自動生成機能）の初期実装（予定）
- CLI ツールの基盤構築（予定）
- 型変換辞書の拡張（予定）

### Changed
- なし

### Fixed
- なし

---

## [0.9.1] – 2026-06-07
### Added
- **KintoneNetLibrary.Backup** プロジェクトを追加（バックアップ・リストア機能）
- **KintoneNetLibrary.IntegrationTests** プロジェクトを追加（実環境依存テストを分離）
- コンソールサンプルプロジェクトを追加（`samples/ConsoleSample/`）
- README をリファクタリング後の API に合わせて更新

### Changed
- **[破壊的変更] `KintoneModelBase` の CRUD メソッドに `IKintoneModelCrudService` を明示的に渡す形式に変更**（Service Locator パターンを廃止）
- **[破壊的変更] `IKintoneApi` を ISP に従い 4 サブインターフェースに分割**（`IKintoneRecordReadApi` / `IKintoneRecordWriteApi` / `IKintoneFileApi` / `IKintoneCursorApi`）
- **[破壊的変更] 識別子の命名規則を統一**（`ID` → `Id`、`AppID` → `AppId`、`RecordID` → `RecordId`、`FindByIDAsync` → `FindByIdAsync` 等）
- Clean Architecture の依存方向違反を修正（`IKintoneApiFactory` 戻り値を `IKintoneApi` に変更、`IKintoneRepository` のクエリ引数を整理）
- `NameConvertor` を `NameConverter` にリネーム（スペルミス修正）
- ロガー呼び出しを `LoggerMessage.Define` パターンに移行（CA1848/CA1873/CA2254 対応）
- ビルド警告を全件解消（CS8600 系 null 安全性、CS1570 系 XML コメント、CA 系アナライザー警告）
- 各ライブラリに `<Version>0.9.1</Version>` を設定

### Fixed
- `KintoneModelBase` の `ParseRecord<T>` が JSON フォーマット不一致で失敗していた問題を修正
- `ValidateSubTableRowStructure` で `[KintoneItem]` 属性なしのプロパティを誤ってエラー扱いしていた問題を修正
- `KintoneExpressionVisitor` の `Any` ハンドラが .NET 10 の `MemoryExtensions.Contains` 最適化に対応していなかった問題を修正

---

## [0.9.0] – 2026-05-01
### Added
- **KintoneModelBase** によるモデル駆動の利用スタイルを実装
- **KintoneTypedCrudService\<T\>** / **KintoneModelCrudService** による CRUD 操作のサービス層を追加
- **KintoneApi** による低レベル API アクセスを提供
- **cursor 自動運用機能**を実装し、大量データ取得を簡潔に実現
- **複数アプリを 1 プログラムで扱える設計**を導入
- 添付ファイルアップロード／ダウンロード API を実装
- バルク API（Bulk Request）に対応
- DI（依存性注入）に対応した構成を提供
- README、LICENSE、CONTRIBUTING.md を追加

### Changed
- なし（初期リリースのため）

### Fixed
- なし（初期リリースのため）

---

## [0.1.0] – 初期開発版（非公開）
- プロジェクト構成の作成
- 基本的な API 呼び出しの試作
- cursor 運用の検証
- モデル構造の検討
