# CLAUDE.md

このファイルは Claude Code がこのリポジトリで作業する際の最上位ガイドです。
詳細は `.claude/docs/` 配下の各ドキュメントを参照してください（このファイルは「索引と原則」に徹し、重複を書きません＝DRY）。

## プロジェクト概要

Undertale のバトルシーン（弾幕パート）を **実機の物理ハート** で操作する Unity プロジェクト。

- **入力**: Hokuyo URG 2D LiDAR が、手で動かす 3D プリント製ハートの位置をスキャンする。
- **変換**: 極座標スキャン → 物理 XY 座標 → ゲーム盤面（Bullet Board）正規化座標。
- **出力**: ゲーム画面の SOUL（ハート）が物理ハートに追従し、弾幕を避ける。

つまり「マウス/キーボードの代わりに、現実のハートを動かして弾幕を避ける」体験を作る。

## 技術スタック

- Unity **6000.3.4f1**（Unity 6）
- Render Pipeline: **URP 17.3**
- Input: **Input System 1.17**（旧 Input Manager は使わない）
- UI: **uGUI (com.unity.ugui)** ＋ TextMeshPro
- LiDAR 連携: **URG-Unity (MediaFrontJapan)** … `com.mediafrontjapan.urg-unity`
  （UST-10LX を SCIP で接続・物体検出。実機の主経路）
- Editor 自動化: **MCP for Unity (CoplayDev)** … `http://127.0.0.1:8080/mcp`

## 設計原則（厳守）

このプロジェクトの全コードは以下に従う。詳細・具体例は
[.claude/docs/coding-standards.md](.claude/docs/coding-standards.md) を参照。

- **SOLID** — ハードウェア層・検出層・変換層・ゲーム層をインターフェースで分離する。
  特に LiDAR 実機への依存は `ILidarSensor` の背後に隔離し、実機が無くても開発できること。
- **DRY** — 座標変換・キャリブレーション・定数は 1 箇所に集約。重複ロジック禁止。
- **KISS** — 標準的な弾幕/HP ロジックを過度なパターンで飾らない。素直に書く。
- **YAGNI** — 「将来必要かも」で作らない。今の体験に必要な最小限のみ実装する。

## アーキテクチャ（4 層 + 設定）

データの流れは一方向： **Hardware → Tracking → Mapping → Input → Battle**

| 層 | 名前空間 | 役割 | 主要な型 |
|----|----------|------|----------|
| Hardware | `UndertaleLiDAR.LiDAR` | センサーから生スキャン取得 | `ILidarSensor`, `LidarScan`, `HokuyoUrgSensor`, `MockLidarSensor` |
| Tracking | `UndertaleLiDAR.Tracking` | スキャンからハート位置を検出 | `IHeartTracker`, `NearestClusterTracker` |
| Mapping | `UndertaleLiDAR.Mapping` | 物理座標 → 正規化盤面座標 | `ICoordinateMapper`, `RectCoordinateMapper` |
| Input | `UndertaleLiDAR.Input` | SOUL の入力源を抽象化 | `IHeartInputSource`, `ScanPlaneInputSource`(urg-unity), `LidarInputSource`, `KeyboardInputSource`, `FallbackInputSource` |
| Battle | `UndertaleLiDAR.Battle` | 弾幕ゲーム本体 | `SoulController`, `BulletBoard`, `Health`, `Bullet`, `BulletSpawner`, `IBulletPattern`, `BattleManager` |
| Config | `UndertaleLiDAR.Config` | 接続/キャリブレーション設定 | `LidarSettings` (ScriptableObject) |

詳細は [.claude/docs/architecture.md](.claude/docs/architecture.md)。

## ディレクトリ規約

- C# スクリプト: `Assets/Scripts/<層名>/` （上表の名前空間と一致させる）
- Prefab: `Assets/Prefabs/`
- シーン: `Assets/Scenes/`（メインは `BattleScene`）
- 設定アセット: `Assets/Settings/`（`LidarSettings` の `.asset` 等）
- ScriptableObject 定義クラス: `Assets/Scripts/Config/`

## Unity Editor 操作のルール（MCP）

シーン構築・GameObject/Prefab 作成・UI 配置・コンポーネント設定は
**手書き YAML を避け、必ず MCP for Unity 経由**で行う。手順とツールは
[.claude/docs/mcp-unity-workflow.md](.claude/docs/mcp-unity-workflow.md) と
`unity-mcp-skill` を参照。

接続確認: `/unity-check`（`.claude/commands/unity-check.md`）。

**MCP 未接続時は作業しない（厳守）**: UnityMCP ツールがセッションに出ていない場合、
シーン/Prefab/UI/GameObject/コンポーネント等の Editor 構築作業は**一切行わない**。
回避目的で Editor 拡張やシーンビルダー（過去の `BattleSceneBuilder` のようなもの）を
新規作成して代替することも**禁止**。まず下記手順で接続を回復してから作業する。
- UnityMCP は **user スコープ**に登録済み（`~/.claude.json` の top-level `mcpServers`）。
  これによりドライブレターの大文字小文字（`c:` / `C:`）に関係なく全セッションでロードされる。
- 出ていない時の回復: Unity Editor が起動済み・ポート 8080 が LISTENING を確認 →
  **新セッションを開始**（VSCode 拡張は `Ctrl+Shift+P`→「Developer: Reload Window」または
  ＋で新規チャット。MCP ツールはセッション開始時のみロードされ、起動済みセッションには
  後から反映されない）→ `/mcp` に `UnityMCP` が Connected で出ることを確認 → `/unity-check`。

## コード作業の鉄則

1. **スクリプトは `create_script`（MCP）または直接ファイル書き込みで作成**する。どちらでも
   起動中の Unity が自動コンパイルする。
2. 編集後は **コンパイル完了を待ち**（`mcpforunity://editor/state` の `is_compiling==false`）、
   **`read_console` でエラー確認**してから次へ進む。
3. MonoBehaviour は「層をまたぐ依存」を直接 `new` せず、インターフェース型のフィールド＋
   Inspector 注入（または `BattleManager` での合成）で受け取る（DIP）。
4. `Update()` で重い処理・GC アロケーションを避ける（スキャンは別スレッド/バッファ再利用）。

## 現状（2026-06-22 時点）

- 新規 Unity 6 プロジェクト。URP/2D/Input System/MCP/URG-Unity 導入済み。
- C# アーキテクチャ実装済み（`Assets/Scripts/`）。urg-unity 連携（`ScanPlaneInputSource`）済み。
- シーン/UI/Prefab は UnityMCP（`manage_scene`/`manage_gameobject`/`manage_prefabs`/`manage_ui`/
  `manage_components` 等）で構築する。MCP 未接続時は構築作業を行わない（上記ルール参照）。
- 実機の主経路は URG-Unity（UST-10LX）。Play 中 `C` キーで IP/位置/角度を校正。
  実機なしは BattleManager の `InputMode=Mock`（マウス操作）で確認可。
