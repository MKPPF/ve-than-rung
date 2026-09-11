using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Small shared terrain-only navigation grid for the painted forest. Each
    /// route honours actor clearance and smooths only collision-free segments.
    /// It never steers by teleporting or changes the animator's visual transform.
    /// </summary>
    public sealed class ForestNavigation : MonoBehaviour
    {
        [SerializeField] private Rect worldBounds = new Rect(-18, -12, 36, 24);
        [SerializeField, Min(0.25f)] private float cellSize = 0.5f;
        [SerializeField, Min(0.1f)] private float actorRadius = 0.55f;
        private bool[] free;
        private int[] previous, queue;
        private int width, height;
        private ContactFilter2D filter;
        private readonly Collider2D[] overlaps = new Collider2D[1];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[1];
        public static ForestNavigation Instance { get; private set; }

        private void Awake() { Instance = this; }
        private void Start() { Rebuild(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Rebuild()
        {
            Physics2D.SyncTransforms();
            int mask = Physics2D.AllLayers;
            if (GameplayCollisionRules.PlayerLayer >= 0) mask &= ~(1 << GameplayCollisionRules.PlayerLayer);
            if (GameplayCollisionRules.EnemyLayer >= 0) mask &= ~(1 << GameplayCollisionRules.EnemyLayer);
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(mask);
            width = Mathf.CeilToInt(worldBounds.width / cellSize);
            height = Mathf.CeilToInt(worldBounds.height / cellSize);
            free = new bool[width * height];
            previous = new int[free.Length];
            queue = new int[free.Length];
            for (int i = 0; i < free.Length; i++) free[i] = CanStandAt(Center(i));
        }

        public bool CanStandAt(Vector2 p) => worldBounds.Contains(p) &&
            Physics2D.OverlapCircle(p, actorRadius, filter, overlaps) == 0;

        public bool CanTravelDirectly(Vector2 a, Vector2 b) => CanStandAt(b) &&
            Physics2D.CircleCast(a, actorRadius, (b - a).normalized,
                filter, hits, Vector2.Distance(a, b)) == 0;

        private Vector2 Center(int i) => worldBounds.min +
            new Vector2((i % width + 0.5f) * cellSize, (i / width + 0.5f) * cellSize);

        private int Nearest(Vector2 p)
        {
            int x = Mathf.FloorToInt((p.x - worldBounds.xMin) / cellSize);
            int y = Mathf.FloorToInt((p.y - worldBounds.yMin) / cellSize);
            int best = -1;
            float distance = float.MaxValue;
            for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                int i = ny * width + nx;
                float d = (Center(i) - p).sqrMagnitude;
                if (free[i] && d < distance) { best = i; distance = d; }
            }
            return best;
        }

        public bool TryWaypoint(Vector2 start, Vector2 goal, out Vector2 waypoint)
        {
            if (free == null) Rebuild();
            waypoint = start;
            if (CanTravelDirectly(start, goal)) { waypoint = goal; return true; }
            int from = Nearest(start), to = Nearest(goal);
            if (from < 0 || to < 0) return false;
            System.Array.Fill(previous, -1);
            int read = 0, write = 0;
            queue[write++] = from;
            previous[from] = from;
            while (read < write && previous[to] < 0)
            {
                int current = queue[read++];
                int x = current % width, y = current / width;
                if (x > 0) Visit(current, current - 1, ref write);
                if (x < width - 1) Visit(current, current + 1, ref write);
                if (y > 0) Visit(current, current - width, ref write);
                if (y < height - 1) Visit(current, current + width, ref write);
            }
            if (previous[to] < 0) return false;
            int node = to;
            while (true)
            {
                Vector2 point = Center(node);
                if (CanTravelDirectly(start, point)) { waypoint = point; return true; }
                if (node == from) break;
                node = previous[node];
            }
            return false;
        }

        private void Visit(int from, int to, ref int count)
        {
            if (!free[to] || previous[to] >= 0) return;
            previous[to] = from;
            queue[count++] = to;
        }
    }

    /// <summary>Caches a route corner per enemy to keep path searches off the per-frame hot path.</summary>
    public sealed class ForestPathAgent
    {
        private Vector2 waypoint, lastGoal;
        private float nextSearch;
        private bool hasRoute;

        public void Reset() { nextSearch = 0; hasRoute = false; }

        public void Move(EnemyMovement movement, Vector2 position, Vector2 goal, float speed)
        {
            ForestNavigation navigation = ForestNavigation.Instance;
            if (navigation == null) { movement.MoveTowards(goal, speed); return; }
            if (Time.time >= nextSearch || (lastGoal - goal).sqrMagnitude > 1f ||
                (position - waypoint).sqrMagnitude < 0.15f)
            {
                nextSearch = Time.time + 0.45f;
                lastGoal = goal;
                hasRoute = navigation.TryWaypoint(position, goal, out waypoint);
            }
            if (hasRoute) movement.MoveTowards(waypoint, speed);
            else movement.Stop();
        }
    }
}
