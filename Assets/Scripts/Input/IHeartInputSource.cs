using UnityEngine;

namespace UndertaleLiDAR.Input
{
    /// <summary>
    /// SOUL を動かすための正規化座標 (0..1) を供給する抽象。
    /// Battle 層はこの 1 つの口だけを見ればよく、入力源 (LiDAR/キーボード) を一切意識しない (DIP/OCP)。
    /// </summary>
    public interface IHeartInputSource
    {
        /// <summary>このフレームの目標位置 (0..1)。有効な入力が無ければ false。</summary>
        bool TryGetNormalizedPosition(out Vector2 normalized);
    }
}
