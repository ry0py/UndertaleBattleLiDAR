using System;
using System.Collections;
using UndertaleLiDAR.Audio;
using UndertaleLiDAR.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// Undertale 風のターン制バトル進行を司る:
    /// 導入セリフ → メニュー (FIGHT/ACT/ITEM/MERCY) → 敵ターン (弾幕回避) → メニュー …。
    /// 入力合成は BattleManager、HP は Health、表示は DialogueBox/BattleMenu に委譲し、
    /// ここは「状態遷移」だけに責任を持つ (SRP)。盤面の表示/弾幕モードもここが切り替える。
    /// </summary>
    public sealed class BattleDirector : MonoBehaviour
    {
        private enum State { Intro, Menu, Resolve, EnemyTurn, Victory, GameOver }

        [Header("データ")]
        [SerializeField] private EncounterData _encounter;

        [Header("参照")]
        [SerializeField] private DialogueBox _dialogue;
        [SerializeField] private BattleMenu _menu;
        [SerializeField] private BulletSpawner _spawner;
        [SerializeField] private SoulController _soul;
        [SerializeField] private Health _health;
        [SerializeField] private MusicManager _music;

        private State _state;
        private Action _afterDialogue;
        private int _actCount;
        private bool _canSpare;
        private int _turnLineIndex;
        private Coroutine _turnRoutine;

        private void Awake()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }
            _dialogue.Finished += OnDialogueFinished;
            _menu.Fight += OnFight;
            _menu.Act += OnAct;
            _menu.Item += OnItem;
            _menu.Mercy += OnMercy;
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_dialogue != null) _dialogue.Finished -= OnDialogueFinished;
            if (_menu != null)
            {
                _menu.Fight -= OnFight;
                _menu.Act -= OnAct;
                _menu.Item -= OnItem;
                _menu.Mercy -= OnMercy;
            }
            if (_health != null) _health.Died -= OnDied;
        }

        private void Start() => EnterIntro();

        private void Update()
        {
            // テキスト系の状態でのみ Z/Enter/Space で送り (メニュー/敵ターンでは無視)。
            if (_state != State.Intro && _state != State.Resolve
                && _state != State.Victory && _state != State.GameOver)
            {
                return;
            }
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.zKey.wasPressedThisFrame
                || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                _dialogue.Advance();
            }
        }

        // --- 状態遷移 ---

        private void EnterIntro()
        {
            _state = State.Intro;
            SetTextMode();
            _menu.SetInteractable(false);
            _afterDialogue = EnterMenu;
            _dialogue.Show(_encounter.IntroLines);
        }

        private void EnterMenu()
        {
            _state = State.Menu;
            SetTextMode();
            _afterDialogue = null;
            _menu.SetInteractable(true);
            _dialogue.Show(_canSpare ? _encounter.SpareReadyLine : NextTurnLine());
        }

        private void EnterResolve(string line, Action after)
        {
            _state = State.Resolve;
            SetTextMode();
            _menu.SetInteractable(false);
            _afterDialogue = after;
            _dialogue.Show(line);
        }

        private void EnterEnemyTurn()
        {
            _state = State.EnemyTurn;
            _menu.SetInteractable(false);
            SetBulletMode();
            _turnRoutine = StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            float dur = Mathf.Max(1f, _encounter.EnemyTurnSeconds);
            float t = 0f;
            while (t < dur && _state == State.EnemyTurn)
            {
                t += Time.deltaTime;
                yield return null;
            }
            _turnRoutine = null;
            if (_state == State.EnemyTurn)
            {
                EnterMenu();
            }
        }

        private void EnterVictory()
        {
            _state = State.Victory;
            StopEnemyTurn();
            SetTextMode();
            _menu.SetInteractable(false);
            _afterDialogue = null;
            if (_music != null) _music.FadeOutBgm(1.5f);
            _dialogue.Show(_encounter.VictoryLine);
        }

        private void EnterGameOver()
        {
            _state = State.GameOver;
            StopEnemyTurn();
            SetTextMode();
            _menu.SetInteractable(false);
            _afterDialogue = null;
            if (_music != null) _music.FadeOutBgm(1.5f);
            _dialogue.Show(_encounter.GameOverLine);
        }

        // --- メニュー押下 ---

        private void OnFight()
        {
            if (_state != State.Menu) return;
            EnterResolve(_encounter.FightLine, EnterEnemyTurn);
        }

        private void OnAct()
        {
            if (_state != State.Menu) return;
            _actCount++;
            if (_actCount >= Mathf.Max(1, _encounter.ActsToSpare))
            {
                _canSpare = true;
            }
            EnterResolve(_encounter.ActLine, EnterEnemyTurn);
        }

        private void OnItem()
        {
            if (_state != State.Menu) return;
            _health.Heal(_encounter.ItemHeal);
            EnterResolve(_encounter.ItemLine, EnterEnemyTurn);
        }

        private void OnMercy()
        {
            if (_state != State.Menu) return;
            if (_canSpare)
            {
                EnterResolve(_encounter.MercyLine, EnterVictory);
            }
            else
            {
                EnterResolve(_encounter.CannotSpareLine, EnterEnemyTurn);
            }
        }

        private void OnDialogueFinished()
        {
            Action cb = _afterDialogue;
            _afterDialogue = null;
            cb?.Invoke();
        }

        private void OnDied()
        {
            if (_state == State.GameOver) return;
            EnterGameOver();
        }

        // --- 盤面モード ---

        /// <summary>セリフ表示モード: 弾幕を止め SOUL を隠し、テキスト枠を出す。</summary>
        private void SetTextMode()
        {
            _spawner.SetActive(false);
            _spawner.ClearBullets();
            if (_soul != null) _soul.gameObject.SetActive(false);
            _dialogue.gameObject.SetActive(true);
        }

        /// <summary>弾幕モード: テキストを消し SOUL を出し、弾幕を開始する。</summary>
        private void SetBulletMode()
        {
            _dialogue.Clear();
            _dialogue.gameObject.SetActive(false);
            if (_soul != null) _soul.gameObject.SetActive(true);
            _spawner.SetActive(true);
        }

        private void StopEnemyTurn()
        {
            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }
            _spawner.SetActive(false);
        }

        private string NextTurnLine()
        {
            string[] lines = _encounter.TurnLines;
            if (lines == null || lines.Length == 0)
            {
                return _encounter.EnemyName + " が ようすを うかがっている。";
            }
            string line = lines[_turnLineIndex % lines.Length];
            _turnLineIndex++;
            return line;
        }

        private bool Validate()
        {
            if (_encounter == null) { Debug.LogError("[BattleDirector] EncounterData 未設定。"); return false; }
            if (_dialogue == null) { Debug.LogError("[BattleDirector] DialogueBox 未設定。"); return false; }
            if (_menu == null) { Debug.LogError("[BattleDirector] BattleMenu 未設定。"); return false; }
            if (_spawner == null) { Debug.LogError("[BattleDirector] BulletSpawner 未設定。"); return false; }
            if (_soul == null) { Debug.LogError("[BattleDirector] SoulController 未設定。"); return false; }
            if (_health == null) { Debug.LogError("[BattleDirector] Health 未設定。"); return false; }
            return true;
        }
    }
}
