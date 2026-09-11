using UnityEngine;

namespace TopDownGame.Maps
{
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            player.transform.position = transform.position;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = transform.position;
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
