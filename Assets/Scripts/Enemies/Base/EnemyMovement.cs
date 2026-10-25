using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TopDownGame.Enemies
{
    public enum EnemyMobilityStyle
    {
        Hopper,
        Swarm,
        Skirmisher,
        Hover,
        Heavy,
        Skitter,
        Rift,
        Colossus
    }

    /// <summary>Owns physics locomotion and committed charge routes so combat controllers never move bodies outside the collision solver.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [Header("Motion Feel")]
        [SerializeField, Min(0.1f)] private float acceleration = 12f;
        [SerializeField, Min(0.1f)] private float deceleration = 18f;
        [SerializeField, Min(45f)] private float turnSpeedDegrees = 540f;
        [SerializeField, Min(0.1f)] private float impulseDamping = 20f;

        [Header("Crowd Navigation")]
        [SerializeField, Min(0.1f)] private float separationRadius = 0.78f;
        [SerializeField, Min(0f)] private float separationStrength = 1.35f;
        [SerializeField, Min(0.1f)] private float navigationRefreshRate = 0.12f;
        [SerializeField, Min(0.1f)] private float stuckDetectionTime = 0.55f;
        [SerializeField, Min(0.1f)] private float unstickDuration = 0.42f;

        [Header("Obstacle Navigation")]
        [SerializeField, Min(0.1f)] private float obstacleProbeDistance = 0.9f;
        [SerializeField, Range(0f, 2f)] private float obstacleAvoidanceStrength =
            0.85f;
        [SerializeField, Range(0.005f, 0.15f)] private float surfaceSkin = 0.035f;
        [SerializeField, Range(1, 4)] private int maxSlideIterations = 2;

        private Rigidbody2D body;
        private Vector2 desiredVelocity;
        private Vector2 currentVelocity;
        private Vector2 impulseVelocity;
        private Vector2 separationVelocity;
        private Vector2 obstacleAvoidanceVelocity;
        private Vector2 lastPosition;
        private Vector2 unstickDirection;
        private float speedMultiplier = 1f;
        private float nextNavigationRefresh;
        private float stuckTimer;
        private float unstickUntil;
        private float weaveSeed;
        private Vector2 chargeStart;
        private Vector2 chargeDestination;
        private Vector2 chargeExpectedPosition;
        private float chargeElapsed;
        private float chargeDuration;
        private readonly Collider2D[] nearbyColliders = new Collider2D[12];
        private readonly List<RaycastHit2D> obstacleHits =
            new List<RaycastHit2D>(4);
        private ContactFilter2D separationFilter;
        private ContactFilter2D movementFilter;

        public Vector2 CurrentVelocity => currentVelocity + impulseVelocity;
        public float Speed => CurrentVelocity.magnitude;
        public EnemyMobilityStyle MobilityStyle { get; private set; } =
            EnemyMobilityStyle.Swarm;
        public bool IsUnsticking => Time.time < unstickUntil;
        public bool IsAvoidingObstacle { get; private set; }
        public Vector2 LastResolvedVelocity { get; private set; }
        public Vector2 Position => body.position;
        public bool IsCharging { get; private set; }
        public bool HasReachedChargeDestination { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            lastPosition = body.position;
            weaveSeed = (EntityId.ToULong(GetEntityId()) % 1000UL) * 0.01f;
            separationFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            int enemyLayer = LayerMask.NameToLayer("EnemyCharacter");
            if (enemyLayer >= 0)
            {
                separationFilter.SetLayerMask(1 << enemyLayer);
            }
            int movementMask = Physics2D.AllLayers;
            int playerLayer = LayerMask.NameToLayer("PlayerCharacter");
            if (playerLayer >= 0) movementMask &= ~(1 << playerLayer);
            if (enemyLayer >= 0) movementMask &= ~(1 << enemyLayer);
            movementFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            movementFilter.SetLayerMask(movementMask);
            if (GetComponent<EnemyCharacterAnimator>() == null)
            {
                gameObject.AddComponent<EnemyCharacterAnimator>();
            }
        }

        private void FixedUpdate()
        {
            if (IsCharging)
            {
                AdvanceCharge();
                return;
            }
            RefreshNavigation(Time.fixedDeltaTime);
            UpdateSteeredVelocity(Time.fixedDeltaTime);
            impulseVelocity = Vector2.MoveTowards(impulseVelocity, Vector2.zero,
                impulseDamping * Time.fixedDeltaTime);
            Vector2 startPosition = body.position;
            Vector2 resolvedPosition = ResolveSurfaceSliding(startPosition,
                CurrentVelocity * Time.fixedDeltaTime);
            LastResolvedVelocity = Time.fixedDeltaTime > 0f
                ? (resolvedPosition - startPosition) / Time.fixedDeltaTime
                : Vector2.zero;
            body.MovePosition(resolvedPosition);
            lastPosition = body.position;
        }

        private void UpdateSteeredVelocity(float deltaTime)
        {
            Vector2 navigatedVelocity = desiredVelocity;
            if (navigatedVelocity.sqrMagnitude > 0.001f)
            {
                navigatedVelocity += separationVelocity;
                navigatedVelocity += obstacleAvoidanceVelocity;
                navigatedVelocity = ApplyMobilityStyle(navigatedVelocity);
                if (IsUnsticking)
                {
                    float requestedSpeed = Mathf.Max(0.1f,
                        desiredVelocity.magnitude);
                    navigatedVelocity = Vector2.Lerp(navigatedVelocity,
                        unstickDirection * requestedSpeed, 0.72f);
                }
            }

            float targetSpeed = navigatedVelocity.magnitude;
            float currentSpeed = currentVelocity.magnitude;
            if (targetSpeed < 0.001f)
            {
                currentVelocity = Vector2.MoveTowards(currentVelocity,
                    Vector2.zero, deceleration * deltaTime);
                return;
            }

            float rate = targetSpeed > currentSpeed ? acceleration : deceleration;
            float nextSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
                rate * deltaTime);
            Vector2 targetDirection = navigatedVelocity / targetSpeed;
            if (currentSpeed < 0.03f)
            {
                currentVelocity = targetDirection * nextSpeed;
                return;
            }

            float currentAngle = Mathf.Atan2(currentVelocity.y,
                currentVelocity.x) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(targetDirection.y,
                targetDirection.x) * Mathf.Rad2Deg;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle,
                turnSpeedDegrees * deltaTime);
            Vector2 nextDirection = new Vector2(
                Mathf.Cos(nextAngle * Mathf.Deg2Rad),
                Mathf.Sin(nextAngle * Mathf.Deg2Rad));
            currentVelocity = nextDirection * nextSpeed;
        }

        public void MoveTowards(Vector2 target, float speed)
        {
            desiredVelocity = (target - body.position).normalized * Mathf.Max(0f, speed) * speedMultiplier;
        }

        public void Pursue(Transform target, float speed,
            float predictionSeconds = 0.18f)
        {
            if (target == null)
            {
                Stop();
                return;
            }

            Vector2 predictedPosition = target.position;
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
            {
                predictedPosition += targetBody.linearVelocity *
                    Mathf.Clamp(predictionSeconds, 0f, 0.6f);
            }

            MoveTowards(predictedPosition, speed);
        }

        public void Arrive(Vector2 target, float maximumSpeed,
            float stopDistance, float slowingDistance)
        {
            Vector2 offset = target - body.position;
            float distance = offset.magnitude;
            float safeStopDistance = Mathf.Max(0f, stopDistance);
            float safeSlowingDistance = Mathf.Max(0.05f, slowingDistance);
            if (distance <= safeStopDistance || distance < 0.001f)
            {
                desiredVelocity = Vector2.zero;
                return;
            }

            float remainingDistance = distance - safeStopDistance;
            float speedScale = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(remainingDistance / safeSlowingDistance));
            desiredVelocity = offset / distance * Mathf.Max(0f, maximumSpeed) *
                speedScale * speedMultiplier;
        }

        public void MoveAway(Vector2 target, float speed)
        {
            desiredVelocity = (body.position - target).normalized * Mathf.Max(0f, speed) * speedMultiplier;
        }

        public void Strafe(Vector2 target, float speed, float direction)
        {
            Vector2 toward = (target - body.position).normalized;
            desiredVelocity = new Vector2(-toward.y, toward.x) *
                Mathf.Sign(direction) * Mathf.Max(0f, speed) * speedMultiplier;
        }

        public void Orbit(Vector2 target, float speed, float desiredDistance,
            float direction, float radialCorrection = 1.15f)
        {
            Vector2 offset = target - body.position;
            float distance = offset.magnitude;
            if (distance < 0.001f)
            {
                desiredVelocity = Vector2.right * Mathf.Max(0f, speed) *
                    Mathf.Sign(direction) * speedMultiplier;
                return;
            }

            Vector2 toward = offset / distance;
            Vector2 tangent = new Vector2(-toward.y, toward.x) *
                Mathf.Sign(direction);
            float distanceError = distance - Mathf.Max(0.1f, desiredDistance);
            Vector2 correctedDirection = tangent + toward * Mathf.Clamp(
                distanceError * radialCorrection, -0.85f, 0.85f);
            desiredVelocity = correctedDirection.normalized *
                Mathf.Max(0f, speed) * speedMultiplier;
        }

        public void Stop()
        {
            desiredVelocity = Vector2.zero;
        }

        public void StopImmediately(bool clearImpulse = true)
        {
            IsCharging = false;
            HasReachedChargeDestination = false;
            desiredVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            if (clearImpulse)
            {
                impulseVelocity = Vector2.zero;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void AddImpulse(Vector2 impulse)
        {
            if (IsCharging && impulse.sqrMagnitude > 0.0001f)
                StopImmediately();
            impulseVelocity += impulse / Mathf.Max(0.1f, body.mass);
        }

        /// <summary>Preserves the old damped impulse's nominal travel, clamped to the first solid surface before it is shown to the player.</summary>
        public Vector2 PlanChargeDestination(Vector2 impulse, float duration)
        {
            float speed = impulse.magnitude / Mathf.Max(0.1f, body.mass);
            float distance = 0f;
            float remaining = Mathf.Max(0.01f, duration);
            while (remaining > 0.00001f)
            {
                float step = Mathf.Min(remaining, Mathf.Max(0.001f, Time.fixedDeltaTime));
                speed = Mathf.MoveTowards(speed, 0f, impulseDamping * step);
                distance += speed * step;
                remaining -= step;
            }
            return ResolveStraightMovement(body.position, body.position + impulse.normalized * distance);
        }

        /// <summary>Follows an already warned route with eased endpoints; knockback or a new obstacle interrupts it instead of moving its impact.</summary>
        public void BeginCharge(Vector2 destination, float duration)
        {
            StopImmediately();
            chargeStart = body.position;
            chargeExpectedPosition = chargeStart;
            chargeDestination = destination;
            chargeDuration = Mathf.Max(0.01f, duration);
            chargeElapsed = 0f;
            IsAvoidingObstacle = false;
            IsCharging = true;
        }

        private void AdvanceCharge()
        {
            if (Vector2.Distance(body.position, chargeExpectedPosition) > 0.12f)
            {
                StopImmediately();
                return;
            }
            chargeElapsed = Mathf.Min(chargeDuration, chargeElapsed + Time.fixedDeltaTime);
            Vector2 next = Vector2.Lerp(chargeStart, chargeDestination,
                Mathf.SmoothStep(0f, 1f, chargeElapsed / chargeDuration));
            Vector2 resolved = ResolveStraightMovement(body.position, next);
            Vector2 velocity = (resolved - body.position) / Time.fixedDeltaTime;
            bool blocked = Vector2.Distance(resolved, next) > 0.001f;
            body.MovePosition(resolved);
            chargeExpectedPosition = resolved;
            LastResolvedVelocity = velocity;
            currentVelocity = velocity;
            lastPosition = body.position;
            if (blocked || chargeElapsed >= chargeDuration - 0.00001f)
            {
                IsCharging = false;
                HasReachedChargeDestination = !blocked;
                currentVelocity = Vector2.zero;
            }
        }

        private Vector2 ResolveStraightMovement(Vector2 start, Vector2 destination)
        {
            Vector2 delta = destination - start;
            float distance = delta.magnitude;
            if (distance < 0.0001f) return start;
            Vector2 direction = delta / distance;
            obstacleHits.Clear();
            body.Cast(start, body.rotation, direction, movementFilter,
                obstacleHits, distance + surfaceSkin);
            foreach (RaycastHit2D hit in obstacleHits)
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - surfaceSkin));
            return start + direction * distance;
        }

        public void ApplySlow(float multiplier, float duration)
        {
            StartCoroutine(SlowRoutine(multiplier, duration));
        }

        public void ConfigureArchetype(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Slime:
                    MobilityStyle = EnemyMobilityStyle.Hopper;
                    separationStrength = 1.15f;
                    break;
                case EnemyArchetype.Goblin:
                    MobilityStyle = EnemyMobilityStyle.Swarm;
                    separationStrength = 1.55f;
                    break;
                case EnemyArchetype.Archer:
                    MobilityStyle = EnemyMobilityStyle.Skirmisher;
                    separationStrength = 1.7f;
                    break;
                case EnemyArchetype.Mage:
                    MobilityStyle = EnemyMobilityStyle.Hover;
                    separationStrength = 1.65f;
                    break;
                case EnemyArchetype.Tank:
                    MobilityStyle = EnemyMobilityStyle.Heavy;
                    separationStrength = 1.9f;
                    break;
                case EnemyArchetype.CrystalScorpion:
                    MobilityStyle = EnemyMobilityStyle.Skitter;
                    separationStrength = 1.5f;
                    break;
                case EnemyArchetype.VoidReaver:
                    MobilityStyle = EnemyMobilityStyle.Rift;
                    separationStrength = 1.8f;
                    break;
                case EnemyArchetype.Boss:
                    MobilityStyle = EnemyMobilityStyle.Colossus;
                    separationStrength = 2.25f;
                    break;
            }
        }

        public void ResetMotion()
        {
            StopAllCoroutines();
            IsCharging = false;
            HasReachedChargeDestination = false;
            desiredVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            impulseVelocity = Vector2.zero;
            separationVelocity = Vector2.zero;
            obstacleAvoidanceVelocity = Vector2.zero;
            speedMultiplier = 1f;
            stuckTimer = 0f;
            unstickUntil = 0f;
            IsAvoidingObstacle = false;
            LastResolvedVelocity = Vector2.zero;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                lastPosition = body.position;
            }
        }

        private void RefreshNavigation(float deltaTime)
        {
            bool wantsToMove = desiredVelocity.sqrMagnitude > 0.04f;
            float movedDistance = Vector2.Distance(body.position, lastPosition);
            if (wantsToMove && impulseVelocity.sqrMagnitude < 0.1f &&
                movedDistance < 0.0025f)
            {
                stuckTimer += deltaTime;
                if (stuckTimer >= stuckDetectionTime)
                {
                    Vector2 forward = desiredVelocity.normalized;
                    float side = (EntityId.ToULong(GetEntityId()) & 1UL) == 0UL
                        ? 1f
                        : -1f;
                    unstickDirection = new Vector2(-forward.y, forward.x) * side;
                    unstickUntil = Time.time + unstickDuration;
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = Mathf.Max(0f, stuckTimer - deltaTime * 2f);
            }

            if (Time.time < nextNavigationRefresh)
            {
                return;
            }

            nextNavigationRefresh = Time.time + navigationRefreshRate;
            separationVelocity = CalculateSeparationVelocity();
            obstacleAvoidanceVelocity = CalculateObstacleAvoidanceVelocity();
        }

        private Vector2 CalculateObstacleAvoidanceVelocity()
        {
            IsAvoidingObstacle = false;
            float requestedSpeed = desiredVelocity.magnitude;
            if (requestedSpeed < 0.05f || obstacleAvoidanceStrength <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 direction = desiredVelocity / requestedSpeed;
            obstacleHits.Clear();
            int hitCount = body.Cast(body.position, body.rotation, direction,
                movementFilter, obstacleHits, obstacleProbeDistance);
            if (hitCount <= 0)
            {
                return Vector2.zero;
            }

            RaycastHit2D hit = obstacleHits[0];
            Vector2 slide = direction - hit.normal *
                Vector2.Dot(direction, hit.normal);
            if (slide.sqrMagnitude < 0.04f)
            {
                float side = (EntityId.ToULong(GetEntityId()) & 1UL) == 0UL
                    ? 1f
                    : -1f;
                slide = new Vector2(-hit.normal.y, hit.normal.x) * side;
            }

            IsAvoidingObstacle = true;
            float proximity = 1f - Mathf.Clamp01(hit.distance /
                Mathf.Max(0.1f, obstacleProbeDistance));
            return slide.normalized * requestedSpeed *
                obstacleAvoidanceStrength * Mathf.Lerp(0.45f, 1f, proximity);
        }

        private Vector2 ResolveSurfaceSliding(Vector2 startPosition,
            Vector2 displacement)
        {
            float remainingDistance = displacement.magnitude;
            if (remainingDistance < 0.0001f)
            {
                return startPosition;
            }

            Vector2 direction = displacement / remainingDistance;
            Vector2 resolvedPosition = startPosition;
            int iterations = Mathf.Max(1, maxSlideIterations);
            while (iterations-- > 0 && remainingDistance > 0.0001f &&
                direction.sqrMagnitude > 0.0001f)
            {
                obstacleHits.Clear();
                int hitCount = body.Cast(resolvedPosition, body.rotation,
                    direction, movementFilter, obstacleHits,
                    remainingDistance + surfaceSkin);
                if (hitCount <= 0)
                {
                    resolvedPosition += direction * remainingDistance;
                    break;
                }

                RaycastHit2D hit = obstacleHits[0];
                float travelled = Mathf.Max(0f,
                    Mathf.Min(remainingDistance, hit.distance - surfaceSkin));
                resolvedPosition += direction * travelled;
                remainingDistance -= travelled;
                direction -= hit.normal * Vector2.Dot(direction, hit.normal);
                IsAvoidingObstacle = true;
            }

            return resolvedPosition;
        }

        private Vector2 CalculateSeparationVelocity()
        {
            if (separationStrength <= 0f || separationRadius <= 0f)
            {
                return Vector2.zero;
            }

            int count = Physics2D.OverlapCircle(body.position, separationRadius,
                separationFilter, nearbyColliders);
            Vector2 separation = Vector2.zero;
            int neighbours = 0;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = nearbyColliders[i];
                if (candidate == null || candidate.attachedRigidbody == body)
                {
                    continue;
                }

                EnemyMovement other = candidate.GetComponentInParent<EnemyMovement>();
                if (other == null || other == this)
                {
                    continue;
                }

                Vector2 offset = body.position -
                    (Vector2)candidate.transform.position;
                float distance = Mathf.Max(0.08f, offset.magnitude);
                separation += offset / distance *
                    (1f - Mathf.Clamp01(distance / separationRadius));
                neighbours++;
            }

            if (neighbours == 0)
            {
                return Vector2.zero;
            }

            return separation / neighbours * separationStrength;
        }

        private Vector2 ApplyMobilityStyle(Vector2 velocity)
        {
            float speed = velocity.magnitude;
            if (speed < 0.001f)
            {
                return velocity;
            }

            Vector2 direction = velocity / speed;
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            float phase = Time.time + weaveSeed;
            float weave = 0f;
            float pulse = 1f;
            switch (MobilityStyle)
            {
                case EnemyMobilityStyle.Hopper:
                    pulse = 0.94f + Mathf.Sin(phase * 7.5f) * 0.06f;
                    break;
                case EnemyMobilityStyle.Swarm:
                    weave = Mathf.Sin(phase * 5.4f) * 0.13f;
                    break;
                case EnemyMobilityStyle.Skirmisher:
                    weave = Mathf.Sin(phase * 3.8f) * 0.09f;
                    break;
                case EnemyMobilityStyle.Hover:
                    weave = Mathf.Sin(phase * 2.4f) * 0.06f;
                    pulse = 0.98f + Mathf.Sin(phase * 3.1f) * 0.02f;
                    break;
                case EnemyMobilityStyle.Skitter:
                    weave = Mathf.Sin(phase * 8.5f) * 0.11f;
                    break;
                case EnemyMobilityStyle.Rift:
                    weave = Mathf.Sin(phase * 4.6f) * 0.07f;
                    pulse = 0.96f + Mathf.Sin(phase * 6.2f) * 0.04f;
                    break;
            }

            return (direction + tangent * weave).normalized * speed * pulse;
        }

        private IEnumerator SlowRoutine(float multiplier, float duration)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            speedMultiplier = 1f;
        }
    }
}
