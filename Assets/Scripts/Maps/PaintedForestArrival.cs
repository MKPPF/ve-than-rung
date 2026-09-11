using TopDownGame.Enemies;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Old Map02 sessions may hold a position from the retired layout; recover only invalid arrivals, without resetting progress.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class PaintedForestArrival : MonoBehaviour
    {
        [SerializeField] private Vector2 entrance;
        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var navigation = ForestNavigation.Instance;
            if (player == null || navigation == null || navigation.CanStandAt(player.transform.position)) return;
            player.GetComponent<PlayerMovement>()?.CancelMovement();
            player.transform.position = entrance;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = entrance; body.linearVelocity = Vector2.zero; }
            GameProgressManager.Instance?.SetSafeRespawn("Map02", entrance);
        }
    }
}
