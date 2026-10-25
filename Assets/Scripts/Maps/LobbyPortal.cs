using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Standing inside opens a destination picker; only an explicit choice can travel.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LobbyPortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string destinationScene = "Map01";
        [SerializeField] private MapData[] destinationMaps;
        [SerializeField, Min(0.2f)] private float chargeDuration = 1.15f;
        [Header("Departure ceremony")]
        [SerializeField, Min(0.5f)] private float departureDuration = 3f;
        private Collider2D activationArea;
        private PlayerInteraction occupant;
        private float chargeTime;
        private bool transporting;
        private bool dismissed;
        private string selectedScene;
        private float departureTime;
        private bool loadRequested;
        private TopDownGame.UI.LobbyTravelUI travelUI;
        public bool IsChoosing => travelUI != null;
        public static readonly string[] Destinations = { "Map01", "Map02", "Map03", "BossMap01", "CaveMap01", "RiftMap01" };

        public MapData GetDestinationData(string scene)
        {
            if (destinationMaps == null) return null;
            foreach (MapData data in destinationMaps)
                if (data != null && data.MapId == scene) return data;
            return null;
        }

        public bool CanInteract => !transporting;
        public string InteractionPrompt => transporting ? "Đang truyền tống..." :
            IsChoosing ? "Chọn điểm đến" : "Bước vào vòng đá • E để chọn điểm đến";
        public float ChargeProgress => Mathf.Clamp01(chargeTime / Mathf.Max(0.2f,chargeDuration));
        public bool IsTransporting => transporting;
        public float DepartureProgress => transporting ? Mathf.Clamp01(departureTime / departureDuration) : 0f;
        public float DepartureDuration => departureDuration;

        private void Awake()
        {
            activationArea = GetComponent<Collider2D>();
            activationArea.isTrigger = true;
        }

        public void Interact(PlayerInteraction player)
        {
            TryOccupy(player);
            if(occupant==player && !IsChoosing) dismissed=false;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryOccupy(other.GetComponentInParent<PlayerInteraction>());
        private void OnTriggerStay2D(Collider2D other) => TryOccupy(other.GetComponentInParent<PlayerInteraction>());

        private void TryOccupy(PlayerInteraction player)
        {
            if(transporting || player==null || !player.CompareTag("Player") ||
                GameplayState.IsInputBlocked || Time.timeScale<=0) return;
            if(!activationArea.OverlapPoint(PhysicalPosition(player))) return;
            if(occupant==player) return;
            occupant=player;
            chargeTime=0;
        }

        private void Update()
        {
            if(transporting) { UpdateDeparture(); return; }
            if(occupant==null) {chargeTime=0; CloseChoice(); return;}
            PlayerHealth health=occupant.GetComponent<PlayerHealth>();
            if(!occupant.gameObject.activeInHierarchy || (health!=null && !health.IsAlive) ||
                !activationArea.OverlapPoint(PhysicalPosition(occupant)))
            {
                occupant=null;
                chargeTime=0;
                dismissed=false;
                CloseChoice();
                return;
            }
            if(Time.timeScale<=0 || GameplayState.IsInputBlocked) return;
            chargeTime+=Time.deltaTime;
            if(chargeTime<chargeDuration || dismissed || IsChoosing) return;
            travelUI=gameObject.AddComponent<TopDownGame.UI.LobbyTravelUI>();
            travelUI.Open(this);
        }

        public bool IsDestinationAvailable(string scene)
        {
            if(System.Array.IndexOf(Destinations,scene)<0) return false;
            var progress=TopDownGame.Save.GameProgressManager.Instance;
            return scene==destinationScene || (progress!=null && progress.IsRegionUnlocked(scene));
        }

        public bool ChooseDestination(string scene)
        {
            if(!IsChoosing || transporting || occupant==null ||
                !occupant.gameObject.activeInHierarchy ||
                !activationArea.OverlapPoint(PhysicalPosition(occupant)) ||
                !IsDestinationAvailable(scene) || GameplayState.IsBlockedByOther(travelUI)) return false;
            var health=occupant.GetComponent<PlayerHealth>();
            if(health!=null && !health.IsAlive) return false;
            if(!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("Không tìm thấy vùng: "+scene,this);
                return false;
            }
            transporting=true;
            selectedScene=scene;
            departureTime=0f;
            loadRequested=false;
            CloseChoice();
            occupant.GetComponent<PlayerMovement>()?.CancelMovement();
            GameplayState.EnterModal(this, false);
            return true;
        }

        private void UpdateDeparture()
        {
            if (loadRequested)
            {
                if (!SceneTransition.IsLoading) CancelDeparture();
                return;
            }
            var health = occupant != null ? occupant.GetComponent<PlayerHealth>() : null;
            if (occupant == null || !occupant.gameObject.activeInHierarchy ||
                (health != null && !health.IsAlive) || !activationArea.OverlapPoint(PhysicalPosition(occupant)))
            {
                CancelDeparture();
                return;
            }
            if (Time.timeScale <= 0f || GameplayState.IsBlockedByOther(this)) return;
            departureTime += Time.deltaTime;
            if (departureTime < departureDuration) return;
            // Save capture and the actual scene load happen only after the entire ceremony.
            loadRequested = true;
            if (!SceneTransition.TryLoadScene(selectedScene, occupant, this)) CancelDeparture();
        }

        private void CancelDeparture()
        {
            transporting = false;
            departureTime = 0f;
            selectedScene = null;
            loadRequested = false;
            dismissed = true;
            GameplayState.ExitModal(this);
            GetComponent<TeleportDepartureVisual>()?.ResetPresentation();
        }

        public void CancelChoice()
        {
            if (transporting) return;
            dismissed=true;
            CloseChoice();
        }

        private void CloseChoice()
        {
            if(travelUI==null) return;
            var closing=travelUI;
            travelUI=null;
            closing.Close();
        }

        private void OnDisable()
        {
            CancelDeparture();
            CloseChoice();
            occupant=null;
            chargeTime=0;
        }

        private static Vector2 PhysicalPosition(PlayerInteraction player)
        {
            Rigidbody2D body=player.GetComponent<Rigidbody2D>();
            return body!=null ? body.position : (Vector2)player.transform.position;
        }
    }
}
