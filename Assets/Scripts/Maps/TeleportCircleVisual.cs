using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>
    /// Fits light to painted stone engravings. Fixed ellipses preserve perspective while
    /// travelling highlights give motion without spinning or obscuring the ground plane.
    /// </summary>
    [RequireComponent(typeof(LobbyPortal))]
    public sealed class TeleportCircleVisual : MonoBehaviour
    {
        [Header("Painted dais alignment")]
        [SerializeField] private Vector2 engravedRadii = new Vector2(1.22f, 0.87f);
        [SerializeField] private Vector2 outerRadii = new Vector2(1.95f, 1.44f);
        private const int Segments = 128;
        private LobbyPortal portal;
        private GameObject root;
        private LineRenderer[] rings, highlights;
        private SpriteRenderer glow, innerLight;
        private SpriteRenderer[] motes;
        private float elapsed;
        private Material lineMaterial;
        public int VisualCount => root != null ? root.transform.childCount : 0;
        public float AnimationTime => elapsed;
        public Vector2 EngravedRadii => engravedRadii;
        public Vector2 OuterRadii => outerRadii;

        private void Start()
        {
            portal = GetComponent<LobbyPortal>();
            root = new GameObject("TeleportRunes");
            root.transform.SetParent(transform, false);
            glow = Sprite("GroundGlow", new Vector3(outerRadii.x * 2, outerRadii.y * 2, 1), -12);
            innerLight = Sprite("InnerGroundLight", new Vector3(engravedRadii.x * 2, engravedRadii.y * 2, 1), -12);
            lineMaterial = new Material(glow.sharedMaterial) { name = "TeleportRunes_Runtime" };
            rings = new LineRenderer[3];
            highlights = new LineRenderer[4];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = Line("EngravedRing_" + i, true);
                DrawEllipse(rings[i], i == 2 ? outerRadii : engravedRadii * (i == 0 ? 1f : 0.87f),
                    0, Mathf.PI * 2);
            }
            for (int i = 0; i < highlights.Length; i++)
            {
                highlights[i] = Line("TravellingLight_" + i, false);
                highlights[i].widthCurve = new AnimationCurve(
                    new Keyframe(0, 0), new Keyframe(0.35f, 0.65f),
                    new Keyframe(0.7f, 1), new Keyframe(1, 0));
            }
            motes = new SpriteRenderer[16];
            for (int i = 0; i < motes.Length; i++)
                motes[i] = Sprite("RuneSpark_" + i, Vector3.one * 0.045f, -10);
            RenderLight();
        }

        private LineRenderer Line(string name, bool loop)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(root.transform, false);
            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = Segments;
            line.sharedMaterial = lineMaterial;
            line.numCapVertices = 3;
            line.sortingOrder = -11;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void DrawEllipse(LineRenderer line, Vector2 radii, float start, float span)
        {
            for (int j = 0; j < Segments; j++)
            {
                float angle = start + span * j / (line.loop ? Segments : Segments - 1f);
                line.SetPosition(j, new Vector3(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y, 0));
            }
        }

        private SpriteRenderer Sprite(string name, Vector3 size, int order)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(root.transform, false);
            obj.transform.localScale = size;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Update()
        {
            if (portal == null || root == null) return;
            elapsed += Time.deltaTime;
            RenderLight();
        }

        private void RenderLight()
        {
            float charge = Mathf.SmoothStep(0, 1, portal.ChargeProgress);
            float departure = portal.IsTransporting ? Mathf.SmoothStep(0f, 1f, portal.DepartureProgress / 0.55f) : 0f;
            float pulse = 0.92f + Mathf.Sin(elapsed * 1.6f) * 0.08f;
            glow.color = new Color(0.24f, 0.85f, 0.73f, (0.035f + charge * 0.10f + departure * 0.20f) * pulse);
            innerLight.color = new Color(0.65f, 1f, 0.83f, charge * 0.075f + departure * 0.18f);
            for (int i = 0; i < rings.Length; i++)
            {
                // The rim stays understated so the original carved runes remain legible.
                float alpha = (0.18f + charge * 0.34f + departure * 0.4f) * pulse;
                Color color = new Color(0.48f, 0.94f, 0.80f, alpha);
                rings[i].startColor = rings[i].endColor = color;
                rings[i].widthMultiplier = 0.014f + charge * 0.006f + departure * 0.013f;
            }
            for (int i = 0; i < highlights.Length; i++)
            {
                DrawEllipse(highlights[i], i < 2 ? engravedRadii : outerRadii,
                    elapsed * (i < 2 ? 0.22f : -0.17f) + (i % 2) * Mathf.PI, Mathf.PI * 0.38f);
                Color color = new Color(0.80f, 1f, 0.88f, Mathf.Min(1f, (0.23f + charge * 0.44f + departure * 0.3f) * pulse));
                highlights[i].startColor = highlights[i].endColor = color;
                highlights[i].widthMultiplier = 0.022f + charge * 0.009f;
            }
            for (int i = 0; i < motes.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / motes.Length;
                float twinkle = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(elapsed * 1.8f + i * 2.399f), 3);
                Vector2 radii = i % 2 == 0 ? engravedRadii : outerRadii;
                motes[i].transform.localPosition = new Vector3(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y, 0);
                motes[i].color = new Color(0.83f, 1f, 0.78f, (0.12f + charge * 0.4f) * twinkle);
                motes[i].transform.localScale = Vector3.one * (0.045f + charge * 0.025f);
            }
        }

        private void OnEnable() { if (root != null) root.SetActive(true); }
        private void OnDisable() { if (root != null) root.SetActive(false); }
        private void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
