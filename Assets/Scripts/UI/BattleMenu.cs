using System;
using UnityEngine;
using UnityEngine.UI;

namespace UndertaleLiDAR.UI
{
    /// <summary>
    /// Undertale 風の 4 ボタン (FIGHT / ACT / ITEM / MERCY)。
    /// 押下を C# イベントで通知するだけで、バトル進行は購読側 (BattleDirector) に委ねる (SRP)。
    /// </summary>
    public sealed class BattleMenu : MonoBehaviour
    {
        [SerializeField] private Button _fight;
        [SerializeField] private Button _act;
        [SerializeField] private Button _item;
        [SerializeField] private Button _mercy;

        public event Action Fight;
        public event Action Act;
        public event Action Item;
        public event Action Mercy;

        private void Awake()
        {
            if (_fight != null) _fight.onClick.AddListener(() => Fight?.Invoke());
            if (_act != null) _act.onClick.AddListener(() => Act?.Invoke());
            if (_item != null) _item.onClick.AddListener(() => Item?.Invoke());
            if (_mercy != null) _mercy.onClick.AddListener(() => Mercy?.Invoke());
        }

        /// <summary>全ボタンの操作可否をまとめて切り替える (敵ターン中は無効化)。</summary>
        public void SetInteractable(bool on)
        {
            if (_fight != null) _fight.interactable = on;
            if (_act != null) _act.interactable = on;
            if (_item != null) _item.interactable = on;
            if (_mercy != null) _mercy.interactable = on;
        }
    }
}
