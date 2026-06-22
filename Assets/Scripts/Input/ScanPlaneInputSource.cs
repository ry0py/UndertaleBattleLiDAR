using MediaFrontJapan.SCIP;
using Unity.Collections;
using Unity.Mathematics;
using UndertaleLiDAR.Mapping;
using UnityEngine;

namespace UndertaleLiDAR.Input
{
    /// <summary>
    /// urg-unity (MediaFrontJapan.SCIP) の <see cref="SCIPScanPlane"/> を入力源に変換するアダプタ。
    /// ライブラリが担う「接続・背景除去・物体検出」をそのまま活用し (DRY/車輪の再発明をしない)、
    /// 検出物体 (センサー座標系メートル) を既存の <see cref="ICoordinateMapper"/> で正規化する。
    /// 直前位置に最も近い物体を選ぶことで、複数検出時もハートを安定して追従する。
    /// </summary>
    public sealed class ScanPlaneInputSource : IHeartInputSource
    {
        private readonly SCIPScanPlane _scanPlane;
        private readonly ICoordinateMapper _mapper;
        private Vector2 _last = new Vector2(0.5f, 0.5f);

        public ScanPlaneInputSource(SCIPScanPlane scanPlane, ICoordinateMapper mapper)
        {
            _scanPlane = scanPlane;
            _mapper = mapper;
        }

        public bool TryGetNormalizedPosition(out Vector2 normalized)
        {
            normalized = _last;
            if (_scanPlane == null)
            {
                return false;
            }

            NativeArray<float2> objects = _scanPlane.ObjectLocalPositions;
            if (!objects.IsCreated || objects.Length == 0)
            {
                return false;
            }

            // 直前の正規化位置に最も近い検出物体を採用 (連続性のため)。
            float bestSqr = float.MaxValue;
            Vector2 best = _last;
            for (int i = 0; i < objects.Length; i++)
            {
                float2 p = objects[i];
                Vector2 candidate = _mapper.ToNormalized(new Vector2(p.x, p.y));
                float sqr = (candidate - _last).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            _last = best;
            normalized = best;
            return true;
        }
    }
}
