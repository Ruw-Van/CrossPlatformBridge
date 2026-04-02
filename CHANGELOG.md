# Changelog

## [0.0.2] - 2026-04-02

### Changed
- `package.json` に UPM ドキュメント・Changelog・ライセンスリンクを追加

### Fixed
- `Network.Data.cs`, `DummyNetworkHandlerTests.cs`, `RoomSettingsTests.cs` の不具合を修正
- Netcode でホストを `ConnectedList` に含めるよう修正（`NetworkHandler.Core.cs`, `NetworkHandler.Room.cs`）

## [0.0.1] - 2026-03-27

### Added
- Use<T>() API によるプラットフォーム切り替えの簡素化
- マッチメイキング機能を全 5 プラットフォーム (Dummy, EOS, PUN2, PhotonFusion, Netcode) に実装
- EOS Payment ハンドラーの追加
- PlayFab / EOS Leaderboard ハンドラーの実装
- Firebase・Netcode・PhotonFusion の PlayMode 統合テスト基盤
- インテグレーションテスト設定の一括作成ウィンドウ

### Changed
- ファサードパターン統一・名前空間再設計・Leaderboard サービス追加
- README.md のサポートプラットフォーム表を更新
- ドキュメント更新: コードベース調査結果の反映とサービス対応表の整備

### Fixed
- プラットフォームテストの一括実行時エラーを修正し、テストカバレッジを拡充
- EOS テスト・Steam のコンパイルエラーとテスト失敗を修正
