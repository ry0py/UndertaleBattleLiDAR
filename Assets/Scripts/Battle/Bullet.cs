using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 1 発の弾。盤面ローカル座標を等速移動し、SOUL と円-円衝突したらダメージを与える。
    /// 盤面外に出たら自滅。物理エンジンを使わず決定論的に判定する (KISS)。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class Bullet : MonoBehaviour
    {
        private const float OffBoardMargin = 4f;

        private RectTransform _rect;
        private Vector2 _velocity;   // px/s (盤面ローカル)
        private float _radius;
        private int _damage;
        private SoulController _soul;
        private Health _health;
        private BulletBoard _board;
        private bool _initialized;

        private void Awake() => _rect = (RectTransform)transform;

        /// <summary>スポーン時に BulletSpawner が必要な依存と初期値を注入する。</summary>
        public void Initialize(Vector2 velocityLocal, float radius, int damage,
            SoulController soul, Health health, BulletBoard board)
        {
            _velocity = velocityLocal;
            _radius = radius;
            _damage = damage;
            _soul = soul;
            _health = health;
            _board = board;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            _rect.anchoredPosition += _velocity * Time.deltaTime;

            if (!_board.ContainsLocal(_rect.anchoredPosition, _radius + OffBoardMargin))
            {
                Destroy(gameObject);
                return;
            }

            float hitDist = _radius + _soul.Radius;
            if ((_rect.anchoredPosition - _soul.LocalPosition).sqrMagnitude <= hitDist * hitDist)
            {
                _health.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
