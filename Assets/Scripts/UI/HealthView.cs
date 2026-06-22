using TMPro;
using UndertaleLiDAR.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace UndertaleLiDAR.UI
{
    /// <summary>
    /// Health の変化を購読して HP テキスト/ゲージへ反映する表示専用コンポーネント。
    /// 表示ロジックをここに隔離し、Health 本体は数値だけに集中させる (SRP)。
    /// </summary>
    public sealed class HealthView : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private TMP_Text _label;
        [Tooltip("Image Type=Filled を割り当てると残量ゲージになる (任意)")]
        [SerializeField] private Image _fill;

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }
            _health.Changed += OnChanged;
            OnChanged(_health.Current, _health.Max); // 購読時点の値で即初期化
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Changed -= OnChanged;
            }
        }

        private void OnChanged(int current, int max)
        {
            if (_label != null)
            {
                _label.text = $"HP {current}/{max}";
            }
            if (_fill != null)
            {
                _fill.fillAmount = max > 0 ? (float)current / max : 0f;
            }
        }
    }
}
