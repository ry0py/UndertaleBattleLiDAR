# MCP for Unity ワークフロー

Editor 操作（シーン・GameObject・Prefab・UI・コンポーネント）は手書き YAML を避け、
必ず MCP for Unity 経由で行う。詳細なツール仕様は `unity-mcp-skill` を参照。

## 接続前提

- Unity Editor が起動し、対象プロジェクトを開いていること。
- MCP ブリッジが `http://127.0.0.1:8080/mcp` で LISTENING。
- Claude Code セッションに `UnityMCP` ツール群（`create_script`, `manage_gameobject`,
  `manage_scene`, `manage_prefabs`, `manage_ui`, `manage_components`, `batch_execute`,
  `read_console`, `manage_camera` など）が出ていること。

**ツールが出ていない時の復旧:**
1. Unity Editor が起動済みか確認。
2. `netstat` で 8080 が LISTENING か確認（サーバー稼働中か）。
3. Claude Code の `/mcp` で `UnityMCP` を reconnect、または Claude Code を再起動。
4. `/unity-check` で疎通とコンパイル状態を確認。

## 鉄則（skill の要点を本プロジェクト向けに抜粋）

1. **resource を先に読む**: `mcpforunity://editor/state`（`ready_for_tools`/`is_compiling`）、
   `mcpforunity://project/info`（uGUI/TMP/Input System の有無）を確認してから操作。
2. **スクリプト作成/編集後はコンパイル待ち → `read_console(types=["error"])`** で確認。
3. **複数操作は `batch_execute`** でまとめる（最大 25/バッチ）。依存があれば `fail_fast=True`。
4. **見た目の検証は `manage_camera(action="screenshot", include_image=True)`**。
5. Prefab 実体化は `manage_gameobject(action="create", prefab_path=...)`。

## このプロジェクトの構築タスク（MCP 復帰後の手順）

1. `BattleScene` を作成（`manage_scene`, template `2d_basic`）。
2. 設定アセット作成: `LidarSettings` の `.asset`（`manage_asset` / メニュー経由）。
3. ルート構成を `batch_execute` で作成:
   - `BattleManager`（空 GO, `BattleManager` 付与）
   - `Canvas`（uGUI, Screen Space - Camera）＋ `EventSystem`
   - `BulletBoard`（UI Image, 盤面枠）＋子に `BulletContainer`
   - `Soul`（UI Image/Sprite, `SoulController` 付与）
   - `HealthBarUI`, `HpText`（TMP）
4. Prefab 化: `Bullet`（`Bullet` 付与, 当たり判定）→ `Assets/Prefabs/Bullet.prefab`。
5. `BattleManager` の `[SerializeField]` を結線（センサー種別・各参照・`LidarSettings`）。
6. `manage_components` で値設定（盤面サイズ・HP・速度・パターン）。
7. `manage_camera` で Play せずに構図確認、`read_console` でエラーゼロを確認。

## 注意
- スクリプトはこのリポジトリに直接書き込んでもよい（起動中 Unity が自動コンパイル）。
  MCP の `create_script` と等価。どちらでも編集後はコンソール確認を行う。
- シーン/Prefab/メタの整合は MCP に任せ、手で `.unity`/`.prefab`/`.meta` を編集しない。
