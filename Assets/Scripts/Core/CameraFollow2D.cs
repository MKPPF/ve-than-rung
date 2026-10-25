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
        [Header("Impact Feedback")]
        [SerializeField, Range(0f, 0.3f)] private float maximumShakeAmplitude = 0.16f;
        [SerializeField, Range(0.05f, 0.5f)] private float maximumShakeDuration = 0.3f;

        private Vector3 velocity;
        private Camera attachedCamera;
        private float authoredOrthographicSize;
        private Object cinematicOwner;
        private Vector3 cinematicFocus;
        private float cinematicZoom = 1f;
        private float cinematicBlend;
        private Vector3 unshakenPosition;
        private float shakeStartedAt, shakeEndsAt, shakeAmplitude;
        public static CameraFollow2D Active { get; private set; }
        public Vector3 UnshakenPosition => unshakenPosition;
        public Vector2 ShakeOffset { get; private set; }
        public float MaximumShakeAmplitude => maximumShakeAmplitude;
        public bool IsShaking => shakeAmplitude > 0f && Time.unscaledTime < shakeEndsAt;
        public bool IsCinematic => cinematicOwner != null;

        public bool BeginCinematicFocus(Object owner, Vector3 focus, float zoomMultiplier)
        {
            if (owner == null || (cinematicOwner != null && cinematicOwner != owner)) return false;
            cinematicOwner = owner;
            ClearShake();
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
            unshakenPosition = transform.position;
            if (attachedCamera != null) authoredOrthographicSize = attachedCamera.orthographicSize;
        }

        private void OnEnable()
        {
            if (GetComponent<Camera>() != null && CompareTag("MainCamera")) Active = this;
        }

        private void OnDisable()
        {
            ClearShake();
            if (Active == this) Active = null;
        }

        public bool IsPointVisible(Vector2 position)
        {
            if (attachedCamera == null) return false;
            Vector3 point = attachedCamera.WorldToViewportPoint(position);
            return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
        }

        public void RequestShake(float strength, float duration)
        {
            if (!isActiveAndEnabled || !FeedbackPreferences.CameraShakeEnabled || GameplayState.IsPaused || IsCinematic ||
                strength <= 0f || duration <= 0f) return;
            float now = Time.unscaledTime;
            if (now >= shakeEndsAt)
            {
                shakeStartedAt = now;
                shakeAmplitude = 0f;
            }
            shakeAmplitude = Mathf.Min(maximumShakeAmplitude, Mathf.Max(shakeAmplitude,strength));
            shakeEndsAt = Mathf.Min(shakeStartedAt + maximumShakeDuration, Mathf.Max(shakeEndsAt,now + duration));
        }

        public void ClearShake()
        {
            shakeStartedAt = shakeEndsAt = shakeAmplitude = 0f;
            ShakeOffset = Vector2.zero;
            transform.position = unshakenPosition;
        }

        private Vector3 SampleShake()
        {
            if (!FeedbackPreferences.CameraShakeEnabled || GameplayState.IsPaused || IsCinematic || Time.unscaledTime >= shakeEndsAt)
            {
                ClearShake();
                return Vector3.zero;
            }
            float age = Time.unscaledTime - shakeStartedAt;
            float envelope = Mathf.Clamp01((shakeEndsAt - Time.unscaledTime) / Mathf.Max(0.001f,shakeEndsAt - shakeStartedAt));
            Vector2 sample = new Vector2(Mathf.Sin(age * 97f),Mathf.Sin(age * 123f + 0.7f));
            return Vector2.ClampMagnitude(sample,1f) * shakeAmplitude * envelope;
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
                ClearShake();
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
                : Vector3.SmoothDamp(unshakenPosition, desiredPosition, ref velocity, smoothTime);
            unshakenPosition = Constrained(PixelSnapped(nextPosition));
            Vector3 shake = SampleShake();
            transform.position = Constrained(PixelSnapped(unshakenPosition + shake));
            ShakeOffset = transform.position - unshakenPosition;
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
            unshakenPosition = Constrained(PixelSnapped(target.position + offset));
            ClearShake();
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
