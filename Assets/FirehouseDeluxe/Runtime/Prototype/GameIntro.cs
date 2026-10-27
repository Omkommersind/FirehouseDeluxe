using UnityEngine;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class GameIntro : StartupScreen
    {
        protected override string NextScenePath => "Assets/Scenes/RoadPlayground.unity";
    }
}
