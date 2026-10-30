using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TopDownGame.Core;

namespace TopDownGame.Player
{
    /// <summary>
    /// Drives responsive top-down locomotion with the cast-and-slide approach
    /// from Unity Technologies' official PhysicsExamples2D controller.
    /// Input intent and observed physical motion stay separate so collisions do
    /// not make the character animate as if it were still crossing open ground.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private enum MovementAxis
        {
            None,
            Horizontal,
            Vertical
        }

        private readonly struct GridPoint : System.IEquatable<GridPoint>
        {
            public GridPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            public bool Equals(GridPoint other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is GridPoint other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Y;
                }
            }
        }

        private readonly struct PathState : System.IEquatable<PathState>
        {
            public PathState(GridPoint point, MovementAxis axis)
            {
                Point = point;
                Axis = axis;
            }

            public GridPoint Point { get; }
            public MovementAxis Axis { get; }

            public bool Equals(PathState other)
            {
                return Point.Equals(other.Point) && Axis == other.Axis;
            }

            public override bool Equals(object obj)
            {
                return obj is PathState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Point.GetHashCode() * 397) ^ (int)Axis;
                }
            }
        }

        private readonly struct DirectedGridEdge :
            System.IEquatable<DirectedGridEdge>
        {
            public DirectedGridEdge(GridPoint from, GridPoint to)
            {
                From = from;
                To = to;
            }

            public GridPoint From { get; }
            public GridPoint To { get; }

            public bool Equals(DirectedGridEdge other)
            {
                return From.Equals(other.From) && To.Equals(other.To);
            }

            public override bool Equals(object obj)
            {
                return obj is DirectedGridEdge other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (From.GetHashCode() * 397) ^ To.GetHashCode();
                }
            }
        }

        private readonly struct OpenPathNode
        {
            public OpenPathNode(PathState state, float score, float heuristic)
            {
                State = state;
                Score = score;
                Heuristic = heuristic;
            }

            public PathState State { get; }
            public float Score { get; }
            public float Heuristic { get; }
        }

        // At the default 3 units/second this produces 1.2 gait cycles (2.4
        // steps) per second: 144 steps/minute. The slightly longer frame
        // exposure makes the opposing arm swing readable without foot sliding.
        public const float DefaultGaitCyclesPerUnit = 0.40f;
        public const float DefaultContactOffset = 0.05f;
        public const int DefaultMaxSlideIterations = 2;
        public const float DefaultImpulseDamping = 18f;
        public const float DefaultClickPathGridSize = 0.5f;
        public const float DefaultClickPathSearchPadding = 16f;
        public const int DefaultMaxPathSearchNodes = 20000;
        public const float DefaultClickPathTurnPenalty = 0.08f;
        public const float DefaultClickGoalSnapDistance = 1.5f;
        private const float MovementEpsilon = 0.005f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField] private bool smoothLocomotion;
        [SerializeField, Min(1f)] private float acceleration = 60f;
        [SerializeField, Min(1f)] private float deceleration = 90f;
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private bool clickToMove = true;
        [SerializeField, Min(0.001f)] private float clickStopDistance =
            MovementEpsilon;

        [Header("Cardinal Click Pathfinding")]
        [SerializeField, Min(0.25f)] private float clickPathGridSize =
            DefaultClickPathGridSize;
        [SerializeField, Min(1f)] private float clickPathSearchPadding =
            DefaultClickPathSearchPadding;
        [SerializeField, Range(256, 30000)] private int maxPathSearchNodes =
            DefaultMaxPathSearchNodes;
        [SerializeField, Range(0f, 0.5f)] private float clickPathTurnPenalty =
            DefaultClickPathTurnPenalty;
        [SerializeField, Min(0.5f)] private float clickGoalSnapDistance =
            DefaultClickGoalSnapDistance;

        [Header("Collision Sliding")]
        [SerializeField, Min(0.001f)] private float contactOffset =
            DefaultContactOffset;
        [SerializeField, Range(1, 8)] private int maxSlideIterations =
            DefaultMaxSlideIterations;
        [SerializeField] private LayerMask movementLayers = ~0;

        [Header("Combat Response")]
        [SerializeField] private PlayerDodge dodge;
        [SerializeField, Min(0.1f)] private float impulseDamping =
            DefaultImpulseDamping;

        [Header("Footsteps")]
        [SerializeField, Range(0.1f, 1.2f)] private float gaitCyclesPerUnit =
            DefaultGaitCyclesPerUnit;

        private Rigidbody2D body;
        private PlayerInput playerInput;
        private PlayerStats stats;
        private InputAction moveAction;
        private ContactFilter2D movementFilter;
        private readonly List<RaycastHit2D> movementHits =
            new List<RaycastHit2D>(1);
        private readonly List<RaycastHit2D> pathHits =
            new List<RaycastHit2D>(8);
        private readonly List<Vector2> clickWaypoints = new List<Vector2>(16);
        private readonly Dictionary<DirectedGridEdge, bool>
            gridTraversalCache =
                new Dictionary<DirectedGridEdge, bool>(512);
        private Vector2 moveDirection;
        private Vector2 currentVelocity;
        private Vector2 impulseVelocity;
        private Vector2 observedVelocity;
        private Vector2 clickDestination;
        private Vector2 requestedClickDestination;
        private bool hasClickDestination;
        private bool clickPathNeedsRebuild;
        private int blockedClickPhysicsSteps;
        private int clickWaypointIndex;
        private MovementAxis clickAxis;
        private MovementAxis inputAxis;
        private Vector2 previousRawInput;
        private Camera mainCamera;
        private Transform visualTransform;
        private SpriteRenderer visualRenderer;
        private Vector3 visualRestPosition;
        private Vector3 visualRestScale;
        private bool useLeftFoot;
        private float gaitPhase;
        private float gaitStepStart;
        private float gaitStepAdvance;
        private float movementLockedUntil;
        private float locomotionSpeed;
        private Vector2 lastLocomotionDirection;

        public Vector2 MoveDirection => moveDirection;
        public Vector2 CurrentVelocity => observedVelocity;
        public float NormalizedSpeed => Mathf.Clamp01(observedVelocity.magnitude /
            Mathf.Max(0.1f, stats != null ? stats.MoveSpeed : moveSpeed));
        public bool IsMoving => moveDirection.sqrMagnitude > 0.001f;
        public bool IsFollowingClickPath => hasClickDestination;
        public bool WantsToMove
        {
            get
            {
                if (hasClickDestination || moveDirection.sqrMagnitude > 0.001f)
                {
                    return true;
                }

                if (GestureInputState.Movement.sqrMagnitude > 0.001f) return true;

                Vector2 rawInput = moveAction != null
                    ? moveAction.ReadValue<Vector2>()
                    : Vector2.zero;
                if (rawInput.sqrMagnitude <= 0.001f)
                {
                    rawInput = ReadKeyboardDirection();
                }
                return rawInput.sqrMagnitude > 0.001f;
            }
        }
        public SpriteRenderer VisualRenderer => visualRenderer;
        public int EffectiveMovementMask => movementFilter.layerMask;
        public float GaitCyclesPerUnit => gaitCyclesPerUnit;
        public float GaitPhase
        {
            get
            {
                // Match the same interpolation interval used by Rigidbody2D.
                // The previous implementation sampled a completed physics step
                // and then extrapolated it into the next one. Turns, stops and
                // wall contacts could therefore put the feet one tick ahead of
                // the body and read as a small hitch.
                float alpha = Time.inFixedTimeStep || Time.fixedDeltaTime <= 0f
                    ? 1f
                    : Mathf.Clamp01((Time.time - Time.fixedTime) /
                        Time.fixedDeltaTime);
                return Mathf.Repeat(gaitStepStart + gaitStepAdvance * alpha, 1f);
            }
        }
        public bool IsMovementLocked => Time.time < movementLockedUntil;

        /// <summary>Preserves eight-way intent for actions while normal walking remains cardinal.</summary>
        public Vector2 ReadInputDirection()
        {
            Vector2 raw = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (raw.sqrMagnitude <= 0.001f) raw = ReadKeyboardDirection();
            if (raw.sqrMagnitude <= 0.001f && GestureInputState.IsActive) raw = GestureInputState.Movement;
            return Vector2.ClampMagnitude(raw, 1f);
        }

        private void Awake()
        {
            GameplayCollisionRules.ConfigurePlayer(gameObject);
            body = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            stats = GetComponent<PlayerStats>();
            if (dodge == null) dodge = GetComponent<PlayerDodge>();
            ConfigureKinematicBody();
            RebuildMovementFilter();
            PrepareMovementVisual();
            ResetMotionSample();
            CacheMoveAction();
        }

        private void OnEnable()
        {
            ConfigureKinematicBody();
            RebuildMovementFilter();
            ResetMotionSample();
            CacheMoveAction();
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            locomotionSpeed = 0f;
            moveDirection = Vector2.zero;
            currentVelocity = Vector2.zero;
            impulseVelocity = Vector2.zero;
            observedVelocity = Vector2.zero;
            CancelClickMovement();
            inputAxis = MovementAxis.None;
            previousRawInput = Vector2.zero;
            gaitStepStart = gaitPhase;
            gaitStepAdvance = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void Update()
        {
            EnsureInputReady();

            if (GameplayState.IsInputBlocked || Time.timeScale <= 0f)
            {
                moveDirection = Vector2.zero;
                currentVelocity = observedVelocity = Vector2.zero;
                locomotionSpeed = 0f;
                gaitStepAdvance = 0f;
                CancelClickMovement();
                return;
            }

            if (IsMovementLocked)
            {
                moveDirection = Vector2.zero;
                CancelClickMovement();
                inputAxis = MovementAxis.None;
                previousRawInput = Vector2.zero;
                UpdateMovementVisual();
                return;
            }

            Vector2 rawInput = moveAction != null
                ? moveAction.ReadValue<Vector2>()
                : Vector2.zero;
            if (rawInput.sqrMagnitude <= 0.001f)
            {
                rawInput = ReadKeyboardDirection();
            }
            if (GestureInputState.IsActive)
            {
                CancelClickMovement();
                if (rawInput.sqrMagnitude <= 0.001f)
                    rawInput = GestureInputState.Movement;
            }
            moveDirection = ResolveCardinalInput(rawInput);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                CancelClickMovement();
            }
            else if (clickToMove && Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }

                if (mainCamera != null)
                {
                    BeginClickMovement(mainCamera.ScreenToWorldPoint(
                        Mouse.current.position.ReadValue()));
                }
            }

            if (moveDirection.sqrMagnitude <= 0.001f &&
                hasClickDestination && clickPathNeedsRebuild)
            {
                Vector2 destinationToRebuild = requestedClickDestination;
                clickPathNeedsRebuild = false;
                TrySetClickDestination(destinationToRebuild);
            }

            if (moveDirection.sqrMagnitude <= 0.001f && hasClickDestination)
            {
                moveDirection = ResolveClickDirection(body.position);
            }

            UpdateMovementVisual();
        }

        public void LockMovement(float duration)
        {
            locomotionSpeed = 0f;
            movementLockedUntil = Mathf.Max(movementLockedUntil,
                Time.time + Mathf.Max(0f, duration));
            moveDirection = Vector2.zero;
            currentVelocity = Vector2.zero;
            impulseVelocity = Vector2.zero;
            observedVelocity = Vector2.zero;
            CancelClickMovement();
            inputAxis = MovementAxis.None;
            previousRawInput = Vector2.zero;
            gaitStepStart = gaitPhase;
            gaitStepAdvance = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void ReleaseMovementLock()
        {
            movementLockedUntil = Time.time;
        }

        public void CancelMovement()
        {
            locomotionSpeed = 0f;
            gaitStepStart = gaitPhase;
            gaitStepAdvance = 0f;
            ReleaseMovementLock();
            moveDirection = Vector2.zero;
            currentVelocity = Vector2.zero;
            observedVelocity = Vector2.zero;
            CancelClickMovement();
            inputAxis = MovementAxis.None;
            previousRawInput = Vector2.zero;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void AddImpulse(Vector2 impulse)
        {
            if (body == null || impulse.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            impulseVelocity += impulse / Mathf.Max(0.1f, body.mass);
            // Taking a hit must not erase the player's click command. The
            // knockback is resolved alongside locomotion, then the existing
            // cardinal route naturally brings the player back to the requested
            // destination. Cancelling here made a monster attack feel like the
            // monster had physically trapped the player.
        }

        /// <summary>
        /// Moves an external action such as a dash through the same collision
        /// solver as regular locomotion. Call from the fixed-timestep loop.
        /// Returns the collision-resolved target position.
        /// </summary>
        public Vector2 MoveKinematically(Vector2 displacement)
        {
            if (body == null)
            {
                return transform.position;
            }

            Vector2 startPosition = body.position;
            Vector2 targetPosition = ResolveSurfaceSliding(startPosition,
                displacement);
            body.MovePosition(targetPosition);
            float effectiveSpeed = stats != null ? stats.MoveSpeed : moveSpeed;
            SampleResolvedMotion(startPosition, targetPosition - startPosition,
                effectiveSpeed);
            return targetPosition;
        }

        private void FixedUpdate()
        {
            if (dodge != null && dodge.IsDodging && !GameplayState.IsInputBlocked && Time.timeScale > 0f)
            {
                locomotionSpeed = 0f;
                currentVelocity = impulseVelocity = Vector2.zero;
                MoveKinematically(dodge.Direction * dodge.Speed * Time.fixedDeltaTime);
                return;
            }
            float effectiveSpeed = stats != null ? stats.MoveSpeed : moveSpeed;
            Vector2 previousPosition = body.position;

            if (IsMovementLocked || GameplayState.IsInputBlocked)
            {
                locomotionSpeed = 0f;
                currentVelocity = Vector2.zero;
            }
            else if (smoothLocomotion)
            {
                bool hasIntent = moveDirection.sqrMagnitude > 0.001f;
                // Smooth only speed. Keep one axis and retain speed through
                // click-path corners rather than braking at every waypoint.
                locomotionSpeed = Mathf.MoveTowards(locomotionSpeed,
                    hasIntent ? effectiveSpeed : 0f,
                    (hasIntent ? acceleration : deceleration) * Time.fixedDeltaTime);
                if (hasIntent) lastLocomotionDirection = moveDirection;
                currentVelocity = lastLocomotionDirection * locomotionSpeed;
            }
            else currentVelocity = moveDirection * effectiveSpeed;
            impulseVelocity = Vector2.MoveTowards(impulseVelocity, Vector2.zero,
                impulseDamping * Time.fixedDeltaTime);
            Vector2 locomotionDisplacement = currentVelocity * Time.fixedDeltaTime;
            bool reachesClickAxisThisStep = false;
            if (hasClickDestination && moveDirection.sqrMagnitude > 0.001f)
            {
                // Each click leg owns one axis until it reaches the target
                // coordinate. Clamp the final physics step exactly onto that
                // coordinate, then turn once by 90 degrees for the second leg.
                float remainingDistance = clickAxis == MovementAxis.Horizontal
                    ? Mathf.Abs(clickDestination.x - previousPosition.x)
                    : Mathf.Abs(clickDestination.y - previousPosition.y);
                // Treat a sub-tolerance remainder as part of this physics step.
                // Otherwise float error can leave a microscopic follow-up step
                // at a waypoint, which reads as a one-frame hitch before the
                // next cardinal leg begins.
                float arrivalTolerance = Mathf.Min(clickStopDistance,
                    MovementEpsilon);
                if (locomotionDisplacement.magnitude >=
                    Mathf.Max(0f, remainingDistance - arrivalTolerance))
                {
                    locomotionDisplacement = moveDirection * remainingDistance;
                    currentVelocity = Time.fixedDeltaTime > 0f
                        ? locomotionDisplacement / Time.fixedDeltaTime
                        : Vector2.zero;
                    reachesClickAxisThisStep = true;
                }
            }

            // Walking is resolved on exactly one axis. In particular, a round or
            // angled collider must stop that axis instead of projecting the
            // character onto its tangent and secretly creating diagonal travel.
            Vector2 targetPosition = ResolveAxisLockedMovement(previousPosition,
                locomotionDisplacement);
            targetPosition = ResolveSurfaceSliding(targetPosition,
                impulseVelocity * Time.fixedDeltaTime);
            body.MovePosition(targetPosition);
            Vector2 resolvedDisplacement = targetPosition - previousPosition;
            if (hasClickDestination &&
                locomotionDisplacement.sqrMagnitude > 0.000001f &&
                resolvedDisplacement.sqrMagnitude <= 0.00000001f &&
                impulseVelocity.sqrMagnitude <= 0.0001f)
            {
                blockedClickPhysicsSteps++;
                if (blockedClickPhysicsSteps >= 4)
                {
                    clickPathNeedsRebuild = true;
                }
            }
            else
            {
                blockedClickPhysicsSteps = 0;
            }

            SampleResolvedMotion(previousPosition,
                resolvedDisplacement, effectiveSpeed);
            if (smoothLocomotion && locomotionDisplacement.sqrMagnitude > 0.000001f &&
                resolvedDisplacement.sqrMagnitude < locomotionDisplacement.sqrMagnitude * 0.01f)
                locomotionSpeed = 0f;
            if (reachesClickAxisThisStep)
            {
                AdvanceClickAxis(targetPosition);
                // Resolve the next leg immediately. Waiting for Update here
                // inserts an empty physics tick when Unity is catching up with
                // more than one FixedUpdate in a rendered frame, which reads as
                // a hitch at the corner of an otherwise clean L-shaped path.
                moveDirection = hasClickDestination
                    ? ResolveClickDirection(targetPosition)
                    : Vector2.zero;
                // Arrival is exact. Do not let keyboard-style braking drift
                // beyond the final click destination.
                if (!hasClickDestination)
                {
                    locomotionSpeed = 0f;
                    currentVelocity = Vector2.zero;
                }
            }
        }

        private Vector2 ResolveCardinalInput(Vector2 rawInput)
        {
            const float InputEpsilon = 0.001f;
            const float AnalogSwitchMargin = 0.1f;
            float horizontal = Mathf.Abs(rawInput.x) > InputEpsilon
                ? rawInput.x
                : 0f;
            float vertical = Mathf.Abs(rawInput.y) > InputEpsilon
                ? rawInput.y
                : 0f;
            bool hasHorizontal = horizontal != 0f;
            bool hasVertical = vertical != 0f;

            if (!hasHorizontal && !hasVertical)
            {
                inputAxis = MovementAxis.None;
                previousRawInput = Vector2.zero;
                return Vector2.zero;
            }

            if (hasHorizontal && !hasVertical)
            {
                inputAxis = MovementAxis.Horizontal;
            }
            else if (hasVertical && !hasHorizontal)
            {
                inputAxis = MovementAxis.Vertical;
            }
            else
            {
                bool horizontalPressedNow =
                    Mathf.Abs(previousRawInput.x) <= InputEpsilon;
                bool verticalPressedNow =
                    Mathf.Abs(previousRawInput.y) <= InputEpsilon;
                if (horizontalPressedNow != verticalPressedNow)
                {
                    // For digital controls, the most recently pressed axis wins.
                    inputAxis = horizontalPressedNow
                        ? MovementAxis.Horizontal
                        : MovementAxis.Vertical;
                }
                else if (Mathf.Abs(horizontal) >
                    Mathf.Abs(vertical) + AnalogSwitchMargin)
                {
                    inputAxis = MovementAxis.Horizontal;
                }
                else if (Mathf.Abs(vertical) >
                    Mathf.Abs(horizontal) + AnalogSwitchMargin)
                {
                    inputAxis = MovementAxis.Vertical;
                }
                else if (inputAxis == MovementAxis.None)
                {
                    inputAxis = MovementAxis.Horizontal;
                }
            }

            previousRawInput = rawInput;
            return inputAxis == MovementAxis.Horizontal
                ? new Vector2(Mathf.Sign(horizontal), 0f)
                : new Vector2(0f, Mathf.Sign(vertical));
        }

        private void BeginClickMovement(Vector2 destination)
        {
            TrySetClickDestination(destination);
        }

        /// <summary>
        /// Builds a collision-aware route made exclusively from horizontal and
        /// vertical legs. Open ground keeps the familiar single-corner path;
        /// walls automatically promote the click to a four-neighbour A* route.
        /// </summary>
        public bool TrySetClickDestination(Vector2 worldDestination)
        {
            if (body == null)
            {
                return false;
            }

            requestedClickDestination = worldDestination;
            clickPathNeedsRebuild = false;
            blockedClickPhysicsSteps = 0;
            Physics2D.SyncTransforms();
            Vector2 start = body.position;
            List<Vector2> route = new List<Vector2>(16);
            bool foundRoute = TryBuildDirectClickRoute(start, worldDestination,
                route) || TryBuildGridClickRoute(start, worldDestination, route);
            if (!foundRoute)
            {
                CancelClickMovement();
                moveDirection = Vector2.zero;
                return false;
            }

            clickWaypoints.Clear();
            for (int i = 0; i < route.Count; i++)
            {
                AppendCardinalWaypoint(clickWaypoints, start, route[i]);
            }

            if (clickWaypoints.Count == 0)
            {
                CancelClickMovement();
                moveDirection = Vector2.zero;
                return true;
            }

            clickWaypointIndex = 0;
            clickDestination = clickWaypoints[clickWaypointIndex];
            clickAxis = GetMovementAxis(clickDestination - start);
            hasClickDestination = clickAxis != MovementAxis.None;
            moveDirection = hasClickDestination
                ? ResolveClickDirection(start)
                : Vector2.zero;
            return hasClickDestination;
        }

        private bool TryBuildDirectClickRoute(Vector2 start, Vector2 destination,
            List<Vector2> route)
        {
            route.Clear();
            Vector2 remaining = destination - start;
            bool needsHorizontal = Mathf.Abs(remaining.x) > MovementEpsilon;
            bool needsVertical = Mathf.Abs(remaining.y) > MovementEpsilon;
            if (!needsHorizontal && !needsVertical)
            {
                return true;
            }

            if (!needsHorizontal || !needsVertical)
            {
                if (!CanTraverseCardinal(start, destination))
                {
                    return false;
                }

                AppendCardinalWaypoint(route, start, destination);
                return true;
            }

            MovementAxis preferredAxis = ChooseFirstClickAxis(remaining);
            Vector2 preferredCorner = preferredAxis == MovementAxis.Horizontal
                ? new Vector2(destination.x, start.y)
                : new Vector2(start.x, destination.y);
            if (CanTraverseCardinal(start, preferredCorner) &&
                CanTraverseCardinal(preferredCorner, destination))
            {
                AppendCardinalWaypoint(route, start, preferredCorner);
                AppendCardinalWaypoint(route, start, destination);
                return true;
            }

            Vector2 alternateCorner = preferredAxis == MovementAxis.Horizontal
                ? new Vector2(start.x, destination.y)
                : new Vector2(destination.x, start.y);
            if (!CanTraverseCardinal(start, alternateCorner) ||
                !CanTraverseCardinal(alternateCorner, destination))
            {
                route.Clear();
                return false;
            }

            AppendCardinalWaypoint(route, start, alternateCorner);
            AppendCardinalWaypoint(route, start, destination);
            return true;
        }

        private bool TryBuildGridClickRoute(Vector2 start, Vector2 destination,
            List<Vector2> route)
        {
            route.Clear();
            // The physics world is stable for this synchronous search, so an
            // edge only needs one shape cast. Clear between searches so a
            // moved dynamic collider is observed by the next click or repath.
            gridTraversalCache.Clear();
            float cellSize = Mathf.Max(0.25f, clickPathGridSize);
            int targetX = Mathf.RoundToInt((destination.x - start.x) / cellSize);
            int targetY = Mathf.RoundToInt((destination.y - start.y) / cellSize);
            int connectorRadius = 2;
            Dictionary<GridPoint, List<Vector2>> goalTails =
                new Dictionary<GridPoint, List<Vector2>>();

            // A regular floor click retains its exact world coordinate. Several
            // nearby grid cells are accepted as connectors so a rounded cell
            // landing beside a wall does not make an otherwise valid goal fail.
            for (int x = targetX - connectorRadius;
                x <= targetX + connectorRadius; x++)
            {
                for (int y = targetY - connectorRadius;
                    y <= targetY + connectorRadius; y++)
                {
                    GridPoint point = new GridPoint(x, y);
                    Vector2 worldPoint = GridToWorld(start, point, cellSize);
                    List<Vector2> tail = new List<Vector2>(2);
                    if (CanOccupyClickPathPoint(worldPoint) &&
                        TryBuildDirectClickRoute(worldPoint, destination, tail))
                    {
                        goalTails[point] = tail;
                    }
                }
            }

            // A chest or solid prop may own the clicked coordinate. In that
            // case route to the nearest reachable floor within interaction
            // range instead of rejecting the command or pushing into it.
            if (goalTails.Count == 0)
            {
                int snapRadius = Mathf.Max(1,
                    Mathf.CeilToInt(clickGoalSnapDistance / cellSize));
                float maximumSnap = Mathf.Max(cellSize,
                    clickGoalSnapDistance - contactOffset);
                for (int x = targetX - snapRadius;
                    x <= targetX + snapRadius; x++)
                {
                    for (int y = targetY - snapRadius;
                        y <= targetY + snapRadius; y++)
                    {
                        GridPoint point = new GridPoint(x, y);
                        Vector2 worldPoint = GridToWorld(start, point, cellSize);
                        if (Vector2.Distance(worldPoint, destination) <= maximumSnap &&
                            CanOccupyClickPathPoint(worldPoint))
                        {
                            goalTails[point] = new List<Vector2>(0);
                        }
                    }
                }
            }

            if (goalTails.Count == 0)
            {
                return false;
            }

            int padding = Mathf.Max(2,
                Mathf.CeilToInt(clickPathSearchPadding / cellSize));
            int minX = Mathf.Min(0, targetX) - padding;
            int maxX = Mathf.Max(0, targetX) + padding;
            int minY = Mathf.Min(0, targetY) - padding;
            int maxY = Mathf.Max(0, targetY) + padding;

            PathState startState = new PathState(new GridPoint(0, 0),
                MovementAxis.None);
            int goalRadius = 0;
            foreach (GridPoint goal in goalTails.Keys)
            {
                goalRadius = Mathf.Max(goalRadius,
                    Mathf.Abs(goal.X - targetX), Mathf.Abs(goal.Y - targetY));
            }

            float startHeuristic = GridHeuristic(startState.Point, targetX,
                targetY, goalRadius);
            List<OpenPathNode> open = new List<OpenPathNode>();
            PushOpenPathNode(open, new OpenPathNode(startState,
                startHeuristic, startHeuristic));
            HashSet<PathState> closed = new HashSet<PathState>();
            Dictionary<PathState, float> costs =
                new Dictionary<PathState, float> { [startState] = 0f };
            Dictionary<PathState, PathState> cameFrom =
                new Dictionary<PathState, PathState>();
            int expandedNodes = 0;

            int nodeBudget = Mathf.Max(256, maxPathSearchNodes);
            while (open.Count > 0 && expandedNodes < nodeBudget)
            {
                OpenPathNode openNode = PopOpenPathNode(open);
                PathState current = openNode.State;
                if (!closed.Add(current))
                {
                    continue;
                }

                expandedNodes++;
                if (goalTails.TryGetValue(current.Point,
                    out List<Vector2> goalTail))
                {
                    BuildCompressedGridRoute(start, cellSize, startState,
                        current, cameFrom, goalTail, route);
                    return true;
                }

                ExpandGridNeighbour(current, 1, 0, MovementAxis.Horizontal,
                    start, cellSize, minX, maxX, minY, maxY, targetX,
                    targetY, goalRadius, costs, cameFrom, open, closed);
                ExpandGridNeighbour(current, -1, 0, MovementAxis.Horizontal,
                    start, cellSize, minX, maxX, minY, maxY, targetX,
                    targetY, goalRadius, costs, cameFrom, open, closed);
                ExpandGridNeighbour(current, 0, 1, MovementAxis.Vertical,
                    start, cellSize, minX, maxX, minY, maxY, targetX,
                    targetY, goalRadius, costs, cameFrom, open, closed);
                ExpandGridNeighbour(current, 0, -1, MovementAxis.Vertical,
                    start, cellSize, minX, maxX, minY, maxY, targetX,
                    targetY, goalRadius, costs, cameFrom, open, closed);
            }

            return false;
        }

        private void ExpandGridNeighbour(PathState current, int offsetX,
            int offsetY, MovementAxis nextAxis, Vector2 gridOrigin,
            float cellSize, int minX, int maxX, int minY, int maxY,
            int targetX, int targetY, int goalRadius,
            Dictionary<PathState, float> costs,
            Dictionary<PathState, PathState> cameFrom,
            List<OpenPathNode> open, HashSet<PathState> closed)
        {
            GridPoint nextPoint = new GridPoint(current.Point.X + offsetX,
                current.Point.Y + offsetY);
            if (nextPoint.X < minX || nextPoint.X > maxX ||
                nextPoint.Y < minY || nextPoint.Y > maxY)
            {
                return;
            }

            PathState next = new PathState(nextPoint, nextAxis);
            if (closed.Contains(next))
            {
                return;
            }

            float turnCost = current.Axis != MovementAxis.None &&
                current.Axis != nextAxis ? clickPathTurnPenalty : 0f;
            float tentativeCost = costs[current] + 1f + turnCost;
            if (costs.TryGetValue(next, out float knownCost) &&
                tentativeCost >= knownCost - 0.0001f)
            {
                return;
            }

            DirectedGridEdge edge = new DirectedGridEdge(current.Point,
                nextPoint);
            if (!gridTraversalCache.TryGetValue(edge, out bool traversable))
            {
                Vector2 currentWorld = GridToWorld(gridOrigin, current.Point,
                    cellSize);
                Vector2 nextWorld = GridToWorld(gridOrigin, nextPoint, cellSize);
                traversable = CanTraverseCardinal(currentWorld, nextWorld);
                gridTraversalCache.Add(edge, traversable);
            }

            if (!traversable)
            {
                return;
            }

            costs[next] = tentativeCost;
            cameFrom[next] = current;
            float heuristic = GridHeuristic(nextPoint, targetX, targetY,
                goalRadius);
            PushOpenPathNode(open, new OpenPathNode(next,
                tentativeCost + heuristic, heuristic));
        }

        private static float GridHeuristic(GridPoint point, int targetX,
            int targetY, int goalRadius)
        {
            return Mathf.Max(0, Mathf.Abs(targetX - point.X) - goalRadius) +
                Mathf.Max(0, Mathf.Abs(targetY - point.Y) - goalRadius);
        }

        private static void PushOpenPathNode(List<OpenPathNode> heap,
            OpenPathNode node)
        {
            heap.Add(node);
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!OpenPathNodePrecedes(node, heap[parent]))
                {
                    break;
                }

                heap[index] = heap[parent];
                index = parent;
            }

            heap[index] = node;
        }

        private static OpenPathNode PopOpenPathNode(List<OpenPathNode> heap)
        {
            OpenPathNode result = heap[0];
            int lastIndex = heap.Count - 1;
            OpenPathNode replacement = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            if (heap.Count == 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= heap.Count)
                {
                    break;
                }

                int right = left + 1;
                int child = right < heap.Count &&
                    OpenPathNodePrecedes(heap[right], heap[left])
                    ? right
                    : left;
                if (!OpenPathNodePrecedes(heap[child], replacement))
                {
                    break;
                }

                heap[index] = heap[child];
                index = child;
            }

            heap[index] = replacement;
            return result;
        }

        private static bool OpenPathNodePrecedes(OpenPathNode left,
            OpenPathNode right)
        {
            return left.Score < right.Score - 0.0001f ||
                (Mathf.Abs(left.Score - right.Score) <= 0.0001f &&
                 left.Heuristic < right.Heuristic);
        }

        private static Vector2 GridToWorld(Vector2 origin, GridPoint point,
            float cellSize)
        {
            return origin + new Vector2(point.X * cellSize,
                point.Y * cellSize);
        }

        private static void BuildCompressedGridRoute(Vector2 start,
            float cellSize, PathState startState, PathState goalState,
            Dictionary<PathState, PathState> cameFrom, List<Vector2> goalTail,
            List<Vector2> route)
        {
            List<PathState> reversed = new List<PathState> { goalState };
            PathState cursor = goalState;
            while (!cursor.Equals(startState) &&
                cameFrom.TryGetValue(cursor, out PathState previous))
            {
                cursor = previous;
                reversed.Add(cursor);
            }

            reversed.Reverse();
            Vector2 previousWorld = start;
            MovementAxis previousAxis = MovementAxis.None;
            for (int i = 1; i < reversed.Count; i++)
            {
                Vector2 worldPoint = GridToWorld(start,
                    reversed[i].Point, cellSize);
                MovementAxis axis = GetMovementAxis(worldPoint - previousWorld);
                if (previousAxis != MovementAxis.None && axis != previousAxis)
                {
                    AppendCardinalWaypoint(route, start, previousWorld);
                }

                previousAxis = axis;
                previousWorld = worldPoint;
            }

            AppendCardinalWaypoint(route, start, previousWorld);
            for (int i = 0; i < goalTail.Count; i++)
            {
                AppendCardinalWaypoint(route, start, goalTail[i]);
            }
        }

        private bool CanTraverseCardinal(Vector2 start, Vector2 end)
        {
            Vector2 displacement = end - start;
            if (Mathf.Abs(displacement.x) > MovementEpsilon &&
                Mathf.Abs(displacement.y) > MovementEpsilon)
            {
                return false;
            }

            float distance = displacement.magnitude;
            if (distance <= MovementEpsilon)
            {
                return true;
            }

            pathHits.Clear();
            body.Cast(start, body.rotation, displacement / distance,
                movementFilter, pathHits, distance);
            for (int i = 0; i < pathHits.Count; i++)
            {
                if (IsClickPathBlocker(pathHits[i].collider))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanOccupyClickPathPoint(Vector2 position)
        {
            return !ClickPathProbeIsBlocked(position, Vector2.right) &&
                !ClickPathProbeIsBlocked(position, Vector2.left) &&
                !ClickPathProbeIsBlocked(position, Vector2.up) &&
                !ClickPathProbeIsBlocked(position, Vector2.down);
        }

        private bool ClickPathProbeIsBlocked(Vector2 position,
            Vector2 direction)
        {
            const float OverlapProbeDistance = 0.01f;
            pathHits.Clear();
            body.Cast(position, body.rotation, direction, movementFilter,
                pathHits, OverlapProbeDistance);
            for (int i = 0; i < pathHits.Count; i++)
            {
                if (IsClickPathBlocker(pathHits[i].collider))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsClickPathBlocker(Collider2D collider)
        {
            return collider != null && !collider.isTrigger &&
                collider.attachedRigidbody != body;
        }

        private static void AppendCardinalWaypoint(List<Vector2> route,
            Vector2 routeStart, Vector2 waypoint)
        {
            Vector2 previous = route.Count > 0 ? route[route.Count - 1] : routeStart;
            if (Vector2.Distance(previous, waypoint) <= MovementEpsilon)
            {
                return;
            }

            if (route.Count > 0)
            {
                Vector2 segmentStart = route.Count > 1
                    ? route[route.Count - 2]
                    : routeStart;
                MovementAxis previousAxis = GetMovementAxis(previous - segmentStart);
                MovementAxis nextAxis = GetMovementAxis(waypoint - previous);
                if (previousAxis != MovementAxis.None && previousAxis == nextAxis)
                {
                    route[route.Count - 1] = waypoint;
                    return;
                }
            }

            route.Add(waypoint);
        }

        private static MovementAxis GetMovementAxis(Vector2 displacement)
        {
            if (Mathf.Abs(displacement.x) > MovementEpsilon)
            {
                return MovementAxis.Horizontal;
            }

            if (Mathf.Abs(displacement.y) > MovementEpsilon)
            {
                return MovementAxis.Vertical;
            }

            return MovementAxis.None;
        }

        private Vector2 ResolveClickDirection(Vector2 position)
        {
            Vector2 remaining = clickDestination - position;
            float arrivalTolerance = Mathf.Min(clickStopDistance,
                MovementEpsilon);
            bool needsHorizontal = Mathf.Abs(remaining.x) > arrivalTolerance;
            bool needsVertical = Mathf.Abs(remaining.y) > arrivalTolerance;
            if (!needsHorizontal && !needsVertical)
            {
                AdvanceClickAxis(position);
                if (!hasClickDestination)
                {
                    return Vector2.zero;
                }

                remaining = clickDestination - position;
                needsHorizontal = Mathf.Abs(remaining.x) > arrivalTolerance;
                needsVertical = Mathf.Abs(remaining.y) > arrivalTolerance;
            }

            if (clickAxis == MovementAxis.None ||
                (clickAxis == MovementAxis.Horizontal && !needsHorizontal) ||
                (clickAxis == MovementAxis.Vertical && !needsVertical))
            {
                clickAxis = needsHorizontal
                    ? MovementAxis.Horizontal
                    : MovementAxis.Vertical;
            }

            return clickAxis == MovementAxis.Horizontal
                ? new Vector2(Mathf.Sign(remaining.x), 0f)
                : new Vector2(0f, Mathf.Sign(remaining.y));
        }

        private void AdvanceClickAxis(Vector2 position)
        {
            Vector2 remaining = clickDestination - position;
            float arrivalTolerance = Mathf.Min(clickStopDistance,
                MovementEpsilon);
            bool needsHorizontal = Mathf.Abs(remaining.x) > arrivalTolerance;
            bool needsVertical = Mathf.Abs(remaining.y) > arrivalTolerance;
            if (needsHorizontal || needsVertical)
            {
                return;
            }

            clickWaypointIndex++;
            while (clickWaypointIndex < clickWaypoints.Count)
            {
                clickDestination = clickWaypoints[clickWaypointIndex];
                clickAxis = GetMovementAxis(clickDestination - position);
                if (clickAxis != MovementAxis.None)
                {
                    return;
                }

                clickWaypointIndex++;
            }

            CancelClickMovement();
        }

        private static MovementAxis ChooseFirstClickAxis(Vector2 remaining)
        {
            return Mathf.Abs(remaining.x) >= Mathf.Abs(remaining.y)
                ? MovementAxis.Horizontal
                : MovementAxis.Vertical;
        }

        private void CancelClickMovement()
        {
            hasClickDestination = false;
            clickAxis = MovementAxis.None;
            clickWaypointIndex = 0;
            clickPathNeedsRebuild = false;
            blockedClickPhysicsSteps = 0;
            clickWaypoints.Clear();
        }

        private Vector2 ResolveAxisLockedMovement(Vector2 startPosition,
            Vector2 displacement)
        {
            float horizontalDistance = Mathf.Abs(displacement.x);
            float verticalDistance = Mathf.Abs(displacement.y);
            if (horizontalDistance <= Mathf.Epsilon &&
                verticalDistance <= Mathf.Epsilon)
            {
                return startPosition;
            }

            // This is also a final safety net for movement requested by another
            // component: even malformed diagonal locomotion is reduced to one
            // cardinal axis before any physics query is made.
            bool horizontal = horizontalDistance >= verticalDistance;
            float requestedDistance = horizontal
                ? horizontalDistance
                : verticalDistance;
            Vector2 direction = horizontal
                ? new Vector2(Mathf.Sign(displacement.x), 0f)
                : new Vector2(0f, Mathf.Sign(displacement.y));

            movementHits.Clear();
            int hitCount = body.Cast(startPosition, body.rotation, direction,
                movementFilter, movementHits, requestedDistance);
            float allowedDistance = requestedDistance;
            if (hitCount > 0)
            {
                allowedDistance = Mathf.Max(0f,
                    movementHits[0].distance - contactOffset);
            }

            return startPosition + direction * allowedDistance;
        }

        /// <summary>
        /// External movement such as dash and knockback keeps surface sliding.
        /// Normal walking uses ResolveAxisLockedMovement so it stays cardinal.
        /// </summary>
        private Vector2 ResolveSurfaceSliding(Vector2 startPosition,
            Vector2 displacement)
        {
            float distanceRemaining = displacement.magnitude;
            if (distanceRemaining <= MovementEpsilon)
            {
                return startPosition;
            }

            Vector2 movementDirection = displacement / distanceRemaining;
            Vector2 targetPosition = startPosition;
            int iterationsRemaining = maxSlideIterations;

            while (iterationsRemaining-- > 0 &&
                distanceRemaining > MovementEpsilon &&
                movementDirection.sqrMagnitude > MovementEpsilon)
            {
                float distance = distanceRemaining;
                movementHits.Clear();
                int hitCount = body.Cast(targetPosition, body.rotation,
                    movementDirection, movementFilter, movementHits, distance);
                if (hitCount > 0)
                {
                    RaycastHit2D hit = movementHits[0];
                    if (hit.distance > contactOffset)
                    {
                        distance = hit.distance - contactOffset;
                        targetPosition += movementDirection * distance;
                    }
                    else
                    {
                        distance = 0f;
                    }

                    movementDirection -= hit.normal * Vector2.Dot(
                        movementDirection, hit.normal);
                }
                else
                {
                    targetPosition += movementDirection * distance;
                }

                distanceRemaining -= distance;
            }

            return targetPosition;
        }

        private void SampleResolvedMotion(Vector2 startPosition,
            Vector2 physicalDisplacement, float effectiveSpeed)
        {
            float movedDistance = physicalDisplacement.magnitude;
            float teleportThreshold = Mathf.Max(1f,
                effectiveSpeed * Time.fixedDeltaTime * 4f);
            gaitStepStart = gaitPhase;
            gaitStepAdvance = 0f;
            if (movedDistance > teleportThreshold)
            {
                observedVelocity = Vector2.zero;
                return;
            }

            observedVelocity = Time.fixedDeltaTime > 0f
                ? physicalDisplacement / Time.fixedDeltaTime
                : Vector2.zero;

            if (movedDistance > 0.0001f && observedVelocity.sqrMagnitude > 0.0025f)
            {
                // Drive the animation from the distance actually travelled so
                // feet do not slide when speed changes. Two foot plants happen
                // in every complete gait cycle.
                float previousGaitPhase = gaitPhase;
                gaitStepAdvance = movedDistance * gaitCyclesPerUnit;
                float unwrappedGaitPhase = previousGaitPhase + gaitStepAdvance;
                gaitPhase = Mathf.Repeat(unwrappedGaitPhase, 1f);
                int previousContact = Mathf.FloorToInt(previousGaitPhase * 2f);
                int currentContact = Mathf.FloorToInt(unwrappedGaitPhase * 2f);
                if (currentContact > previousContact)
                {
                    FootstepEffect.Spawn(startPosition + physicalDisplacement,
                        observedVelocity, useLeftFoot);
                    useLeftFoot = !useLeftFoot;
                }
            }
        }

        private void ResetMotionSample()
        {
            observedVelocity = Vector2.zero;
            gaitStepStart = gaitPhase;
            gaitStepAdvance = 0f;
        }

        private void ConfigureKinematicBody()
        {
            if (body == null)
            {
                return;
            }

            // KinematicTopDownController is designed around a kinematic body:
            // casts decide the valid target and physics applies MovePosition.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.useFullKinematicContacts = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void RebuildMovementFilter()
        {
            movementFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            // The builder serializes this mask correctly, but applying the
            // runtime rule again protects old prefabs and dynamically created
            // players. Shape casts do not reliably inherit the global layer
            // collision matrix on every Unity version, so explicitly remove
            // enemy bodies from navigation queries as well.
            int effectiveMask = movementLayers.value &
                GameplayCollisionRules.PlayerMovementMask;
            movementFilter.SetLayerMask(effectiveMask);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            RemoveOverlap(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            RemoveOverlap(collision);
        }

        private void RemoveOverlap(Collision2D collision)
        {
            if (collision.collider == null || collision.otherCollider == null ||
                movementFilter.IsFilteringLayerMask(collision.collider.gameObject))
            {
                return;
            }

            ColliderDistance2D colliderDistance = Physics2D.Distance(
                collision.otherCollider, collision.collider);
            if (colliderDistance.isOverlapped)
            {
                // Mirrors the official sample's recovery for spawn, teleport or
                // external movement that begins inside a blocking collider.
                body.position += colliderDistance.normal * colliderDistance.distance;
            }
        }

        private void PrepareMovementVisual()
        {
            Transform existing = transform.Find("MovementVisual");
            if (existing != null)
            {
                visualTransform = existing;
                visualRenderer = existing.GetComponent<SpriteRenderer>();
            }

            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (visualRenderer == null && rootRenderer != null)
            {
                GameObject visualObject = new GameObject("MovementVisual");
                visualObject.transform.SetParent(transform, false);
                visualTransform = visualObject.transform;
                visualRenderer = visualObject.AddComponent<SpriteRenderer>();
                visualRenderer.color = rootRenderer.color;
                visualRenderer.sharedMaterial = rootRenderer.sharedMaterial;
                visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
                visualRenderer.sortingOrder = rootRenderer.sortingOrder;
                visualRenderer.flipX = rootRenderer.flipX;
            }

            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            Sprite initialCustomSprite = ThanhGiongHeroController.GetInitialSavedHeroSprite();
            if (visualRenderer != null && initialCustomSprite != null)
            {
                visualRenderer.sprite = initialCustomSprite;
                visualRenderer.color = Color.white;
                visualRenderer.enabled = true;
            }

            if (visualTransform != null)
            {
                visualRestPosition = visualTransform.localPosition;
                visualRestScale = visualTransform.localScale;
            }
        }

        private void UpdateMovementVisual()
        {
            if (visualTransform == null || visualRenderer == null)
            {
                return;
            }

            // Sprite frames already contain the body motion. Keeping the visual
            // transform fixed prevents procedural bob, tilt and squash from
            // stacking on top of the artwork and making the player shake.
            visualTransform.localPosition = visualRestPosition;
            visualTransform.localScale = visualRestScale;
            visualTransform.localRotation = Quaternion.identity;

            // PlayerCharacterAnimator owns the facing direction. Writing flipX
            // here as well can overwrite its directional left/right frames,
            // depending on the scripts' Update order.
        }

        private void CacheMoveAction()
        {
            if (moveAction != null || playerInput == null || playerInput.actions == null)
            {
                return;
            }

            moveAction = playerInput.actions.FindAction(moveActionName, false);
            if (moveAction == null)
            {
                Debug.LogWarning($"Input Action '{moveActionName}' was not found. " +
                    "Using direct keyboard input instead.", this);
            }
        }

        private void EnsureInputReady()
        {
            if (playerInput == null || !playerInput.enabled)
            {
                return;
            }

            if (!playerInput.inputIsActive)
            {
                playerInput.ActivateInput();
            }

            CacheMoveAction();
            if (moveAction != null && !moveAction.enabled)
            {
                moveAction.Enable();
            }
        }

        private static Vector2 ReadKeyboardDirection()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }
    }
}
