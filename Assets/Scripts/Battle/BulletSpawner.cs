using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 弾幕パターンを駆動し、弾 Prefab を盤面に生成する (IBulletEmitter 実装)。
    /// パターン生成はこの 1 箇所 (Awake) のみが具象を知る合成点。
    /// 別パターンに差し替える場合もここだけ変更すればよい。
    /// </summary>
    public sealed class BulletSpawner : MonoBehaviour, IBulletEmitter
    {
        [Header("参照")]
        [SerializeField] private BulletBoard _board;
        [SerializeField] private SoulController _soul;
        [SerializeField] private Health _health;
        [SerializeField] private Bullet _bulletPrefab;

        [Header("弾パラメータ")]
        [SerializeField] private float _bulletRadius = 6f;
        [SerializeField] private int _bulletDamage = 3;

        [Header("パターン: Random Rain")]
        [SerializeField] private float _spawnInterval = 0.25f;
        [SerializeField] private float _bulletSpeed = 200f;
        [SerializeField] private bool _active = true;

        private IBulletPattern _pattern;

        private void Awake() => _pattern = new RandomRainPattern(_spawnInterval, _bulletSpeed);

        private void Update()
        {
            if (_active && _pattern != null)
            {
                _pattern.Tick(Time.deltaTime, this);
            }
        }

        public void SetActive(bool active) => _active = active;

        /// <summary>盤面に残っている弾をすべて消す (敵ターン終了時に呼ぶ)。</summary>
        public void ClearBullets()
        {
            if (_board == null)
            {
                return;
            }
            Bullet[] bullets = _board.GetComponentsInChildren<Bullet>(true);
            foreach (Bullet b in bullets)
            {
                if (b != null) Destroy(b.gameObject);
            }
        }

        public Rect BoardLocalRect => _board.LocalRect;
        public Vector2 SoulLocalPosition => _soul.LocalPosition;

        public void Emit(Vector2 fromLocal, Vector2 velocityLocal)
        {
            if (_bulletPrefab == null || _board == null)
            {
                return;
            }
            Bullet bullet = Instantiate(_bulletPrefab, _board.transform);
            ((RectTransform)bullet.transform).anchoredPosition = fromLocal;
            bullet.Initialize(velocityLocal, _bulletRadius, _bulletDamage, _soul, _health, _board);
        }
    }
}
