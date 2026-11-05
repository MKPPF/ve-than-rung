using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Uses generated destination metadata and accepts nearby, living players before saving or travelling.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MapDoor : MonoBehaviour, IInteractable
    {
        public const string LobbyDisplayName = "Trạm nghỉ Tán Rừng";
        [SerializeField] private string nextScene = "Map02";
        [SerializeField] private MapData destinationMap;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color lockedColor = new Color(0.45f, 0.12f, 0.12f);
        [SerializeField] private Color openColor = new Color(0.15f, 0.8f, 0.35f);

        private PlayerStats cachedPlayerStats;
        private bool lastReachedState;

        public bool IsLocked { get; private set; }
        public MapData DestinationMap => destinationMap;
        public string DestinationScene => nextScene;
        public string DestinationName => nextScene == "Lobby" ? LobbyDisplayName
            : destinationMap != null ? destinationMap.DisplayName : "khu vực kế tiếp";
        public bool HasValidDestination => nextScene == "Lobby" ? destinationMap == null
            : destinationMap != null && destinationMap.MapId == nextScene && !string.IsNullOrWhiteSpace(destinationMap.DisplayName);
        public bool CanInteract => isActiveAndEnabled && !IsLocked && !SceneTransition.IsLoading &&
            HasValidDestination && Application.CanStreamedLevelBeLoaded(nextScene);

        public int RequiredLevel => nextScene == "Lobby" ? 1
            : destinationMap != null ? destinationMap.RecommendedMinLevel
            : MapData.RecommendedMinimumForScene(nextScene);

        private PlayerStats GetPlayerStats()
        {
            if (cachedPlayerStats == null || !cachedPlayerStats.gameObject.activeInHierarchy)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                cachedPlayerStats = player != null ? player.GetComponent<PlayerStats>() : null;
            }
            return cachedPlayerStats;
        }

        public bool HasReachedRequiredLevel(PlayerStats stats = null)
        {
            if (nextScene == "Lobby" || GamePreferences.IsAutomatedTestRun)
            {
                return true;
            }
            if (stats == null)
            {
                stats = GetPlayerStats();
            }
            return stats != null && stats.Level >= RequiredLevel;
        }

        public string InteractionPrompt
        {
            get
            {
                if (IsLocked) return "Cổng đang khóa";
                if (!HasValidDestination || !Application.CanStreamedLevelBeLoaded(nextScene)) return "Cổng chưa sẵn sàng";
                if (SceneTransition.IsLoading) return "Đang chuyển vùng...";

                PlayerStats stats = GetPlayerStats();
                if (!HasReachedRequiredLevel(stats))
                {
                    int currentLvl = stats != null ? stats.Level : 1;
                    return "Cần đạt Cấp " + RequiredLevel + " để đến " + DestinationName + "\n" +
                        "(Hiện tại: Cấp " + currentLvl + "/" + RequiredLevel + " • Hãy săn quái để lên cấp)";
                }

                return "E / Nhấp đúp - Đến " + DestinationName + "\n" +
                    (destinationMap != null ? "Cấp đề nghị: " + destinationMap.RecommendedMinLevel + "-" +
                        destinationMap.RecommendedMaxLevel + " • " : "") + "Tự động lưu tiến trình";
            }
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual == null)
            {
                visual = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            lastReachedState = HasReachedRequiredLevel();
            SetLocked(false);
        }

        private void Update()
        {
            bool reached = HasReachedRequiredLevel();
            if (!lastReachedState && reached && !IsLocked && nextScene != "Lobby")
            {
                lastReachedState = true;
                TopDownGame.UI.GameplayHUD.ShowNotice(
                    "CỔNG ĐẾN " + DestinationName.ToUpperInvariant() + " ĐÃ MỞ!\n(Đã đạt Cấp " + RequiredLevel + ")",
                    new Color(0.3f, 1f, 0.45f), 3.5f);
            }
            UpdateVisualColor();
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            UpdateVisualColor();
        }

        private void UpdateVisualColor()
        {
            if (visual != null)
            {
                visual.color = (IsLocked || !HasReachedRequiredLevel()) ? lockedColor : openColor;
            }
        }

        public void Interact(PlayerInteraction player)
        {
            if (!CanInteract || player == null || !player.isActiveAndEnabled || GameplayState.IsInputBlocked ||
                Time.timeScale <= 0f) return;
            Collider2D area = GetComponent<Collider2D>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Vector2 position = body != null ? body.position : (Vector2)player.transform.position;
            if (!area.enabled || Vector2.Distance(area.ClosestPoint(position), position) > player.InteractionRadius) return;

            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (!HasReachedRequiredLevel(stats))
            {
                int currentLvl = stats != null ? stats.Level : 1;
                TopDownGame.UI.GameplayHUD.ShowNotice(
                    "Cần đạt Cấp " + RequiredLevel + " để mở cổng đến " + DestinationName + "! (Hiện tại: Cấp " + currentLvl + ")",
                    new Color(1f, 0.45f, 0.45f), 2.5f);
                return;
            }

            SceneTransition.TryLoadScene(nextScene, player);
        }
    }
}
