using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 弾幕の盤面 (Undertale の四角い枠)。SOUL も弾もこの矩形のローカル座標
    /// (中心原点) で動く。座標の唯一の基準点であり、サイズ・正規化変換を一元管理する (DRY/SRP)。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BulletBoard : MonoBehaviour
    {
        private RectTransform _rect;
        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        /// <summary>盤面のサイズ (px, UI ローカル)。</summary>
        public Vector2 Size => Rect.rect.size;

        /// <summary>中心原点のローカル矩形。</summary>
        public Rect LocalRect
        {
            get
            {
                Vector2 s = Size;
                return new Rect(-s.x * 0.5f, -s.y * 0.5f, s.x, s.y);
            }
        }

        /// <summary>正規化 (0..1) → 盤面ローカル座標。padding で縁に SOUL 半径ぶんの余白を確保。</summary>
        public Vector2 NormalizedToLocal(Vector2 normalized, float padding)
        {
            Vector2 s = Size;
            float halfW = Mathf.Max(0f, s.x * 0.5f - padding);
            float halfH = Mathf.Max(0f, s.y * 0.5f - padding);
            return new Vector2(
                Mathf.Lerp(-halfW, halfW, normalized.x),
                Mathf.Lerp(-halfH, halfH, normalized.y));
        }

        /// <summary>ローカル座標が盤面内 (+margin) にあるか。</summary>
        public bool ContainsLocal(Vector2 local, float margin)
        {
            Vector2 s = Size;
            return Mathf.Abs(local.x) <= s.x * 0.5f + margin
                && Mathf.Abs(local.y) <= s.y * 0.5f + margin;
        }
    }
}
