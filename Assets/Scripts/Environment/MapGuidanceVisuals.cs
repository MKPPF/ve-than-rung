using System.Collections.Generic;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.World
{
    /// <summary>
    /// Adds readable, non-colliding landmarks to generated maps so players can
    /// understand the entrance, rewards and exit without changing maze topology.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapGuidanceVisuals : MonoBehaviour
    {
        [SerializeField] private string mapId = "Map01";
        [SerializeField] private Transform entranceAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private Transform rewardAnchor;

        private sealed class PulseVisual
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 BaseScale;
            public float ScaleAmplitude;
            public float BaseAlpha;
            public float Speed;
            public float Phase;
            public float SpinSpeed;
        }

        private readonly List<PulseVisual> visuals = new List<PulseVisual>(12);

        public string MapId => mapId;
        public bool HasValidAnchors => entranceAnchor != null &&
            exitAnchor != null && rewardAnchor != null;
        public int VisualCount => visuals.Count;

        private void Start()
        {
            if (!HasValidAnchors)
            {
                enabled = false;
                return;
            }

            ResolvePalette(out Color entranceColor, out Color exitColor,
                out Color rewardColor);
            CreateGroundSignal(entranceAnchor, "Entrance", entranceColor,
                1.7f, 0f);
            CreateGroundSignal(rewardAnchor, "Reward", rewardColor,
                1.35f, 1.8f);
            // LobbyPortal owns the planar dais. Combat exit beacons would add
            // another bright column and foreground ring over the player here.
            if (mapId != "Lobby") CreateExitSignal(exitAnchor, exitColor);
        }

        private void Update()
        {
            float time = Time.time;
            for (int i = 0; i < visuals.Count; i++)
            {
                PulseVisual visual = visuals[i];
                float wave = Mathf.Sin(time * visual.Speed + visual.Phase);
                visual.Transform.localScale = visual.BaseScale *
                    (1f + wave * visual.ScaleAmplitude);
                if (Mathf.Abs(visual.SpinSpeed) > 0.01f)
                {
                    visual.Transform.Rotate(0f, 0f,
                        visual.SpinSpeed * Time.deltaTime);
                }

                Color color = visual.Renderer.color;
                color.a = visual.BaseAlpha * (0.72f + (wave + 1f) * 0.14f);
                visual.Renderer.color = color;
            }
        }

        private void CreateGroundSignal(Transform anchor, string prefix,
            Color color, float diameter, float phase)
        {
            CreateLayer(anchor, prefix + "Glow", RuntimeSpriteFactory.SoftGlowSprite,
                color, new Vector3(diameter, diameter * 0.62f, 1f), -2,
                0.09f, 1.35f, phase, 0f);
            CreateLayer(anchor, prefix + "Ring", RuntimeSpriteFactory.RingSprite,
                color, new Vector3(diameter * 0.72f, diameter * 0.44f, 1f), -1,
                0.14f, 1.7f, phase + 0.8f, 12f);
        }

        private void CreateExitSignal(Transform anchor, Color color)
        {
            CreateGroundSignal(anchor, "Exit", color, 2.25f, 0.7f);
            CreateLayer(anchor, "ExitOuterRing", RuntimeSpriteFactory.RingSprite,
                color, new Vector3(2.45f, 1.45f, 1f), 6,
                0.08f, 1.1f, 2.2f, -18f);

            PulseVisual beam = CreateLayer(anchor, "ExitBeacon",
                RuntimeSpriteFactory.WhiteSprite, color,
                new Vector3(0.075f, 3.4f, 1f), 5,
                0.08f, 1.4f, 1.1f, 0f);
            beam.Transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Color beamColor = beam.Renderer.color;
            beamColor.a = Mathf.Min(beamColor.a, 0.24f);
            beam.Renderer.color = beamColor;
            beam.BaseAlpha = beamColor.a;
        }

        private PulseVisual CreateLayer(Transform parent, string name,
            Sprite sprite, Color color, Vector3 scale, int sortingOrder,
            float amplitude, float speed, float phase, float spinSpeed)
        {
            GameObject layer = new GameObject("GuidanceVisual_" + name);
            layer.transform.SetParent(parent, false);
            layer.transform.localScale = scale;
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            PulseVisual visual = new PulseVisual
            {
                Transform = layer.transform,
                Renderer = renderer,
                BaseScale = scale,
                ScaleAmplitude = amplitude,
                BaseAlpha = color.a,
                Speed = speed,
                Phase = phase,
                SpinSpeed = spinSpeed
            };
            visuals.Add(visual);
            return visual;
        }

        private void ResolvePalette(out Color entrance, out Color exit,
            out Color reward)
        {
            switch (mapId)
            {
                case "Lobby":
                    entrance = new Color(0.45f, 0.9f, 1f, 0.34f);
                    exit = new Color(0.25f, 1f, 0.58f, 0.58f);
                    reward = new Color(1f, 0.72f, 0.22f, 0.48f);
                    break;
                case "Map02":
                    entrance = new Color(0.5f, 0.95f, 0.45f, 0.34f);
                    exit = new Color(1f, 0.58f, 0.18f, 0.58f);
                    reward = new Color(1f, 0.82f, 0.28f, 0.5f);
                    break;
                case "Map03":
                    entrance = new Color(0.45f, 0.82f, 1f, 0.36f);
                    exit = new Color(0.72f, 0.45f, 1f, 0.62f);
                    reward = new Color(0.35f, 1f, 0.9f, 0.5f);
                    break;
                case "BossMap01":
                    entrance = new Color(1f, 0.62f, 0.22f, 0.38f);
                    exit = new Color(1f, 0.22f, 0.12f, 0.64f);
                    reward = new Color(1f, 0.8f, 0.2f, 0.56f);
                    break;
                case "CaveMap01":
                    entrance = new Color(0.35f, 0.85f, 1f, 0.4f);
                    exit = new Color(0.28f, 0.65f, 1f, 0.64f);
                    reward = new Color(0.75f, 0.42f, 1f, 0.56f);
                    break;
                default:
                    entrance = new Color(0.5f, 1f, 0.62f, 0.34f);
                    exit = new Color(1f, 0.78f, 0.25f, 0.58f);
                    reward = new Color(0.35f, 0.9f, 1f, 0.5f);
                    break;
            }
        }
    }
}
