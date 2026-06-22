using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 盤面上端のランダムな位置から弾が等間隔で降ってくる基本パターン。
    /// パターン固有の状態 (発射タイマー) は自身が保持し、Spawner は状態を持たない。
    /// </summary>
    public sealed class RandomRainPattern : IBulletPattern
    {
        private readonly float _interval;
        private readonly float _speed;
        private float _timer;

        public RandomRainPattern(float interval, float speed)
        {
            _interval = Mathf.Max(0.02f, interval);
            _speed = speed;
        }

        public void Tick(float deltaTime, IBulletEmitter emitter)
        {
            _timer += deltaTime;
            while (_timer >= _interval)
            {
                _timer -= _interval;
                Rect r = emitter.BoardLocalRect;
                float x = Random.Range(r.xMin, r.xMax);
                emitter.Emit(new Vector2(x, r.yMax), new Vector2(0f, -_speed));
            }
        }
    }
}
