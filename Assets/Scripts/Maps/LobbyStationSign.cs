using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Station names remain readable while input hints appear only when close, leaving the scenery uncluttered.</summary>
    public sealed class LobbyStationSign : MonoBehaviour
    {
        [SerializeField] private Transform station;
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh prompt;
        [SerializeField] private Color accent = Color.white;
        private Transform player;
        private float nextResolveTime;
        private Renderer titleRenderer;

        private void Start()
        {
            if (title != null) titleRenderer = title.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (player == null && Time.unscaledTime >= nextResolveTime)
            {
                nextResolveTime = Time.unscaledTime + 1f;
                GameObject target = GameObject.FindWithTag("Player");
                player = target != null ? target.transform : null;
            }
            if (station == null || title == null || prompt == null) return;
            float distance = player != null ? Vector2.Distance(player.position, station.position) : 100f;
            Color color = accent;
            color.a = Mathf.Lerp(0.3f, 1f, 1f - Mathf.InverseLerp(2.2f, 6f, distance));
            title.color = color;
            var portal = station.GetComponent<LobbyPortal>();
            bool departing = portal != null && portal.IsTransporting;
            if (titleRenderer != null) titleRenderer.enabled = !departing;
            if (portal != null) prompt.text = "BƯỚC VÀO VÒNG ĐÁ";
            prompt.gameObject.SetActive(!departing && distance < 2.2f);
        }
    }
}
