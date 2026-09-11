using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>
    /// Presents a committed departure, never loads a scene or writes a save. The portal owns
    /// time and cancellation; CameraFollow2D retains sole ownership of the camera transform.
    /// </summary>
    [RequireComponent(typeof(LobbyPortal))]
    public sealed class TeleportDepartureVisual : MonoBehaviour
    {
        [Header("Departure presentation")]
        [SerializeField] private Shader beamShader;
        [SerializeField, Range(1f, 1.4f)] private float zoomOutMultiplier = 1.18f;
        private LobbyPortal portal;
        private GameObject root;
        private Material material;
        private SpriteRenderer[] sparks;
        private CameraFollow2D cameraFollow;
        private bool presenting;
        public bool IsVisible => root != null && root.activeSelf;
        public bool HasBeamMaterial => material != null;
        public int VisualCount => root != null ? root.transform.childCount : 0;

        private void Start()
        {
            portal = GetComponent<LobbyPortal>();
            root = new GameObject("DepartureLight");
            root.transform.SetParent(transform, false);
            if (beamShader == null) beamShader = Shader.Find("TopDownGame/TeleportDeparture");
            var beam = new GameObject("HeavenwardRays", typeof(SpriteRenderer));
            beam.transform.SetParent(root.transform, false);
            beam.transform.localPosition = new Vector3(0, 2.1f, 0);
            beam.transform.localScale = new Vector3(5.4f, 7.4f, 1);
            var renderer = beam.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 16;
            if (beamShader != null && beamShader.isSupported)
            {
                material = new Material(beamShader) { name = "TeleportDeparture_Owned" };
                renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
                renderer.sharedMaterial = material;
            }
            else
            {
                // A stripped/unsupported shader must never strand the player.
                renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
                renderer.color = new Color(0.4f, 1f, 0.85f, 0.22f);
            }
            sparks = new SpriteRenderer[36];
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = new GameObject("AscendingSpark_" + i, typeof(SpriteRenderer));
                spark.transform.SetParent(root.transform, false);
                sparks[i] = spark.GetComponent<SpriteRenderer>();
                sparks[i].sprite = RuntimeSpriteFactory.SoftGlowSprite;
                sparks[i].sortingOrder = i % 2 == 0 ? 17 : -8;
            }
            root.SetActive(false);
        }

        private void LateUpdate()
        {
            if (portal == null || root == null) return;
            if (!portal.IsTransporting)
            {
                if (presenting) ResetPresentation();
                return;
            }
            if (!presenting)
            {
                presenting = true;
                root.SetActive(true);
                Camera camera = Camera.main;
                cameraFollow = camera != null ? camera.GetComponent<CameraFollow2D>() : null;
                cameraFollow?.BeginCinematicFocus(this, transform.position + Vector3.up * 1.1f, zoomOutMultiplier);
            }
            float t = portal.DepartureProgress;
            float clock = t * portal.DepartureDuration;
            cameraFollow?.SetCinematicProgress(this, t / 0.42f);
            if (material != null)
            {
                material.SetFloat("_Progress", t);
                material.SetFloat("_Clock", clock);
            }
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.12f) / 0.35f));
            for (int i = 0; i < sparks.Length; i++)
            {
                float phase = Mathf.Repeat(clock * (0.36f + i % 3 * 0.07f) + i * 0.618f, 1f);
                // Each particle ascends on its own vertical track; no inward
                // spiral that would visually taper the otherwise straight beam.
                float angle = i * 2.399f;
                const float radius = 1.75f;
                sparks[i].transform.localPosition = new Vector3(Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 0.65f + phase * 4.4f, 0);
                sparks[i].transform.localScale = new Vector3(0.045f, 0.08f + t * 0.1f, 1);
                sparks[i].color = new Color(0.75f, 1f, 0.85f,
                    Mathf.Sin(phase * Mathf.PI) * strength * 0.85f);
            }
        }

        public void ResetPresentation()
        {
            cameraFollow?.EndCinematicFocus(this);
            cameraFollow = null;
            presenting = false;
            if (root != null) root.SetActive(false);
        }

        private void OnDisable() => ResetPresentation();
        private void OnDestroy()
        {
            ResetPresentation();
            if (root != null) Destroy(root);
            if (material != null) Destroy(material);
        }
    }
}
