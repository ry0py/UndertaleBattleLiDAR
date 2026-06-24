using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace UndertaleLiDAR.UI
{
    /// <summary>
    /// コードから AddComponent された InputSystemUIInputModule は UI アクション (Point/Click 等) が
    /// 未割当で、ボタンのクリックを処理できない。シーンを編集せず、起動時に既定 UI アクションを
    /// 割り当てて UI 入力を有効化する (ボタンが反応しない不具合の対策)。
    /// </summary>
    public static class UIInputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureUiActions()
        {
            var module = Object.FindFirstObjectByType<InputSystemUIInputModule>(FindObjectsInactive.Include);
            if (module == null)
            {
                return;
            }
            // 未設定 (actionsAsset も point も無い) の場合だけ既定アクションを割り当てる。
            // OnEnable 後に割り当てるため、enabled をトグルしてアクションを確実に有効化する。
            if (module.actionsAsset == null || module.point == null)
            {
                bool wasEnabled = module.enabled;
                module.enabled = false;
                module.AssignDefaultActions();
                module.enabled = wasEnabled;
            }
        }
    }
}
