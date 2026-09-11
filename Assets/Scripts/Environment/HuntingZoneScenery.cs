using System.Collections.Generic;
using TopDownGame.Core;
using TopDownGame.Maps;
using UnityEngine;

namespace TopDownGame.World
{
    [DisallowMultipleComponent]
    public sealed class HuntingZoneScenery : MonoBehaviour
    {
        [SerializeField, Range(8, 64)] private int driftingLeafCount = 26;
        [SerializeField, Range(4, 32)] private int fireflyCount = 14;

        private struct SceneryStyle
        {
            public string DrifterName;
            public string GlowName;
            public Color DrifterColor;
            public Color GlowColor;
            public int DrifterCount;
            public int GlowCount;
            public Vector2 DriftVelocity;
            public float GlowSpeed;
        }

        private sealed class Drifter
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Phase;
            public bool Firefly;
        }

        private readonly List<Drifter> drifters = new List<Drifter>(48);
        private Bounds area = new Bounds(Vector3.zero, new Vector3(42f, 25f, 1f));

        private void Start()
        {
            ResolveArea();
            SceneryStyle style = ResolveStyle();
            CreateDrifters(style, false);
            CreateDrifters(style, true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < drifters.Count; i++)
            {
                Drifter item = drifters[i];
                Vector3 position = item.Transform.position;
                position += (Vector3)(item.Velocity * dt);
                position.x += Mathf.Sin(Time.time * 0.8f + item.Phase) * dt * 0.08f;
                position.y += Mathf.Cos(Time.time * 1.1f + item.Phase) * dt * 0.035f;
                if (position.x > area.max.x) position.x = area.min.x;
                if (position.x < area.min.x) position.x = area.max.x;
                if (position.y > area.max.y) position.y = area.min.y;
                if (position.y < area.min.y) position.y = area.max.y;
                item.Transform.position = position;
                if (item.Firefly)
                {
                    Color color = item.Renderer.color;
                    color.a = 0.28f + (Mathf.Sin(Time.time * 2.2f +
                        item.Phase) * 0.5f + 0.5f) * 0.5f;
                    item.Renderer.color = color;
                }
                else
                {
                    item.Transform.Rotate(0f, 0f, dt * 24f);
                }
            }
        }

        private void ResolveArea()
        {
            GameObject floor = GameObject.Find("Floor");
            SpriteRenderer renderer = floor != null
                ? floor.GetComponent<SpriteRenderer>()
                : null;
            if (renderer != null && renderer.bounds.size.x > 4f)
            {
                area = renderer.bounds;
                area.Expand(new Vector3(-4f, -4f, 0f));
            }
        }

        private void CreateDrifters(SceneryStyle style, bool firefly)
        {
            int count = firefly ? style.GlowCount : style.DrifterCount;
            Transform root = new GameObject(firefly ? style.GlowName :
                style.DrifterName)
                .transform;
            root.SetParent(transform, false);
            for (int i = 0; i < count; i++)
            {
                GameObject visual = new GameObject(firefly ? "Firefly" : "Leaf");
                visual.transform.SetParent(root, false);
                visual.transform.position = new Vector3(
                    Random.Range(area.min.x, area.max.x),
                    Random.Range(area.min.y, area.max.y), 0f);
                visual.transform.localScale = firefly
                    ? Vector3.one * Random.Range(0.08f, 0.16f)
                    : new Vector3(Random.Range(0.08f, 0.16f),
                        Random.Range(0.035f, 0.07f), 1f);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = firefly
                    ? RuntimeSpriteFactory.SoftGlowSprite
                    : RuntimeSpriteFactory.CircleSprite;
                renderer.color = firefly
                    ? style.GlowColor
                    : style.DrifterColor;
                renderer.sortingOrder = firefly ? 22 : 4;
                drifters.Add(new Drifter
                {
                    Transform = visual.transform,
                    Renderer = renderer,
                    Velocity = firefly
                        ? Random.insideUnitCircle.normalized * Random.Range(
                            style.GlowSpeed * 0.55f, style.GlowSpeed)
                        : new Vector2(Random.Range(style.DriftVelocity.x * 1.25f,
                            style.DriftVelocity.x * 0.75f),
                            Random.Range(style.DriftVelocity.y * 1.3f,
                                style.DriftVelocity.y * 0.7f)),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Firefly = firefly
                });
            }
        }

        private SceneryStyle ResolveStyle()
        {
            string id = GetComponent<MapManager>()?.Data?.MapId ?? "Map01";
            switch (id)
            {
                case "Map02":
                    return CreateStyle("DeepForestLeaves", "AmberFireflies",
                        new Color(0.3f, 0.58f, 0.2f, 0.34f),
                        new Color(1f, 0.58f, 0.18f, 0.62f), 34, 12,
                        new Vector2(-0.13f, -0.07f), 0.1f);
                case "Map03":
                    return CreateStyle("ShrinePetals", "ArcaneMotes",
                        new Color(0.48f, 0.62f, 0.88f, 0.3f),
                        new Color(0.68f, 0.42f, 1f, 0.66f), 16, 22,
                        new Vector2(-0.08f, -0.045f), 0.13f);
                case "BossMap01":
                    return CreateStyle("FallingAsh", "WarningEmbers",
                        new Color(0.35f, 0.22f, 0.16f, 0.3f),
                        new Color(1f, 0.28f, 0.1f, 0.68f), 12, 24,
                        new Vector2(-0.045f, -0.085f), 0.17f);
                case "CaveMap01":
                    return CreateStyle("CrystalDust", "CyanCaveMotes",
                        new Color(0.45f, 0.62f, 0.9f, 0.32f),
                        new Color(0.3f, 0.78f, 1f, 0.7f), 10, 28,
                        new Vector2(-0.035f, -0.035f), 0.12f);
                case "RiftMap01":
                    return CreateStyle("VoidShards", "RiftMotes",
                        new Color(0.42f, 0.12f, 0.7f, 0.42f),
                        new Color(0.24f, 0.78f, 1f, 0.78f), 22, 34,
                        new Vector2(-0.08f, 0.045f), 0.2f);
                default:
                    return CreateStyle("FreshForestLeaves", "GoldenFireflies",
                        new Color(0.48f, 0.72f, 0.25f, 0.34f),
                        new Color(1f, 0.92f, 0.35f, 0.62f),
                        Mathf.Min(driftingLeafCount, 22),
                        Mathf.Min(fireflyCount, 14),
                        new Vector2(-0.13f, -0.075f), 0.1f);
            }
        }

        private static SceneryStyle CreateStyle(string drifterName,
            string glowName, Color drifterColor, Color glowColor,
            int drifterCount, int glowCount, Vector2 driftVelocity,
            float glowSpeed)
        {
            return new SceneryStyle
            {
                DrifterName = drifterName,
                GlowName = glowName,
                DrifterColor = drifterColor,
                GlowColor = glowColor,
                DrifterCount = drifterCount,
                GlowCount = glowCount,
                DriftVelocity = driftVelocity,
                GlowSpeed = glowSpeed
            };
        }
    }
}
