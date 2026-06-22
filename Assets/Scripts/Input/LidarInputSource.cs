using UndertaleLiDAR.LiDAR;
using UndertaleLiDAR.Mapping;
using UndertaleLiDAR.Tracking;
using UnityEngine;

namespace UndertaleLiDAR.Input
{
    /// <summary>
    /// LiDAR パイプライン (センサー → 検出 → 変換) を 1 本の入力源に合成するアダプタ。
    /// 自前のロジックを持たず、各層を順に呼ぶだけ (SRP/合成)。
    /// </summary>
    public sealed class LidarInputSource : IHeartInputSource
    {
        private readonly ILidarSensor _sensor;
        private readonly IHeartTracker _tracker;
        private readonly ICoordinateMapper _mapper;

        public LidarInputSource(ILidarSensor sensor, IHeartTracker tracker, ICoordinateMapper mapper)
        {
            _sensor = sensor;
            _tracker = tracker;
            _mapper = mapper;
        }

        public bool TryGetNormalizedPosition(out Vector2 normalized)
        {
            normalized = Vector2.zero;
            if (!_sensor.TryGetLatestScan(out LidarScan scan))
            {
                return false;
            }
            if (!_tracker.TryTrack(scan, out Vector2 physicalM))
            {
                return false;
            }
            normalized = _mapper.ToNormalized(physicalM);
            return true;
        }
    }
}
