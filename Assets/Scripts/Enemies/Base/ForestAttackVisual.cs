using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Readable locked-sector and falling-arrow telegraphs. Visual time is scaled
    /// gameplay time; geometry is bounded and owns no damage or persistent state.
    /// </summary>
    public sealed class ForestAttackVisual : MonoBehaviour
    {
        private LineRenderer outline;
        private Material material;
        private Transform[] arrows;
        private Vector2[] landings;
        private float duration, elapsed;
        private bool rain;
        public float Progress => Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));

        public static ForestAttackVisual ShowSector(Vector2 center, Vector2 facing,
            float radius, float windup)
        {
            var effect = Create("Forest Slash Sector", center, radius, windup, false);
            effect.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            return effect;
        }

        public static ForestAttackVisual ShowRain(Vector2 center, float radius, float windup)
            => Create("Forest Arrow Rain", center, radius, windup, true);

        private static ForestAttackVisual Create(string name, Vector2 center,
            float radius, float windup, bool rain)
        {
            var obj = new GameObject(name);
            obj.transform.position = center;
            var effect = obj.AddComponent<ForestAttackVisual>();
            effect.duration = Mathf.Max(0.1f, windup);
            effect.rain = rain;
            effect.material = new Material(Shader.Find("Sprites/Default"));
            effect.outline = obj.AddComponent<LineRenderer>();
            var line = effect.outline;
            line.sharedMaterial = effect.material;
            line.useWorldSpace = false;
            // Ground-only ink: never draw warnings over actors or join the
            // sector to its centre with the ugly crossing triangle spokes.
            line.loop = rain;
            line.widthMultiplier = rain ? 0.035f : 0.025f;
            line.sortingOrder = -5;
            line.numCornerVertices = 2;
            int count = rain ? 48 : 26;
            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float angle = rain ? i * 360f / count : Mathf.Lerp(-65, 65, i / (count - 1f));
                float radians = angle * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * radius);
            }
            effect.UpdateColors(0);
            if (rain) effect.CreateArrows(radius);
            return effect;
        }

        private void CreateArrows(float radius)
        {
            arrows = new Transform[9];
            landings = new Vector2[9];
            for (int i = 0; i < arrows.Length; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float spread = Mathf.Sqrt(i / 9f) * radius * 0.85f;
                landings[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
                var arrow = new GameObject("FallingArrow");
                arrow.transform.SetParent(transform, false);
                arrows[i] = arrow.transform;
                Segment(arrow.transform, new Vector2(0, 0.22f), new Vector2(0.045f, 0.5f), 0, new Color(0.33f, 0.19f, 0.09f));
                Segment(arrow.transform, new Vector2(-0.06f, 0.43f), new Vector2(0.13f, 0.08f), -35, new Color(0.7f, 0.84f, 0.44f));
                Segment(arrow.transform, new Vector2(0.06f, 0.43f), new Vector2(0.13f, 0.08f), 35, new Color(0.7f, 0.84f, 0.44f));
                Segment(arrow.transform, new Vector2(-0.04f, 0.01f), new Vector2(0.035f, 0.16f), -32, new Color(1, 0.87f, 0.5f));
                Segment(arrow.transform, new Vector2(0.04f, 0.01f), new Vector2(0.035f, 0.16f), 32, new Color(1, 0.87f, 0.5f));
                arrow.SetActive(false);
            }
        }

        private static void Segment(Transform parent, Vector2 center, Vector2 size, float angle, Color color)
        {
            var obj = new GameObject("ArrowDetail", typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = center;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            obj.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = 13;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            UpdateColors(progress);
            if (rain)
            {
                float fall = Mathf.Clamp01((progress - 0.64f) / 0.36f);
                for (int i = 0; i < arrows.Length; i++)
                {
                    arrows[i].gameObject.SetActive(progress > 0.64f);
                    arrows[i].localPosition = landings[i] + new Vector2((1 - fall) * 0.5f, (1 - fall) * 3f);
                }
            }
            if (elapsed >= duration + (rain ? 0.24f : 0.03f)) Destroy(gameObject);
        }

        private void UpdateColors(float progress)
        {
            Color color = rain ? new Color(1f, 0.68f, 0.26f) : new Color(1f, 0.48f, 0.2f);
            float fade = elapsed > duration ? 1-Mathf.Clamp01((elapsed-duration)/0.24f) : 1;
            color.a = Mathf.Lerp(0.38f, 0.72f, progress) * fade;
            outline.startColor = outline.endColor = color;
        }

        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
