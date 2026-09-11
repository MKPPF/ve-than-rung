using System.Collections.Generic;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.World
{
    /// <summary>
    /// Runtime identity for Map01: animated slime nests and bubbles make each
    /// combat clearing readable while keeping every visual non-blocking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlimeMarshController : MonoBehaviour
    {
        [SerializeField] private Vector2[] nestPositions =
            new Vector2[0];
        [SerializeField, Range(2, 8)] private int bubblesPerNest = 4;
        [SerializeField] private Color slimeColor =
            new Color(0.34f, 1f, 0.12f, 0.5f);

        private sealed class NestVisual
        {
            public Transform Root;
            public SpriteRenderer Glow;
            public SpriteRenderer Mist;
            public float Phase;
            public readonly List<SpriteRenderer> Bubbles =
                new List<SpriteRenderer>(6);
        }

        private readonly List<NestVisual> nests = new List<NestVisual>(8);

        public int NestCount => nestPositions != null
            ? nestPositions.Length
            : 0;

        private void Start()
        {
            CreateNestVisuals();
        }

        private void Update()
        {
            for (int i = 0; i < nests.Count; i++)
            {
                NestVisual nest = nests[i];
                float wave = Mathf.Sin(Time.time * 1.8f + nest.Phase) *
                    0.5f + 0.5f;
                nest.Glow.transform.localScale = Vector3.one *
                    Mathf.Lerp(1.05f, 1.28f, wave);
                nest.Mist.transform.localScale = new Vector3(
                    Mathf.Lerp(1.35f, 1.7f, wave),
                    Mathf.Lerp(0.3f, 0.48f, 1f - wave), 1f);
                Color mistColor = slimeColor;
                mistColor.a = Mathf.Lerp(0.08f, 0.2f, 1f - wave);
                nest.Mist.color = mistColor;

                for (int bubbleIndex = 0;
                    bubbleIndex < nest.Bubbles.Count; bubbleIndex++)
                {
                    SpriteRenderer bubble = nest.Bubbles[bubbleIndex];
                    float bubbleWave = Mathf.Repeat(Time.time *
                        (0.42f + bubbleIndex * 0.035f) + nest.Phase +
                        bubbleIndex * 0.21f, 1f);
                    Vector3 position = bubble.transform.localPosition;
                    position.y = Mathf.Lerp(-0.42f, 0.58f, bubbleWave);
                    bubble.transform.localPosition = position;
                    bubble.transform.localScale = Vector3.one *
                        Mathf.Lerp(0.16f, 0.04f, bubbleWave);
                    Color bubbleColor = slimeColor;
                    bubbleColor.a = Mathf.Sin(bubbleWave * Mathf.PI) * 0.62f;
                    bubble.color = bubbleColor;
                }
            }
        }

        private void CreateNestVisuals()
        {
            if (nestPositions == null) return;
            for (int i = 0; i < nestPositions.Length; i++)
            {
                GameObject rootObject = new GameObject("SlimeNestVisual_" +
                    (i + 1).ToString("00"));
                rootObject.transform.SetParent(transform, false);
                rootObject.transform.position = nestPositions[i];
                NestVisual nest = new NestVisual
                {
                    Root = rootObject.transform,
                    Phase = i * 1.37f,
                    Glow = CreateVisual("Glow", rootObject.transform,
                        RuntimeSpriteFactory.SoftGlowSprite, 0.72f, -7),
                    Mist = CreateVisual("LowMist", rootObject.transform,
                        RuntimeSpriteFactory.SoftGlowSprite, 0.9f, -6)
                };

                int bubbleCount = Mathf.Max(2, bubblesPerNest);
                for (int bubbleIndex = 0;
                    bubbleIndex < bubbleCount; bubbleIndex++)
                {
                    SpriteRenderer bubble = CreateVisual("Bubble_" +
                        (bubbleIndex + 1).ToString("00"), rootObject.transform,
                        RuntimeSpriteFactory.CircleSprite, 0.12f, 3);
                    float angle = bubbleIndex * Mathf.PI * 2f / bubbleCount +
                        i * 0.63f;
                    bubble.transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * 0.72f,
                        Mathf.Sin(angle) * 0.32f, 0f);
                    nest.Bubbles.Add(bubble);
                }

                nests.Add(nest);
            }
        }

        private SpriteRenderer CreateVisual(string name, Transform parent,
            Sprite sprite, float scale, int sortingOrder)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = slimeColor;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
