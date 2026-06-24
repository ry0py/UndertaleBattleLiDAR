using UnityEngine;

namespace UndertaleLiDAR.UI
{
    /// <summary>
    /// コードからピクセルアートの Sprite を生成して提供する (アセット import に依存しない)。
    /// 生成物は静的にキャッシュし、Play 中 1 度だけ作る。白で作っておき Image.color で着色する。
    /// </summary>
    public static class RuntimeSprites
    {
        private static Sprite _heart;
        private static Sprite _solid;

        /// <summary>Undertale 風のドット絵ハート (白)。Image.color を赤にして使う。</summary>
        public static Sprite Heart => _heart != null ? _heart : (_heart = BuildHeart());

        /// <summary>角丸の無い完全な白矩形。HP ゲージ等のソリッド塗りに使う。</summary>
        public static Sprite Solid => _solid != null ? _solid : (_solid = BuildSolid());

        private static Sprite BuildSolid()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite BuildHeart()
        {
            // 1 = 塗り。左右対称のハート (13x11)。
            string[] rows =
            {
                "..XXX...XXX..",
                ".XXXXX.XXXXX.",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX",
                ".XXXXXXXXXXX.",
                "..XXXXXXXXX..",
                "...XXXXXXX...",
                "....XXXXX....",
                ".....XXX.....",
                "......X......",
            };
            int w = rows[0].Length;
            int h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color32(0, 0, 0, 0);
            var fill = new Color32(255, 255, 255, 255);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = rows[h - 1 - y]; // テクスチャは下から上なので行を反転
                for (int x = 0; x < w; x++)
                {
                    px[y * w + x] = row[x] == 'X' ? fill : clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }
    }
}
