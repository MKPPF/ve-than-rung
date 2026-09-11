using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>
    /// Follows an interpolated physics target in LateUpdate.
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0f;
        [SerializeField] private bool pixelSnap = false;
        [Header("Optional Authored Map Bounds")]
        [SerializeField] private bool constrainToBounds;
        [SerializeField] private Rect worldBounds = new Rect(-13f, -7.8f, 26f, 15.6f);

        private Vector3 velocity;
        private Camera attachedCamera;
        private float authoredOrthographicSize;
        private Object cinematicOwner;
        private Vector3 cinematicFocus;
        private float cinematicZoom = 1f;
        private float cinematicBlend;
        public bool IsCinematic => cinematicOwner != null;

        public bool BeginCinematicFocus(Object owner, Vector3 focus, float zoomMultiplier)
        {
            if (owner == null || (cinematicOwner != null && cinematicOwner != owner)) return false;
            cinematicOwner = owner;
            cinematicFocus = focus;
            cinematicZoom = Mathf.Max(1f, zoomMultiplier);
            cinematicBlend = 0f;
            return true;
        }

        public void SetCinematicProgress(Object owner, float progress)
        {
            if (cinematicOwner != owner) return;
            cinematicBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        }

        public void EndCinematicFocus(Object owner)
        {
            if (cinematicOwner != owner) return;
            cinematicOwner = null;
            cinematicBlend = 0f;
            cinematicZoom = 1f;
            if (attachedCamera != null) attachedCamera.orthographicSize = authoredOrthographicSize;
            SnapToTarget();
        }

        private void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            if (attachedCamera != null) authoredOrthographicSize = attachedCamera.orthographicSize;
        }

        private void Start()
        {
            if (ResolveTarget())
            {
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (!ResolveTarget())
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            if (IsCinematic)
            {
                desiredPosition = Vector3.Lerp(desiredPosition, cinematicFocus + offset, cinematicBlend);
                if (attachedCamera != null)
                    attachedCamera.orthographicSize = authoredOrthographicSize * Mathf.Lerp(1f, cinematicZoom, cinematicBlend);
            }
            Vector3 nextPosition = smoothTime <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            transform.position = Constrained(PixelSnapped(nextPosition));
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        private bool ResolveTarget()
        {
            if (target != null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(targetTag))
            {
                return false;
            }

            GameObject candidate = GameObject.FindGameObjectWithTag(targetTag);
            if (candidate == null)
            {
                return false;
            }

            target = candidate.transform;
            velocity = Vector3.zero;
            return true;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = Constrained(PixelSnapped(target.position + offset));
        }

        private Vector3 Constrained(Vector3 position)
        {
            if (!constrainToBounds || attachedCamera == null || !attachedCamera.orthographic)
                return position;
            // When an ultrawide viewport cannot fit the painted map, reduce the
            // vertical view rather than exposing unpainted world outside it.
            float requestedSize = authoredOrthographicSize * (IsCinematic ? Mathf.Lerp(1f, cinematicZoom, cinematicBlend) : 1f);
            float halfHeight = Mathf.Min(requestedSize,
                worldBounds.height * 0.5f, worldBounds.width * 0.5f / Mathf.Max(0.1f, attachedCamera.aspect));
            attachedCamera.orthographicSize = halfHeight;
            Vector2 center = ClampViewportCenter(position, worldBounds,
                halfHeight, attachedCamera.aspect);
            return new Vector3(center.x, center.y, position.z);
        }

        public static Vector2 ClampViewportCenter(Vector2 desired, Rect bounds, float halfHeight, float aspect)
        {
            float halfWidth = Mathf.Max(0f, halfHeight * aspect);
            float x = halfWidth * 2 >= bounds.width ? bounds.center.x :
                Mathf.Clamp(desired.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            float y = halfHeight * 2 >= bounds.height ? bounds.center.y :
                Mathf.Clamp(desired.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
            return new Vector2(x, y);
        }

        private Vector3 PixelSnapped(Vector3 position)
        {
            if (!pixelSnap || attachedCamera == null ||
                !attachedCamera.orthographic || attachedCamera.pixelHeight <= 0)
            {
                return position;
            }

            float unitsPerPixel = attachedCamera.orthographicSize * 2f /
                attachedCamera.pixelHeight;
            position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
            position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
            return position;
        }
    }
}
