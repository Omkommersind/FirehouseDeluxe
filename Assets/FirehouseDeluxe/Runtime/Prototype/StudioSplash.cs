using UnityEngine;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class StudioSplash : StartupScreen
    {
        [SerializeField, Min(0f)] private float displayDuration = 3f;

        protected override string NextScenePath => "Assets/Scenes/GameIntro.unity";
        protected override double DisplayDuration => displayDuration;
    }
}
