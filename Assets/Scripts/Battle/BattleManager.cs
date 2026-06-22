using System;
using MediaFrontJapan.SCIP;
using UndertaleLiDAR.Config;
using UndertaleLiDAR.Input;
using UndertaleLiDAR.LiDAR;
using UndertaleLiDAR.Mapping;
using UndertaleLiDAR.Tracking;
using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>SOUL を動かす入力経路の選択。</summary>
    public enum InputMode
    {
        /// <summary>urg-unity (UST-10LX) の SCIPScanPlane を使う実機経路。</summary>
        ScanPlane,
        /// <summary>マウス駆動の擬似 LiDAR。実機なしの開発・デモ用。</summary>
        Mock,
        /// <summary>自前 SCIP シリアル実装 (URG-04LX 等)。URG_SERIAL_ENABLED 必須。</summary>
        HokuyoSerial,
    }

    /// <summary>
    /// 合成ルート (Composition Root)。各層の具象をここで 1 度だけ生成・結線する。
    /// 他のクラスは具象を new せず、インターフェース越しに協調する (DIP)。
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private LidarSettings _settings;
        [SerializeField] private InputMode _inputMode = InputMode.ScanPlane;

        [Header("実機 (ScanPlane: urg-unity)")]
        [Tooltip("SCIPInputModules.prefab 内の SCIPScanPlane を割り当てる")]
        [SerializeField] private SCIPScanPlane _scanPlane;

        [Header("入力フォールバック")]
        [SerializeField] private bool _keyboardFallback = true;
        [Tooltip("キーボード移動速度 (正規化単位/秒)")]
        [SerializeField] private float _keyboardSpeed = 0.8f;

        [Header("シーン参照")]
        [SerializeField] private SoulController _soul;
        [SerializeField] private Health _health;
        [SerializeField] private BulletSpawner _spawner;

        private ILidarSensor _sensor; // Mock/HokuyoSerial 経路でのみ生成

        private void Awake()
        {
            if (!ValidateReferences())
            {
                return;
            }

            // mapper は全経路共通 (物理[m] → 正規化)。tracker は LiDAR 生スキャン経路でのみ使用。
            ICoordinateMapper mapper = new RectCoordinateMapper(
                _settings.PhysicalMin, _settings.PhysicalMax,
                _settings.RotationDeg, _settings.InvertX, _settings.InvertY);

            IHeartInputSource primary = BuildPrimaryInput(mapper);

            IHeartInputSource input = _keyboardFallback
                ? new FallbackInputSource(primary, new KeyboardInputSource(_keyboardSpeed))
                : primary;

            _soul.SetInputSource(input);
            _health.Died += OnDied;
        }

        /// <summary>選択された経路に応じた主入力源を組み立てる (唯一の具象生成点)。</summary>
        private IHeartInputSource BuildPrimaryInput(ICoordinateMapper mapper)
        {
            switch (_inputMode)
            {
                case InputMode.ScanPlane:
                    if (_scanPlane == null)
                    {
                        Debug.LogError("[BattleManager] InputMode.ScanPlane だが SCIPScanPlane 未設定。" +
                                       "キーボードのみで継続します。");
                        return null;
                    }
                    return new ScanPlaneInputSource(_scanPlane, mapper);

                case InputMode.HokuyoSerial:
                    return BuildLidarPipeline(new HokuyoUrgSensor(_settings), mapper);

                case InputMode.Mock:
                default:
                    return BuildLidarPipeline(new MockLidarSensor(
                        _settings.PhysicalMin, _settings.PhysicalMax,
                        _settings.ClusterRadiusM, _settings.MinClusterPoints), mapper);
            }
        }

        /// <summary>生スキャン系センサー (Mock/Serial) を 検出→変換 と合成する。</summary>
        private IHeartInputSource BuildLidarPipeline(ILidarSensor sensor, ICoordinateMapper mapper)
        {
            _sensor = sensor;
            IHeartTracker tracker = new NearestClusterTracker(
                _settings.ClusterRadiusM, _settings.MinClusterPoints);
            try
            {
                _sensor.Connect();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning("[BattleManager] LiDAR 接続失敗。キーボードのみで継続します。");
            }
            return new LidarInputSource(_sensor, tracker, mapper);
        }

        private void OnDied()
        {
            _spawner.SetActive(false);
            Debug.Log("Game Over");
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
            _sensor?.Dispose();
        }

        private bool ValidateReferences()
        {
            if (_settings == null) { Debug.LogError("[BattleManager] LidarSettings 未設定。"); return false; }
            if (_soul == null) { Debug.LogError("[BattleManager] SoulController 未設定。"); return false; }
            if (_health == null) { Debug.LogError("[BattleManager] Health 未設定。"); return false; }
            if (_spawner == null) { Debug.LogError("[BattleManager] BulletSpawner 未設定。"); return false; }
            return true;
        }
    }
}
