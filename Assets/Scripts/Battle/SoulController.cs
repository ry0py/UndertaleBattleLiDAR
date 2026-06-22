using UndertaleLiDAR.Input;
using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// SOUL (ハート) を入力源の正規化座標に従って盤面内へ配置する。
    /// 入力源の正体 (LiDAR/キーボード) は知らない。座標確定は BulletBoard に委譲 (SRP/DIP)。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SoulController : MonoBehaviour
    {
        [SerializeField] private BulletBoard _board;
        [Tooltip("SOUL の当たり判定半径 (px)。縁の余白と衝突判定に使う。")]
        [SerializeField] private float _radius = 8f;

        private RectTransform _rect;
        private IHeartInputSource _input;

        public float Radius => _radius;
        public Vector2 LocalPosition => _rect.anchoredPosition;

        private void Awake() => _rect = (RectTransform)transform;

        /// <summary>合成ルート (BattleManager) から入力源を注入する。</summary>
        public void SetInputSource(IHeartInputSource input) => _input = input;

        private void Update()
        {
            if (_input == null || _board == null)
            {
                return;
            }
            if (_input.TryGetNormalizedPosition(out Vector2 normalized))
            {
                _rect.anchoredPosition = _board.NormalizedToLocal(normalized, _radius);
            }
        }
    }
}
