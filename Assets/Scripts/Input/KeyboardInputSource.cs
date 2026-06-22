using UnityEngine;
using UnityEngine.InputSystem;

namespace UndertaleLiDAR.Input
{
    /// <summary>
    /// LiDAR が使えない/未検出のときのフォールバック入力。
    /// 矢印・WASD で正規化位置を相対移動させる。Input System を使用 (旧 Input は使わない)。
    /// </summary>
    public sealed class KeyboardInputSource : IHeartInputSource
    {
        private readonly float _speedPerSec; // 正規化単位/秒
        private Vector2 _position = new Vector2(0.5f, 0.5f);

        public KeyboardInputSource(float speedPerSec)
        {
            _speedPerSec = Mathf.Max(0.01f, speedPerSec);
        }

        public bool TryGetNormalizedPosition(out Vector2 normalized)
        {
            normalized = _position;
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return false;
            }

            Vector2 dir = Vector2.zero;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y -= 1f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y += 1f;

            if (dir.sqrMagnitude > 1f) dir.Normalize();
            _position += dir * (_speedPerSec * Time.deltaTime);
            _position = new Vector2(Mathf.Clamp01(_position.x), Mathf.Clamp01(_position.y));

            normalized = _position;
            return true;
        }
    }
}
