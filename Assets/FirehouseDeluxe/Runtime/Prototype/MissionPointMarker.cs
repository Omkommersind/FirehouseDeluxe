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

        private readonly Color32[] pixels = new Color32[Pixels * Pixels];
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private MissionRoute owner;
        private Texture2D texture;
        private Sprite sprite;
        private BoxCollider2D trigger;
        private bool armed;
        private float nextFrameTime;

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
            sprite = Sprite.Create(texture, new Rect(0, 0, Pixels, Pixels), Vector2.one * 0.5f,
                Pixels / WorldSize, 0, SpriteMeshType.FullRect);
            var visual = GetComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sharedMaterial = sourceRenderer.sharedMaterial;
            visual.sortingLayerID = sourceRenderer.sortingLayerID;
            visual.sortingOrder = sourceRenderer.sortingOrder;
            DrawFrame();
        }

        internal void Prepare(bool requireClearArea)
        {
            armed = !requireClearArea;
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
            if (Time.time < nextFrameTime)
                return;
            nextFrameTime = Time.time + 1f / 15f;
            DrawFrame();
        }

        private void DrawFrame()
        {
            float phase = Time.time * owner.RainbowSpeed;
            float alpha = owner.Opacity * (1f - owner.PulseAmount
                * (0.5f + 0.5f * Mathf.Sin(Time.time * owner.PulseSpeed * Mathf.PI * 2f)));
            for (int y = 0; y < Pixels; y++)
            {
                for (int x = 0; x < Pixels; x++)
                {
                    bool edge = x == 0 || y == 0 || x == Pixels - 1 || y == Pixels - 1;
                    Color color = Color.HSVToRGB(Mathf.Repeat(phase + (x + y) / 14f, 1f), 0.8f, 1f);
                    color.a = alpha * (edge ? 1f : 0.25f);
                    pixels[y * Pixels + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
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
