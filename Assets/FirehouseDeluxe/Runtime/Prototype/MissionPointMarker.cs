using System.Collections.Generic;
using FirehouseDeluxe.Vehicles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class MissionPointMarker : MonoBehaviour
    {
        public const int Pixels = 8;
        public const float WorldSize = 2f;
        public static readonly Color32 MarkerColor = new Color32(0x9D, 0x1C, 0x48, 255);

        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private MissionRoute owner;
        private Texture2D texture;
        private Sprite sprite;
        private SpriteRenderer visual;
        private BoxCollider2D trigger;
        private bool armed;
        private float blinkStartedAt;

        internal void Initialize(MissionRoute route, TilemapRenderer sourceRenderer)
        {
            owner = route;
            trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one * WorldSize;

            texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false)
            {
                name = "Mission Point 8x8",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[Pixels * Pixels];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = MarkerColor;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Pixels, Pixels), Vector2.one * 0.5f,
                Pixels / WorldSize, 0, SpriteMeshType.FullRect);
            visual = GetComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sharedMaterial = sourceRenderer.sharedMaterial;
            visual.sortingLayerID = sourceRenderer.sortingLayerID;
            visual.sortingOrder = sourceRenderer.sortingOrder;
        }

        internal void Prepare(bool requireClearArea)
        {
            armed = !requireClearArea;
            blinkStartedAt = Time.time;
            visual.enabled = true;
        }

        private void FixedUpdate()
        {
            if (armed)
                return;

            // Repeated or nearby destinations require a new approach, even with several car colliders.
            trigger.Overlap(ContactFilter2D.noFilter, overlaps);
            foreach (Collider2D other in overlaps)
                if (other.GetComponentInParent<TopDownVehicle>() != null)
                    return;
            armed = true;
        }

        private void Update()
        {
            float frequency = owner.BlinkFrequency;
            // Only hide the visual: the destination remains reachable throughout the blink.
            visual.enabled = frequency <= 0f
                || Mathf.Repeat((Time.time - blinkStartedAt) * frequency, 1f) < 0.5f;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryReach(other);
        private void OnTriggerStay2D(Collider2D other) => TryReach(other);

        private void TryReach(Collider2D other)
        {
            if (armed && other.GetComponentInParent<TopDownVehicle>() != null)
                owner.ReachCurrentPoint();
        }

        private void OnDestroy()
        {
            if (sprite != null)
                Destroy(sprite);
            if (texture != null)
                Destroy(texture);
        }
    }
}
