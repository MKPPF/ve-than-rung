using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Small persistent ambient sprites bring the painted lobby to life without adding colliders or per-frame allocations.</summary>
    public sealed class LobbyAtmosphere : MonoBehaviour
    {
        [SerializeField] private Vector2[] warmLightPositions;
        [SerializeField] private Vector3 portalPosition;
        [SerializeField] private Vector3 chimneyPosition;
        private SpriteRenderer[] lights;
        private SpriteRenderer[] motes;
        private SpriteRenderer[] smoke;
        private SpriteRenderer portalGlow;
        private float elapsed;

        private void Start()
        {
            lights = new SpriteRenderer[warmLightPositions != null ? warmLightPositions.Length : 0];
            for (int i = 0; i < lights.Length; i++)
                lights[i] = Create("LanternGlow", warmLightPositions[i], new Vector2(1.4f, 1.05f),
                    new Color(1f, 0.62f, 0.23f, 0.13f), -15);
            portalGlow = Create("PortalBreath", portalPosition, new Vector2(3.9f, 2.88f),
                new Color(0.25f, 0.85f, 0.72f, 0.045f), -14);
            motes = new SpriteRenderer[18];
            for (int i = 0; i < motes.Length; i++)
                motes[i] = Create("Firefly_" + i, Vector3.zero, Vector2.one * 0.08f,
                    new Color(1f, 0.88f, 0.5f, 0.4f), 15);
            smoke = new SpriteRenderer[5];
            for (int i = 0; i < smoke.Length; i++)
                smoke[i] = Create("ChimneySmoke_" + i, chimneyPosition, Vector2.one * 0.7f,
                    new Color(0.62f, 0.68f, 0.72f, 0.12f), -12);
        }

        private SpriteRenderer Create(string name, Vector3 position, Vector2 scale, Color color, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Update()
        {
            if (lights == null) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < lights.Length; i++)
            {
                Color color = lights[i].color;
                color.a = 0.11f + Mathf.Sin(elapsed * 2.4f + i * 1.7f) * 0.025f;
                lights[i].color = color;
            }
            Color portalColor = portalGlow.color;
            portalColor.a = 0.045f + Mathf.Sin(elapsed * 1.3f) * 0.01f;
            portalGlow.color = portalColor;
            for (int i = 0; i < motes.Length; i++)
            {
                float phase = elapsed * 0.24f + i * 2.399f;
                motes[i].transform.localPosition = new Vector3(
                    -8f + (i * 3.17f % 18f) + Mathf.Sin(phase) * 0.3f,
                    -2.7f + (i * 1.93f % 5.4f) + Mathf.Cos(phase * 0.8f) * 0.22f);
                Color color = motes[i].color;
                color.a = 0.08f + Mathf.Pow(Mathf.Max(0, Mathf.Sin(phase)), 3) * 0.45f;
                motes[i].color = color;
            }
            for (int i = 0; i < smoke.Length; i++)
            {
                float t = Mathf.Repeat(elapsed * 0.16f + i / 5f, 1);
                smoke[i].transform.position = chimneyPosition + new Vector3(t * 0.7f, t * 1.2f);
                smoke[i].transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, t);
                Color color = smoke[i].color;
                color.a = Mathf.Sin(t * Mathf.PI) * 0.14f;
                smoke[i].color = color;
            }
        }
    }
}
