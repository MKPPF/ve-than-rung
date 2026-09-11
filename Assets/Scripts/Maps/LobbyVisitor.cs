using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Walks a builder-validated plaza loop without player/combat/save components or solid colliders.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LobbyVisitor : MonoBehaviour
    {
        [Header("Authored plaza loop")]
        [SerializeField] private Vector2[] route;
        [SerializeField, Min(0.1f)] private float speed = 0.8f;
        [SerializeField, Min(0.1f)] private float acceleration = 2.8f;
        [SerializeField, Min(0)] private float restDuration = 3f;
        [SerializeField, Min(0)] private float initialDelay;
        private Rigidbody2D body;
        private Transform player;
        public int RoutePointCount => route == null ? 0 : route.Length;
        private Vector2 velocity;
        private float remainingRest;
        private int destination;
        public Vector2 Velocity => velocity;
        public float DistanceTravelled { get; private set; }
        public bool IsResting => remainingRest > 0;

        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();
            remainingRest=initialDelay;
            var actor=GameObject.FindGameObjectWithTag("Player"); player=actor!=null?actor.transform:null;
            destination=route!=null && route.Length>1 ? 1 : 0;
        }
        private void FixedUpdate()
        {
            if(route==null || route.Length<2){velocity=Vector2.zero;return;}
            if(remainingRest>0){remainingRest=Mathf.Max(0,remainingRest-Time.fixedDeltaTime);velocity=Vector2.zero;return;}
            Vector2 displacement=route[destination]-body.position;
            float distance=displacement.magnitude;
            if(distance<0.025f)
            {
                velocity=Vector2.zero;destination=(destination+1)%route.Length;
                remainingRest=restDuration;return;
            }
            // Yield before crossing the player's feet, without adding a blocking collider.
            if(player!=null && Vector2.Distance(player.position,body.position)<0.85f &&
                Vector2.Dot((Vector2)player.position-body.position,displacement)>0)
            {velocity=Vector2.MoveTowards(velocity,Vector2.zero,acceleration*2*Time.fixedDeltaTime);return;}
            float brakingSpeed=Mathf.Sqrt(2*acceleration*distance);
            Vector2 desired=displacement.normalized*Mathf.Min(speed,brakingSpeed);
            velocity=Vector2.MoveTowards(velocity,desired,acceleration*Time.fixedDeltaTime);
            Vector2 step=Vector2.ClampMagnitude(velocity*Time.fixedDeltaTime,distance);
            body.MovePosition(body.position+step);DistanceTravelled+=step.magnitude;
        }
        private void OnDisable(){velocity=Vector2.zero;}
    }
}
