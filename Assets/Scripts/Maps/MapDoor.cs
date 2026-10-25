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

        public bool IsLocked { get; private set; }
        public MapData DestinationMap => destinationMap;
        public string DestinationScene => nextScene;
        public string DestinationName => nextScene == "Lobby" ? LobbyDisplayName
            : destinationMap != null ? destinationMap.DisplayName : "khu vực kế tiếp";
        public bool HasValidDestination => nextScene == "Lobby" ? destinationMap == null
            : destinationMap != null && destinationMap.MapId == nextScene && !string.IsNullOrWhiteSpace(destinationMap.DisplayName);
        public bool CanInteract => isActiveAndEnabled && !IsLocked && !SceneTransition.IsLoading &&
            HasValidDestination && Application.CanStreamedLevelBeLoaded(nextScene);
        public string InteractionPrompt => IsLocked
            ? "Cổng đang khóa"
            : !HasValidDestination || !Application.CanStreamedLevelBeLoaded(nextScene) ? "Cổng chưa sẵn sàng"
            : SceneTransition.IsLoading ? "Đang chuyển vùng..."
            : "E / Nhấp đúp - Đến " + DestinationName + "\n" +
                (destinationMap != null ? "Cấp đề nghị: " + destinationMap.RecommendedMinLevel + "-" +
                    destinationMap.RecommendedMaxLevel + " • " : "") + "Tự động lưu tiến trình";

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
            SetLocked(false);
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (visual != null)
            {
                visual.color = locked ? lockedColor : openColor;
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
            SceneTransition.TryLoadScene(nextScene, player);
        }
    }
}
