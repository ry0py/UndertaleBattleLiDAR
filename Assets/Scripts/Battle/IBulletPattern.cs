using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 弾を発射し、盤面/SOUL 情報を弾幕パターンへ提供する口。
    /// パターンは具象 Spawner ではなくこの小さな抽象に依存する (DIP/ISP)。
    /// </summary>
    public interface IBulletEmitter
    {
        /// <summary>盤面ローカル座標 from から velocity[px/s] で弾を発射する。</summary>
        void Emit(Vector2 fromLocal, Vector2 velocityLocal);

        Rect BoardLocalRect { get; }
        Vector2 SoulLocalPosition { get; }
    }

    /// <summary>
    /// 弾幕パターン。毎フレーム Tick され、必要に応じて emitter で弾を撃つ。
    /// 新パターンは本インターフェースを実装するだけ。既存コードは不変更 (OCP)。
    /// </summary>
    public interface IBulletPattern
    {
        void Tick(float deltaTime, IBulletEmitter emitter);
    }
}
