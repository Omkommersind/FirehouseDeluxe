using System;
using UnityEngine;

namespace FirehouseDeluxe.Rescue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class RescuePerson : MonoBehaviour
    {
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite stressSprite;
        [SerializeField, Min(1), Tooltip("Size in pixels of the head square at the top of the idle sprite.")]
        private int headSizePixels = 64;
        [SerializeField, Min(0.1f)] private float descentSpeed = 8f;
        [SerializeField, Min(0.1f)] private float fallAcceleration = 35f;

        private enum State { Waiting, Descending, Falling, Finished }
        private State state;
        private Rigidbody2D body;
        private BoxCollider2D hitbox;
        private SpriteRenderer spriteRenderer;
        private Sprite waitingHeadSprite;
        private RescueLadder ladder;
        private Vector2 fallVelocity;
        private float despawnY;

        public event Action<RescuePerson, bool> Finished;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = idleSprite;
            ShowWaitingHead();
        }

        private void ShowWaitingHead()
        {
            if (idleSprite == null)
                return;

            Rect source = idleSprite.rect;
            float size = Mathf.Min(headSizePixels, source.width, source.height);
            var headRect = new Rect(source.x + Mathf.Floor((source.width - size) * 0.5f),
                source.yMax - size, size, size);
            // Reuse the original texture and place the bottom of the head on the sill.
            waitingHeadSprite = Sprite.Create(idleSprite.texture, headRect,
                new Vector2(0.5f, 0f), idleSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            waitingHeadSprite.name = idleSprite.name + " Head";
            spriteRenderer.sprite = waitingHeadSprite;
            hitbox.size = Vector2.one * (size / idleSprite.pixelsPerUnit);
            hitbox.offset = Vector2.up * (hitbox.size.y * 0.5f);
        }

        public void Initialize(float fallDespawnY) => despawnY = fallDespawnY;

        public void TryBeginDescent(RescueLadder rescueLadder)
        {
            if (state != State.Waiting || rescueLadder == null || !rescueLadder.CanCatch)
                return;

            ladder = rescueLadder;
            state = State.Descending;
            spriteRenderer.sprite = idleSprite;
            // The passenger remains independent of the vehicle so falling never follows the truck.
            hitbox.enabled = false;
            body.position = ladder.TopPosition;
        }

        private void FixedUpdate()
        {
            if (state == State.Descending)
            {
                if (ladder == null || !ladder.CanCatch)
                {
                    BeginFall();
                    return;
                }

                Vector2 destination = ladder.EndPosition;
                Vector2 next = Vector2.MoveTowards(body.position, destination, descentSpeed * Time.fixedDeltaTime);
                body.MovePosition(next);
                if (next == destination)
                    Finish(true);
            }
            else if (state == State.Falling)
            {
                fallVelocity.y -= fallAcceleration * Time.fixedDeltaTime;
                Vector2 next = body.position + fallVelocity * Time.fixedDeltaTime;
                body.MovePosition(next);
                if (next.y <= despawnY)
                    Finish(false);
            }
        }

        private void BeginFall()
        {
            state = State.Falling;
            spriteRenderer.sprite = stressSprite;
            fallVelocity = new Vector2(ladder != null ? ladder.HorizontalSpeed : 0f, 0f);
            ladder = null;
        }

        private void Finish(bool rescued)
        {
            state = State.Finished;
            gameObject.SetActive(false);
            Finished?.Invoke(this, rescued);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (waitingHeadSprite != null)
                Destroy(waitingHeadSprite);
        }
    }
}
