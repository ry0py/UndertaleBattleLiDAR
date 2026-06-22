using UnityEngine;

namespace UndertaleLiDAR.Input
{
    /// <summary>
    /// 主入力が有効ならそれを、無効なら副入力を使う合成入力源。
    /// 例: LiDAR がハートを見失った間だけキーボードに切り替える。
    /// 入力源の組み合わせを「新クラス無し」で表現でき、Battle 層は変更不要 (OCP)。
    /// </summary>
    public sealed class FallbackInputSource : IHeartInputSource
    {
        private readonly IHeartInputSource _primary;
        private readonly IHeartInputSource _fallback;

        public FallbackInputSource(IHeartInputSource primary, IHeartInputSource fallback)
        {
            _primary = primary;
            _fallback = fallback;
        }

        public bool TryGetNormalizedPosition(out Vector2 normalized)
        {
            if (_primary != null && _primary.TryGetNormalizedPosition(out normalized))
            {
                return true;
            }
            if (_fallback != null)
            {
                return _fallback.TryGetNormalizedPosition(out normalized);
            }
            normalized = Vector2.zero;
            return false;
        }
    }
}
