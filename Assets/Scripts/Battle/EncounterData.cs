using UnityEngine;

namespace UndertaleLiDAR.Battle
{
    /// <summary>
    /// 1 戦闘ぶんのデータ (敵名・セリフ・ターン設定)。コードを書き換えずに
    /// 戦闘内容を差し替えられるよう外部化する (LidarSettings と同じ集約方針: DRY)。
    /// </summary>
    [CreateAssetMenu(fileName = "Encounter", menuName = "UndertaleLiDAR/Encounter")]
    public sealed class EncounterData : ScriptableObject
    {
        [Header("敵")]
        public string EnemyName = "FROGGIT";

        [Header("導入セリフ (戦闘開始時)")]
        [TextArea] public string[] IntroLines =
        {
            "FROGGIT が おそいかかってきた！",
            "ハートを うごかして こうげきを よけよう。",
        };

        [Header("毎ターンの説明 (メニュー中に順番に表示)")]
        [TextArea] public string[] TurnLines =
        {
            "FROGGIT は なにか いいたげだ。",
            "あたりに わるい よかんが ただよう。",
            "FROGGIT は きみの ようすを みている。",
        };

        [Header("各メニューの結果セリフ")]
        [TextArea] public string FightLine = "きみは こうげきした！ FROGGIT は ひるんでいる。";
        [TextArea] public string ActLine = "きみは FROGGIT を なでた。 FROGGIT は こまっている。";
        [TextArea] public string ItemLine = "きみは ほうたい を つかった。 HP が かいふくした。";
        [TextArea] public string MercyLine = "きみは FROGGIT を みのがした。";
        [TextArea] public string CannotSpareLine = "まだ みのがせない みたいだ。";
        [TextArea] public string SpareReadyLine = "FROGGIT の なまえが きいろい。 いまなら みのがせる！";
        [TextArea] public string VictoryLine = "きみは こころ を まもりぬいた！  YOU WON!";
        [TextArea] public string GameOverLine = "きみの こころ が くだけちった……";

        [Header("ターン設定")]
        [Tooltip("敵ターン (弾幕回避) の秒数")] public float EnemyTurnSeconds = 8f;
        [Tooltip("ITEM で回復する HP")] public int ItemHeal = 10;
        [Tooltip("ACT を何回でみのがせるようになるか")] public int ActsToSpare = 2;
    }
}
