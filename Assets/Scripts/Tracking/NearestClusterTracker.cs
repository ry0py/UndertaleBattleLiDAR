using UndertaleLiDAR.LiDAR;
using UnityEngine;

namespace UndertaleLiDAR.Tracking
{
    /// <summary>
    /// 「最も近い点」を核に、半径内の点を 1 クラスタとみなし重心を返すシンプルな検出器。
    /// 手で持つハートはセンサーに最も近い物体である、という前提に基づく (KISS)。
    /// </summary>
    public sealed class NearestClusterTracker : IHeartTracker
    {
        private readonly float _clusterRadiusM;
        private readonly int _minPoints;

        public NearestClusterTracker(float clusterRadiusM, int minPoints)
        {
            _clusterRadiusM = Mathf.Max(0.001f, clusterRadiusM);
            _minPoints = Mathf.Max(1, minPoints);
        }

        public bool TryTrack(LidarScan scan, out Vector2 positionM)
        {
            positionM = Vector2.zero;
            if (scan == null || scan.Count == 0)
            {
                return false;
            }

            // 1) 最近点を核として選ぶ。
            int nearestIndex = -1;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < scan.Count; i++)
            {
                float d = scan[i].DistanceM;
                float sqr = d * d;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestIndex = i;
                }
            }

            // 2) 核の近傍 (半径内) を集めて重心を取る。
            Vector2 core = scan[nearestIndex].ToCartesian();
            float radiusSqr = _clusterRadiusM * _clusterRadiusM;
            Vector2 sum = Vector2.zero;
            int count = 0;
            for (int i = 0; i < scan.Count; i++)
            {
                Vector2 p = scan[i].ToCartesian();
                if ((p - core).sqrMagnitude <= radiusSqr)
                {
                    sum += p;
                    count++;
                }
            }

            if (count < _minPoints)
            {
                return false;
            }

            positionM = sum / count;
            return true;
        }
    }
}
