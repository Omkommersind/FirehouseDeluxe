using System.Collections;
using UnityEngine;

namespace FirehouseDeluxe.Rescue
{
    [DisallowMultipleComponent]
    public sealed class RescuePersonVoice : MonoBehaviour
    {
        [SerializeField] private AudioClip neural1;
        [SerializeField] private AudioClip neural2;
        [SerializeField] private AudioClip fallCry;
        [SerializeField] private AudioClip whatIsIt;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Chance of one random line when a passenger or group appears.")]
        private float appearanceChance = 0.2f;

        private RescuePerson person;
        private AudioSource source;
        private bool hasFallen;

        private void Awake()
        {
            person = GetComponentInParent<RescuePerson>();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private void OnEnable()
        {
            if (person != null)
                person.FallStarted += OnFallStarted;
        }

        public void PlayAppearance()
        {
            if (!hasFallen && Random.value < appearanceChance)
                Play(Random.Range(0, 2) == 0 ? neural1 : neural2);
        }

        private void OnFallStarted()
        {
            if (hasFallen)
                return;

            hasFallen = true;
            // Stay in the round's scene, but outlive the passenger's immediate despawn.
            transform.SetParent(person.transform.parent, true);
            person.FallStarted -= OnFallStarted;
            person = null;
            StartCoroutine(PlayFallVoice());
        }

        private IEnumerator PlayFallVoice()
        {
            AudioClip clip = Random.Range(0, 2) == 0 ? fallCry : whatIsIt;
            double endsAt = Play(clip);
            // A newly queued source may not report playback yet; keep it alive for the full clip.
            while (AudioSettings.dspTime < endsAt)
                yield return null;
            Destroy(gameObject);
        }

        private double Play(AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            if (clip == null)
                return AudioSettings.dspTime;

            double startsAt = AudioSettings.dspTime + 0.05;
            source.PlayScheduled(startsAt);
            return startsAt + (double)clip.samples / clip.frequency;
        }

        private void OnDisable()
        {
            if (person != null)
                person.FallStarted -= OnFallStarted;
            source.Stop();
        }
    }
}
