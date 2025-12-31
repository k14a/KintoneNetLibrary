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
- サンプルプロジェクトの追加（予定）

### Changed
- API 設計の見直し（必要に応じて）
- 内部構造のリファクタリング（予定）

### Fixed
- 現時点ではなし

---

## [0.9.0] – YYYY-MM-DD（リリース予定）
### Added
- **KintoneModelBase** によるモデル駆動の利用スタイルを実装
- **KintoneModelCrudService<T>** による CRUD 操作のサービス層を追加
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
