using System;
using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// HP 管理と被弾無敵 (i-frame)。値の変化と死亡をイベントで通知し、
    /// UI 等の表示は購読側に任せる (SRP: Health は数値だけを司る)。
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 20;
        [Tooltip("被弾後の無敵時間 [s]")]
        [SerializeField] private float _invulnerableSeconds = 0.5f;

        private int _current;
        private float _invulnerableUntil;

        /// <summary>(現在HP, 最大HP)。</summary>
        public event Action<int, int> Changed;
        public event Action Died;

        public int Current => _current;
        public int Max => _maxHp;
        public bool IsAlive => _current > 0;

        private void Awake() => _current = _maxHp;
        private void Start() => Changed?.Invoke(_current, _maxHp);

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || _current <= 0 || Time.time < _invulnerableUntil)
            {
                return;
            }
            _invulnerableUntil = Time.time + _invulnerableSeconds;
            _current = Mathf.Max(0, _current - amount);
            Changed?.Invoke(_current, _maxHp);
            if (_current == 0)
            {
                Died?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || _current <= 0)
            {
                return;
            }
            _current = Mathf.Min(_maxHp, _current + amount);
            Changed?.Invoke(_current, _maxHp);
        }

        public void ResetHealth()
        {
            _current = _maxHp;
            _invulnerableUntil = 0f;
            Changed?.Invoke(_current, _maxHp);
        }
    }
}
