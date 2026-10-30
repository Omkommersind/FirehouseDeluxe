using UnityEngine;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip track;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float minigameVolumeMultiplier = 0.5f;
        [SerializeField, Min(0f), Tooltip("Seconds to transition between normal and minigame volume.")]
        private float volumeTransitionDuration = 0.2f;

        private AudioSource source;
        private float volumeMultiplier = 1f;
        private bool ducked;

        internal static BackgroundMusic CreateForRun()
        {
            var prefab = Resources.Load<BackgroundMusic>("BackgroundMusic");
            if (prefab == null)
            {
                Debug.LogError("BackgroundMusic: missing Resources/BackgroundMusic.prefab.");
                return null;
            }

            BackgroundMusic music = Instantiate(prefab);
            music.name = "Background Music";
            return music;
        }

        private void Awake()
        {
            // The map is disabled during rescue missions; music must remain active independently.
            DontDestroyOnLoad(gameObject);
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = volume;
            source.clip = track;
            if (track != null)
                source.Play();
            else
                Debug.LogError("BackgroundMusic: assign the music track in the prefab.", this);
        }

        internal void SetDucked(bool value) => ducked = value;

        private void Update()
        {
            float target = ducked ? minigameVolumeMultiplier : 1f;
            volumeMultiplier = volumeTransitionDuration > 0f
                ? Mathf.MoveTowards(volumeMultiplier, target,
                    Time.unscaledDeltaTime / volumeTransitionDuration)
                : target;
            source.volume = volume * volumeMultiplier;
        }

        internal void EndRun()
        {
            source.Stop();
            Destroy(gameObject);
        }
    }
}
