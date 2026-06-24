using UndertaleLiDAR.Input;
using UndertaleLiDAR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// SOUL (ハート) を入力源の正規化座標に従って盤面内へ配置する。
    /// 入力源の正体 (LiDAR/キーボード) は知らない。座標確定は BulletBoard に委譲 (SRP/DIP)。
    /// メニュー中は SetVisible(false) で表示と移動だけを止める。GameObject は常時 active のままにし、
    /// 同居する Health が止まらないようにする (HP が 0 表示になる不具合の回避)。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SoulController : MonoBehaviour
    {
        [SerializeField] private BulletBoard _board;
        [Tooltip("SOUL の当たり判定半径 (px)。縁の余白と衝突判定に使う。")]
        [SerializeField] private float _radius = 8f;

        private RectTransform _rect;
        private Image _image;
        private IHeartInputSource _input;
        private bool _visible = true;

        public float Radius => _radius;
        public Vector2 LocalPosition => _rect.anchoredPosition;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _image = GetComponent<Image>();
            if (_image != null)
            {
                // 四角ではなく Undertale 風のハートにする。
                _image.sprite = RuntimeSprites.Heart;
                _image.type = Image.Type.Simple;
                _image.preserveAspect = true;
                _image.color = new Color(1f, 0f, 0f, 1f);
            }
        }

        /// <summary>合成ルート (BattleManager) から入力源を注入する。</summary>
        public void SetInputSource(IHeartInputSource input) => _input = input;

        /// <summary>敵ターンのみ表示・操作可能にする。表示開始時は盤面中央へ戻す。</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_image != null)
            {
                _image.enabled = visible;
            }
            if (visible && _board != null && _rect != null)
            {
                _rect.anchoredPosition = _board.NormalizedToLocal(new Vector2(0.5f, 0.5f), _radius);
            }
        }

        private void Update()
        {
            if (!_visible || _input == null || _board == null)
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
