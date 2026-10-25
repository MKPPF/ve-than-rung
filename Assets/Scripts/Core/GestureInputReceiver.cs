using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TopDownGame.Core
{
    /// <summary>
    /// Opt-in desktop bridge: F8 starts a localhost listener without changing
    /// generated scenes or prefabs. Gameplay retains ownership of all actions.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GestureInputReceiver : MonoBehaviour
    {
        [Header("Local Gesture Controller")]
        [SerializeField, Range(1024, 65535)] private int port = 5057;
        [SerializeField, Range(0.05f, 0.5f)] private float packetTimeout = 0.3f;
        [SerializeField] private bool allowBackgroundControl = true;

        private UdpClient socket;
        private string session;
        private long sequence = -1;
        private long castId;
        private SkillManager skills;
        private PlayerMovement player;
        private float nextPlayerSearch;
        private string status = "";
        private bool controlEnabled;
        private bool suppressNextBatch = true;
        private float lastPacketTime = float.NegativeInfinity;
        private float nextReplyTime;
        private bool? previousRunInBackground;
        private GameObject statusDisplay;

        public bool ControlEnabled => controlEnabled;
        public bool HasRecentPacket => controlEnabled && Time.realtimeSinceStartup - lastPacketTime < 1f;
        public bool HasStatusDisplay => statusDisplay != null && statusDisplay.activeInHierarchy;

        /// <summary>Lets a scene HUD replace the fallback message without adding another overlay over gameplay hints.</summary>
        public void RegisterStatusDisplay(GameObject display) => statusDisplay = display;
        public string StatusText
        {
            get
            {
                if (!string.IsNullOrEmpty(status)) return status;
                if (!controlEnabled) return "Điều khiển cử chỉ đang tắt.";
                if (!HasRecentPacket) return "Chờ camera. Chạy mục 8 trong notebook cử chỉ.";
                string reason = GetBlockReason();
                if (reason == "focus") return "Đã nối camera. Click Game View để điều khiển.";
                if (reason == "player") return "Đã nối camera. Vào game để điều khiển.";
                if (reason == "paused") return "Đã nối camera. Đóng bảng / bỏ tạm dừng.";
                if (!Application.isFocused) return "Đã nối camera. Đang điều khiển nền; F8 để tắt.";
                return "Đã kết nối. Chỉ ngón trỏ / dịch tay để di chuyển.";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isBatchMode ||
                FindAnyObjectByType<GestureInputReceiver>() != null) return;
            var host = new GameObject("Gesture Input (F8)");
            DontDestroyOnLoad(host);
            host.AddComponent<GestureInputReceiver>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            foreach (string argument in Environment.GetCommandLineArgs())
                if (argument == "--hand-gestures") SetControlEnabled(true);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GestureInputState.Clear();
            player = null;
            skills = null;
            nextPlayerSearch = 0f;
            suppressNextBatch = true;
        }

        public void SetControlEnabled(bool value)
        {
            if (previousRunInBackground.HasValue)
            {
                Application.runInBackground = previousRunInBackground.Value;
                previousRunInBackground = null;
            }
            socket?.Close();
            socket = null;
            GestureInputState.Clear();
            controlEnabled = false;
            session = null;
            sequence = -1;
            castId = 0;
            suppressNextBatch = true;
            status = "";
            lastPacketTime = float.NegativeInfinity;
            nextReplyTime = 0f;
            if (!value) return;
            try
            {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                socket.Client.Blocking = false;
                controlEnabled = true;
                previousRunInBackground = Application.runInBackground;
                Application.runInBackground = true;
            }
            catch (SocketException)
            {
                socket?.Close();
                socket = null;
                status = "Cổng cử chỉ đang bận. Đóng phiên game khác, rồi nhấn F8.";
                Debug.LogWarning(status);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                SetControlEnabled(!controlEnabled);
            if (socket == null) return;
            if (player == null && Time.realtimeSinceStartup >= nextPlayerSearch)
            {
                nextPlayerSearch = Time.realtimeSinceStartup + 0.5f;
                player = FindAnyObjectByType<PlayerMovement>();
                skills = player != null ? player.GetComponent<SkillManager>() : null;
            }

            GesturePacket latest = null;
            IPEndPoint replyEndpoint = null;
            bool castRequested = false;
            // Drain bounded work on the main thread; never block a Unity frame.
            try
            {
                for (int i = 0; i < 64 && socket.Available > 0; i++)
                {
                    var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
                    byte[] bytes = socket.Receive(ref endpoint);
                    if (bytes.Length > 4096) continue;
                    GesturePacket packet;
                    try { packet = JsonUtility.FromJson<GesturePacket>(Encoding.UTF8.GetString(bytes)); }
                    catch (ArgumentException) { continue; }
                    if (packet == null || !packet.IsValid(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())) continue;
                    bool newSession = session != packet.session;
                    if (newSession)
                    {
                        session = packet.session;
                        sequence = -1;
                        castId = packet.castId;
                        castRequested = false;
                    }
                    if (packet.sequence <= sequence || packet.castId < castId) continue;
                    sequence = packet.sequence;
                    // Consume events even during pause/focus loss. Never queue a spell
                    // for later when the player closes a modal or returns to the app.
                    if (packet.castId > castId)
                    {
                        castRequested = packet.skillSlot >= 0;
                        castId = packet.castId;
                    }
                    latest = packet;
                    replyEndpoint = endpoint;
                    lastPacketTime = Time.realtimeSinceStartup;
                }
            }
            catch (SocketException)
            {
                SetControlEnabled(false);
                status = "Kết nối điều khiển tay bị ngắt. Nhấn F8 để kết nối lại.";
                return;
            }

            string blockReason = GetBlockReason();
            bool blocked = !string.IsNullOrEmpty(blockReason);
            if (replyEndpoint != null && Time.realtimeSinceStartup >= nextReplyTime)
            {
                var reply = new GestureConnectionStatus
                {
                    session = session, sequence = sequence, enabled = controlEnabled,
                    focused = Application.isFocused, playerReady = player != null && player.isActiveAndEnabled,
                    blocked = blocked, reason = blockReason
                };
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(reply));
                try { socket.Send(bytes, bytes.Length, replyEndpoint); }
                catch (SocketException) { /* A closed camera cannot interrupt the Unity frame. */ }
                nextReplyTime = Time.realtimeSinceStartup + 0.15f;
            }
            if (blocked)
            {
                GestureInputState.Clear();
                suppressNextBatch = true;
                return;
            }
            if (latest == null) return;
            Vector2 direction = new Vector2(latest.moveX, latest.moveY);
            Vector2 aim = new Vector2(latest.aimX, latest.aimY);
            GestureInputState.Set(direction, aim, packetTimeout);
            if (castRequested && !suppressNextBatch && skills != null)
                skills.TryCastFromGesture(latest.skillSlot, aim);
            suppressNextBatch = false;
        }

        private string GetBlockReason()
        {
            if (player == null || !player.isActiveAndEnabled) return "player";
            if (GameplayState.IsInputBlocked || GameplayState.ModalClosedThisFrame || Time.timeScale <= 0f)
                return "paused";
            return Application.isFocused || allowBackgroundControl ? "" : "focus";
        }

        private void OnApplicationFocus(bool focused)
        {
            GestureInputState.Clear();
            suppressNextBatch = true;
        }

        private void OnGUI()
        {
            if (HasStatusDisplay) return;
            if (!controlEnabled && string.IsNullOrEmpty(status)) return;
            GUI.Box(new Rect(10, Screen.height - 44, Mathf.Min(Screen.width - 20, 900), 34),
                "Điều khiển tay: " + StatusText + " | F8 để tắt");
        }

        private void OnDisable()
        {
            SetControlEnabled(false);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            socket?.Close();
            GestureInputState.Clear();
        }
    }
}
