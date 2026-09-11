using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Fixed-count ripples and waterfall spray sit only on the painting's water; pause freezes their clock.</summary>
    public sealed class ForestWaterAmbience : MonoBehaviour
    {
        [SerializeField] private Vector2 pondCenter;
        [SerializeField] private Vector2 pondRadius = new Vector2(1.8f, 1.1f);
        [SerializeField] private Vector2 waterfallBottom;
        private SpriteRenderer[] ripples, spray;
        private float elapsed;
        public float AnimationTime => elapsed;
        private void Start()
        {
            ripples = new SpriteRenderer[6];
            spray = new SpriteRenderer[12];
            for (int i = 0; i < ripples.Length; i++) ripples[i] = Create("PondRipple", RuntimeSpriteFactory.RingSprite);
            for (int i = 0; i < spray.Length; i++) spray[i] = Create("WaterfallSpray", RuntimeSpriteFactory.SoftGlowSprite);
        }
        private SpriteRenderer Create(string name, Sprite sprite)
        {
            var visual = new GameObject(name, typeof(SpriteRenderer));
            visual.transform.SetParent(transform, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -20;
            return renderer;
        }
        private void Update()
        {
            if (ripples == null) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < ripples.Length; i++)
            {
                float t = Mathf.Repeat(elapsed * 0.36f + i * 0.163f, 1f);
                Vector2 offset = new Vector2(Mathf.Sin(i * 2.4f) * pondRadius.x * 0.55f,
                    Mathf.Cos(i * 2.4f) * pondRadius.y * 0.55f);
                ripples[i].transform.position = pondCenter + offset;
                ripples[i].transform.localScale = new Vector3(0.1f + t * 0.75f, 0.08f + t * 0.3f, 1);
                ripples[i].color = new Color(0.66f, 1f, 0.95f, Mathf.Sin(t * Mathf.PI) * 0.19f);
            }
            for (int i = 0; i < spray.Length; i++)
            {
                float t = Mathf.Repeat(elapsed * 0.85f + i * 0.083f, 1);
                spray[i].transform.position = waterfallBottom + new Vector2(Mathf.Sin(i * 4.3f) * t * 0.6f, (1 - t) * 0.8f - t * 0.18f);
                spray[i].transform.localScale = new Vector3(0.025f, 0.08f, 1);
                spray[i].color = new Color(0.8f, 1f, 1f, Mathf.Sin(t * Mathf.PI) * 0.48f);
            }
        }
        private void OnDestroy()
        {
            if (ripples != null) foreach (var r in ripples) if (r != null) Destroy(r.gameObject);
            if (spray != null) foreach (var r in spray) if (r != null) Destroy(r.gameObject);
        }
    }
}
