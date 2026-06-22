# アーキテクチャ詳細

データは一方向に流れる。各層は **下位層のインターフェースだけ**に依存し、具象実装には依存しない（DIP）。

```
[物理ハート]
    │  手で動かす
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Hardware 層 (UndertaleLiDAR.LiDAR)                            │
│   ILidarSensor ── HokuyoUrgSensor (実機 / SCIP 2.0)          │
│              └─── MockLidarSensor (マウス駆動 / 開発用)       │
│   出力: LidarScan（極座標 measurement の配列）               │
└──────────────────────────────────────────────────────────────┘
    │  LidarScan
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Tracking 層 (UndertaleLiDAR.Tracking)                        │
│   IHeartTracker ── NearestClusterTracker                     │
│   出力: 物理 XY 座標 (メートル, Vector2) ＋ 検出成否         │
└──────────────────────────────────────────────────────────────┘
    │  物理 Vector2
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Mapping 層 (UndertaleLiDAR.Mapping)                          │
│   ICoordinateMapper ── RectCoordinateMapper                  │
│   出力: 正規化座標 (0..1, Vector2)  ※盤面非依存             │
└──────────────────────────────────────────────────────────────┘
    │  正規化 Vector2
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Input 層 (UndertaleLiDAR.Input)                              │
│   IHeartInputSource ── LidarInputSource (上3層を合成)        │
│                   └─── KeyboardInputSource (フォールバック)  │
│   出力: 正規化座標 (0..1) ＋ 有効フラグ                      │
└──────────────────────────────────────────────────────────────┘
    │  正規化 Vector2
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Battle 層 (UndertaleLiDAR.Battle)                            │
│   SoulController ── 正規化座標を BulletBoard 内の実座標へ     │
│   BulletBoard ──── 盤面の矩形境界（唯一の座標基準）         │
│   BulletSpawner ── IBulletPattern で弾を生成                 │
│   Bullet ───────── 衝突時に Health へダメージ               │
│   Health ───────── HP 管理・イベント発火                     │
│   BattleManager ── 上記を組み立て・状態遷移を統括（合成点） │
└──────────────────────────────────────────────────────────────┘
```

## 層の責務（単一責任 / SRP）

### Hardware 層
- 唯一の責務: センサーから生スキャンを取得し `LidarScan` として供給する。
- **座標変換やゲームロジックを持たない。** 単位はセンサー固有（mm 距離・step 角度）。
- `HokuyoUrgSensor` は SCIP 2.0 をバックグラウンドスレッドで通信（[lidar-integration.md](lidar-integration.md)）。
- `MockLidarSensor` は実機なしで上位層を開発・デモするための差し替え。**LSP**: どちらも
  `ILidarSensor` 契約（接続→`TryGetLatestScan` で最新スキャンをプル→切断）を完全に満たす。
- スキャン取得は **プル型**（メインスレッドが `TryGetLatestScan` を呼ぶ）。Hokuyo は別スレッドで
  受信した最新スキャンをロック越しに渡すため、Unity API をスレッド外から触らずに済む。

### Tracking 層
- 唯一の責務: スキャンの中から「ハート（最も近い物体クラスタ）」を 1 点に特定する。
- ノイズ点の除去・クラスタリング・重心計算のみ。盤面やゲームを知らない。

### Mapping 層
- 唯一の責務: 物理座標 ↔ 正規化座標 (0..1) の相互変換。
- キャリブレーション（物理スキャン範囲の min/max）はここ **だけ** が保持（DRY）。

### Input 層
- 唯一の責務: 「SOUL を動かす正規化座標を毎フレーム供給する」抽象 `IHeartInputSource`。
- `LidarInputSource` は Hardware+Tracking+Mapping を **合成するだけ**で、自前のロジックを持たない。
- `KeyboardInputSource` は LiDAR が無い/未検出時のフォールバック（Input System 使用）。
- これにより Battle 層は **入力源を一切知らずに**動く（OCP: 入力源追加は新クラスのみ）。

### Battle 層
- Undertale 弾幕の本体。`BulletBoard` が座標の唯一の基準（SOUL も弾もこの矩形内）。
- `IBulletPattern` で弾幕パターンを差し替え可能（OCP）。新パターン＝新クラス、既存は不変更。
- `BattleManager` が依存を組み立てる **唯一の合成ルート**（Composition Root）。

## 依存性注入の方針

- MonoBehaviour 同士の参照は Inspector 注入（`[SerializeField]`）を基本とする。
- 非 MonoBehaviour（センサー実装・トラッカー・マッパー）は `BattleManager` か
  各 InputSource の `Awake` で生成・結線する。具象の `new` は合成点に閉じ込める。
- `LidarSettings`（ScriptableObject）で接続情報とキャリブレーションを外部化し、
  コード再ビルド無しで現場調整できるようにする。

## なぜこの構成か（KISS/YAGNI の判断記録）

- 層は **4 つだけ**。これ以上分けても今の体験に価値が無い（YAGNI）。
- 抽象（interface）は「差し替えが現実に起きる所」にだけ置いた:
  - センサー（実機⇔モック）… 必須
  - 入力源（LiDAR⇔キーボード）… 必須（デモ・デバッグ）
  - 弾幕パターン … バトル拡張の中心
- トラッカー/マッパーも interface 化したが、実装は各 1 つ。差し替え予定が無ければ
  将来 interface を畳んでも良い（過剰な抽象を増やさない方針）。
