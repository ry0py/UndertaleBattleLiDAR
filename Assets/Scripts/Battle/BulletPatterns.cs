using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 盤面の縁から SOUL を狙って弾を撃つパターン。SOUL 位置は emitter 越しに取得する (DIP)。
    /// </summary>
    public sealed class AimedShotPattern : IBulletPattern
    {
        private readonly float _interval;
        private readonly float _speed;
        private float _timer;

        public AimedShotPattern(float interval, float speed)
        {
            _interval = Mathf.Max(0.05f, interval);
            _speed = speed;
        }

        public void Tick(float deltaTime, IBulletEmitter emitter)
        {
            _timer += deltaTime;
            while (_timer >= _interval)
            {
                _timer -= _interval;
                Rect r = emitter.BoardLocalRect;
                Vector2 from = RandomEdgePoint(r);
                Vector2 dir = emitter.SoulLocalPosition - from;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
                emitter.Emit(from, dir.normalized * _speed);
            }
        }

        private static Vector2 RandomEdgePoint(Rect r)
        {
            switch (Random.Range(0, 4))
            {
                case 0: return new Vector2(Random.Range(r.xMin, r.xMax), r.yMax);
                case 1: return new Vector2(Random.Range(r.xMin, r.xMax), r.yMin);
                case 2: return new Vector2(r.xMin, Random.Range(r.yMin, r.yMax));
                default: return new Vector2(r.xMax, Random.Range(r.yMin, r.yMax));
            }
        }
    }

    /// <summary>
    /// 複数パターンを同時に走らせる合成パターン (OCP: 既存パターンを変更せず弾幕を厚くできる)。
    /// </summary>
    public sealed class CompositePattern : IBulletPattern
    {
        private readonly IBulletPattern[] _patterns;

        public CompositePattern(params IBulletPattern[] patterns)
        {
            _patterns = patterns ?? System.Array.Empty<IBulletPattern>();
        }

        public void Tick(float deltaTime, IBulletEmitter emitter)
        {
            foreach (IBulletPattern p in _patterns)
            {
                p?.Tick(deltaTime, emitter);
            }
        }
    }
}
