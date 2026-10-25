using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Maps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Builds a non-interactive tactical overview from the live scene and adds
    /// high-contrast UI markers without changing world colliders or navigation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class MiniMapController : MonoBehaviour
    {
        private const int TextureWidth = 320;
        private const int TextureHeight = 200;

        [Header("Layout")]
        [SerializeField] private Vector2 panelSize = new Vector2(310f, 210f);
        [SerializeField] private Vector2 panelMargin = new Vector2(18f, 18f);
        [SerializeField, Min(0.1f)] private float markerRefreshInterval = 0.45f;

        private sealed class Marker
        {
            public Transform Target;
            public RectTransform Rect;
            public EnemyHealth Enemy;
        }

        private readonly List<Marker> markers = new List<Marker>(40);
        private RectTransform panel;
        private RectTransform markerRoot;
        private RawImage mapImage;
        private Camera mapCamera;
        private RenderTexture mapTexture;
        private Bounds worldBounds;
        private float nextMarkerRefreshTime;

        public bool IsReady => panel != null && markerRoot != null &&
            mapCamera != null && mapTexture != null;
        public int MarkerCount => markers.Count;
        public Bounds WorldBounds => worldBounds;
        public Camera MapCamera => mapCamera;
        public RectTransform Panel => panel;

        private void Start()
        {
            BuildInterface();
            ResolveWorldBounds();
            CreateMapCamera();
            RefreshMarkers();
        }

        private void LateUpdate()
        {
            if (!IsReady)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            panel.localScale = Vector3.one * Mathf.Max(1f, 0.7f / Mathf.Max(0.01f, canvas.scaleFactor));

            if (Time.unscaledTime >= nextMarkerRefreshTime)
            {
                RefreshMarkers();
            }

            for (int i = 0; i < markers.Count; i++)
            {
                Marker marker = markers[i];
                bool visible = marker.Target != null &&
                    (marker.Enemy == null || marker.Enemy.IsAlive);
                marker.Rect.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                float normalizedX = Mathf.InverseLerp(worldBounds.min.x,
                    worldBounds.max.x, marker.Target.position.x);
                float normalizedY = Mathf.InverseLerp(worldBounds.min.y,
                    worldBounds.max.y, marker.Target.position.y);
                Vector2 anchor = new Vector2(Mathf.Clamp01(normalizedX),
                    Mathf.Clamp01(normalizedY));
                marker.Rect.anchorMin = anchor;
                marker.Rect.anchorMax = anchor;
                marker.Rect.anchoredPosition = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            if (mapCamera != null)
            {
                Destroy(mapCamera.gameObject);
            }

            if (mapTexture != null)
            {
                mapTexture.Release();
                Destroy(mapTexture);
            }
        }

        private void BuildInterface()
        {
            Transform oldPanel = transform.Find("MiniMapPanel");
            if (oldPanel != null)
            {
                Destroy(oldPanel.gameObject);
            }

            GameObject panelObject = new GameObject("MiniMapPanel",
                typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.sizeDelta = new Vector2(panelSize.x, Mathf.Max(310f, panelSize.y));
            panel.anchoredPosition = new Vector2(-panelMargin.x, panelMargin.y);
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.055f, 0.075f, 0.9f);
            panelImage.raycastTarget = false;

            GameObject titleObject = new GameObject("Title",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panel, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(8f, -62f);
            titleRect.offsetMax = new Vector2(-8f, -4f);
            TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
            title.text = "BẢN ĐỒ\n" + ResolveMapName();
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.75f, 0.95f, 1f);
            title.raycastTarget = false;

            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(RawImage), typeof(RectMask2D));
            viewport.transform.SetParent(panel, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 64f);
            viewportRect.offsetMax = new Vector2(-8f, -66f);
            mapImage = viewport.GetComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;

            GameObject markerObject = new GameObject("Markers",
                typeof(RectTransform));
            markerObject.transform.SetParent(viewportRect, false);
            markerRoot = markerObject.GetComponent<RectTransform>();
            markerRoot.anchorMin = Vector2.zero;
            markerRoot.anchorMax = Vector2.one;
            markerRoot.offsetMin = Vector2.zero;
            markerRoot.offsetMax = Vector2.zero;

            float legendY = panel.sizeDelta.y - 62;
            Legend("Bạn", 12, legendY, new Color(0.25f, 0.95f, 1f), RuntimeSpriteFactory.RingSprite);
            Legend("Lối đi", 158, legendY, new Color(0.3f, 1f, 0.45f), RuntimeSpriteFactory.RingSprite);
            Legend("Rương", 12, legendY + 28, new Color(1f, 0.8f, 0.2f), RuntimeSpriteFactory.WhiteSprite);
            Legend("Địch", 158, legendY + 28, new Color(1f, 0.22f, 0.16f), RuntimeSpriteFactory.CircleSprite);
        }

        private void Legend(string label, float x, float y, Color color, Sprite sprite)
        {
            Image icon = FantasyEquipmentUI.Icon(panel, "LegendIcon_" + label, sprite, x, y + 8, 12, 12);
            icon.color = color;
            TMP_Text text = FantasyEquipmentUI.Text(panel, "Legend_" + label, label, x + 20, y, 118, 28, 21);
            text.enableAutoSizing = false;
        }

        private string ResolveMapName()
        {
            MapManager manager = FindAnyObjectByType<MapManager>();
            if (manager != null && manager.Data != null &&
                !string.IsNullOrWhiteSpace(manager.Data.DisplayName))
            {
                return manager.Data.DisplayName.ToUpperInvariant();
            }

            string scene = SceneManager.GetActiveScene().name;
            return scene == "Lobby" ? "TRẠM NGHỈ TÁN RỪNG" : scene.ToUpperInvariant();
        }

        private void ResolveWorldBounds()
        {
            Transform walls = GameObject.Find("Environment")?.transform
                .Find("Walls");
            Collider2D[] wallColliders = walls != null
                ? walls.GetComponentsInChildren<Collider2D>(true)
                : System.Array.Empty<Collider2D>();
            if (wallColliders.Length > 0)
            {
                worldBounds = wallColliders[0].bounds;
                for (int i = 1; i < wallColliders.Length; i++)
                {
                    worldBounds.Encapsulate(wallColliders[i].bounds);
                }
                return;
            }

            SpriteRenderer floor = GameObject.Find("Floor")?
                .GetComponent<SpriteRenderer>();
            worldBounds = floor != null
                ? floor.bounds
                : new Bounds(Vector3.zero, new Vector3(40f, 24f, 1f));
        }

        private void CreateMapCamera()
        {
            GameObject cameraObject = new GameObject("MiniMapCamera");
            mapCamera = cameraObject.AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.transform.position = new Vector3(worldBounds.center.x,
                worldBounds.center.y, -10f);
            float aspect = TextureWidth / (float)TextureHeight;
            mapCamera.orthographicSize = Mathf.Max(worldBounds.extents.y,
                worldBounds.extents.x / aspect) + 0.4f;
            Camera main = Camera.main;
            mapCamera.backgroundColor = main != null
                ? main.backgroundColor
                : new Color(0.08f, 0.14f, 0.12f);
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            mapCamera.depth = -20f;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;

            mapTexture = new RenderTexture(TextureWidth, TextureHeight, 16,
                RenderTextureFormat.ARGB32)
            {
                name = "MiniMapRenderTexture",
                filterMode = FilterMode.Bilinear
            };
            mapTexture.Create();
            mapCamera.targetTexture = mapTexture;
            mapImage.texture = mapTexture;
        }

        private void RefreshMarkers()
        {
            nextMarkerRefreshTime = Time.unscaledTime + markerRefreshInterval;
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].Rect != null)
                {
                    Destroy(markers[i].Rect.gameObject);
                }
            }
            markers.Clear();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CreateMarker("Player", player.transform,
                    new Color(0.2f, 0.95f, 1f), 13f,
                    RuntimeSpriteFactory.RingSprite, null);
            }

            foreach (MapDoor door in FindObjectsByType<MapDoor>())
            {
                CreateMarker("Exit", door.transform,
                    new Color(0.2f, 1f, 0.4f), 11f,
                    RuntimeSpriteFactory.RingSprite, null);
            }

            foreach (RewardChest chest in FindObjectsByType<RewardChest>())
            {
                CreateMarker("Reward", chest.transform,
                    new Color(1f, 0.76f, 0.15f), 9f,
                    RuntimeSpriteFactory.WhiteSprite, null);
            }

            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
            {
                if (enemy != null && enemy.IsAlive)
                {
                    CreateMarker("Enemy", enemy.transform,
                        new Color(1f, 0.22f, 0.16f), 6f,
                        RuntimeSpriteFactory.CircleSprite, enemy);
                }
            }
        }

        private void CreateMarker(string name, Transform target, Color color,
            float size, Sprite sprite, EnemyHealth enemy)
        {
            GameObject markerObject = new GameObject(name + "Marker",
                typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(markerRoot, false);
            RectTransform rect = markerObject.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * size;
            Image image = markerObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            markers.Add(new Marker
            {
                Target = target,
                Rect = rect,
                Enemy = enemy
            });
        }
    }
}
