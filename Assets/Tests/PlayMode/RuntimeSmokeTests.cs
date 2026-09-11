using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class RuntimeSmokeTests
    {
        private const string TestSaveKey = "TopDownGame.Tests.Progress.v1";
        private const string TestGoldGrantKey =
            "TopDownGame.Tests.Progress.GoldGrant.v1";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // A failed coroutine stops before its local Destroy calls. Remove
            // every probe left by the previous test so colliders and movement
            // components cannot leak into the next test's physics world.
            CleanupRuntimeProbes();
            yield return null;

            if (GameProgressManager.Instance == null)
            {
                new GameObject(nameof(GameProgressManager))
                    .AddComponent<GameProgressManager>();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SkillInventoryInitializesFourUsableSlots()
        {
            GameProgressManager.Instance.BeginNewGame();
            Assert.That(PlayerPrefs.HasKey(TestSaveKey), Is.True,
                "PlayMode tests must use the isolated PlayerPrefs key.");

            GameObject player = new GameObject("TestPlayer");
            SkillInventory inventory = player.AddComponent<SkillInventory>();
            yield return null;

            Assert.That(inventory.Slots.Count, Is.EqualTo(4));
            Assert.That(inventory.TryEquip("Fireball", 0), Is.True);
            Assert.That(inventory.Slots[0].skillId, Is.EqualTo("Fireball"));

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraFollowResolvesTaggedPlayerWhenSerializedTargetIsMissing()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            bool ownsPlayer = player == null;
            if (ownsPlayer)
            {
                player = new GameObject("CameraFollowPlayerProbe");
                player.tag = "Player";
            }

            Vector3 originalPlayerPosition = player.transform.position;
            player.transform.position = new Vector3(2.5f, -1.25f, 0f);

            GameObject cameraObject = new GameObject("CameraFollowProbe");
            cameraObject.transform.position = new Vector3(-8f, 6f, -10f);
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();

            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            FieldInfo targetField = typeof(CameraFollow2D).GetField(
                "target", Flags);

            try
            {
                Assert.That(targetField, Is.Not.Null);
                Assert.That(targetField.GetValue(follow), Is.Null,
                    "The regression setup requires the serialized target to be missing.");

                yield return null;

                Assert.That(targetField.GetValue(follow), Is.SameAs(player.transform),
                    "CameraFollow2D should recover its target from the Player tag.");
                Assert.That(cameraObject.transform.position,
                    Is.EqualTo(new Vector3(2.5f, -1.25f, -10f)),
                    "The camera should snap to the recovered target on startup.");

                player.transform.position = new Vector3(-3.75f, 4.5f, 0f);
                yield return null;

                Assert.That(cameraObject.transform.position,
                    Is.EqualTo(new Vector3(-3.75f, 4.5f, -10f)),
                    "LateUpdate should keep following the recovered Player target.");
            }
            finally
            {
                Object.Destroy(cameraObject);
                if (ownsPlayer)
                {
                    Object.Destroy(player);
                }
                else
                {
                    player.transform.position = originalPlayerPosition;
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerMovementKeepsConfiguredSpeedAfterRuntimeInitialization()
        {
            GameObject player = new GameObject("MovementSpeedProbe");
            player.AddComponent<PlayerStats>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            SetClickDestination(movement, new Vector2(100f, 0f));

            // Let input and physics initialize before measuring an exact second
            // (50 default physics ticks) of movement.
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float startX = body.position.x;
            for (int i = 0; i < 50; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float distance = body.position.x - startX;
            Assert.That(distance, Is.EqualTo(PlayerStats.DefaultMoveSpeed).Within(0.08f),
                "Runtime initialization or scene reload changed the player's move speed.");

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerMovementStartsStopsAndReversesResponsively()
        {
            GameObject player = new GameObject("MovementResponseProbe");
            player.AddComponent<PlayerStats>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo hasDestinationField = typeof(PlayerMovement).GetField(
                "hasClickDestination", Flags);
            FieldInfo velocityField = typeof(PlayerMovement).GetField(
                "currentVelocity", Flags);
            Assert.That(hasDestinationField, Is.Not.Null);
            Assert.That(velocityField, Is.Not.Null);

            SetClickDestination(movement, new Vector2(100f, 0f));
            yield return null;
            Vector2 startPosition = body.position;
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector2 forwardVelocity = (Vector2)velocityField.GetValue(movement);
            Vector2 forwardPosition = body.position;
            Assert.That(forwardVelocity.x,
                Is.EqualTo(PlayerStats.DefaultMoveSpeed).Within(0.05f),
                "Movement should reach full speed in about 0.1 seconds.");
            Assert.That(forwardPosition.x - startPosition.x, Is.GreaterThan(0.25f),
                "Commanded velocity must produce real Rigidbody2D travel.");

            SetClickDestination(movement, new Vector2(-100f, 0f));
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector2 reverseVelocity = (Vector2)velocityField.GetValue(movement);
            Assert.That(reverseVelocity.x,
                Is.EqualTo(-PlayerStats.DefaultMoveSpeed).Within(0.05f),
                "A hard turn should not drift in the old direction.");
            Assert.That(body.position.x, Is.LessThan(forwardPosition.x - 0.25f),
                "The physical body should reverse with the input.");

            hasDestinationField.SetValue(movement, false);
            yield return null;
            Vector2 stopPosition = body.position;
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector2 stoppedVelocity = (Vector2)velocityField.GetValue(movement);
            Assert.That(stoppedVelocity.magnitude, Is.LessThan(0.01f),
                "Releasing movement should stop without a long slide.");
            Assert.That(Vector2.Distance(body.position, stopPosition),
                Is.LessThan(0.01f), "The physical body should stop with the input.");

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementFollowsOneRightAnglePath()
        {
            PlayerMovement movement = CreateMovementProbe(
                "RightAngleMovementProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            Vector2 destination = new Vector2(1.5f, 1f);
            SetClickDestination(movement, destination);

            yield return null;
            Vector2 previous = body.position;
            bool sawHorizontal = false;
            bool sawVertical = false;
            int axisChanges = 0;
            int previousAxis = 0;
            for (int i = 0; i < 45; i++)
            {
                yield return new WaitForFixedUpdate();
                Vector2 current = body.position;
                Vector2 step = current - previous;
                previous = current;
                bool movedHorizontal = Mathf.Abs(step.x) > 0.0001f;
                bool movedVertical = Mathf.Abs(step.y) > 0.0001f;
                Assert.That(movedHorizontal && movedVertical, Is.False,
                    "A click path step must never move diagonally.");
                if (!movedHorizontal && !movedVertical)
                {
                    continue;
                }

                int currentAxis = movedHorizontal ? 1 : 2;
                if (previousAxis != 0 && currentAxis != previousAxis)
                {
                    axisChanges++;
                }
                previousAxis = currentAxis;
                sawHorizontal |= movedHorizontal;
                sawVertical |= movedVertical;
                if (movedVertical)
                {
                    Assert.That(body.position.x,
                        Is.EqualTo(destination.x).Within(0.015f),
                        "The vertical leg may start only after the horizontal leg ends.");
                }
            }

            Assert.That(sawHorizontal, Is.True);
            Assert.That(sawVertical, Is.True);
            Assert.That(axisChanges, Is.EqualTo(1),
                "Click movement must form one L with a single 90-degree turn.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.015f));
            Assert.That(ReadClickDestinationState(movement), Is.False);

            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementTurnsWithoutAnIdlePhysicsTick()
        {
            PlayerMovement movement = CreateMovementProbe(
                "ContinuousRightAngleMovementProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            float twoPhysicsSteps = PlayerStats.DefaultMoveSpeed *
                Time.fixedDeltaTime * 2f;
            SetClickDestination(movement,
                new Vector2(twoPhysicsSteps, twoPhysicsSteps));

            // TrySetClickDestination resolves the first leg synchronously. Wait
            // for exactly two physics ticks; a frame yield can contain an extra
            // FixedUpdate when the Test Runner is catching up.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(body.position.x,
                Is.EqualTo(twoPhysicsSteps).Within(0.001f));
            Assert.That(body.position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(movement.MoveDirection, Is.EqualTo(Vector2.up),
                "The click path must turn 90 degrees without an empty physics tick.");
            Assert.That(ReadClickDestinationState(movement), Is.True);

            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerAnimationFacesResolvedLegAtRightAngleTurn()
        {
            GameObject player = new GameObject("RightAngleAnimationProbe");
            Sprite idle = CreateProbeSprite(Color.blue);
            Sprite right = CreateProbeSprite(Color.red);
            Sprite up = CreateProbeSprite(Color.green);
            SpriteRenderer rootRenderer = player.AddComponent<SpriteRenderer>();
            rootRenderer.sprite = idle;
            player.AddComponent<PlayerStats>();
            player.AddComponent<CircleCollider2D>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            PlayerCharacterAnimator animator =
                player.AddComponent<PlayerCharacterAnimator>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();

            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            typeof(PlayerCharacterAnimator).GetField("idleSprite", Flags)
                ?.SetValue(animator, idle);
            typeof(PlayerCharacterAnimator).GetField("walkRightFrames", Flags)
                ?.SetValue(animator, new[] { right });
            typeof(PlayerCharacterAnimator).GetField("walkUpFrames", Flags)
                ?.SetValue(animator, new[] { up });

            SetClickDestination(movement, new Vector2(0.12f, 0.12f));
            yield return null;
            Vector2 previous = body.position;
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForFixedUpdate();
                Vector2 current = body.position;
                Vector2 step = current - previous;
                previous = current;
                yield return null;

                if (!ReadClickDestinationState(movement))
                {
                    Assert.That(movement.VisualRenderer.sprite, Is.SameAs(idle));
                    continue;
                }

                if (Mathf.Abs(step.x) > 0.0001f)
                {
                    Assert.That(movement.VisualRenderer.sprite, Is.SameAs(right),
                        "The sprite must finish the horizontal leg before facing up.");
                }
                else if (Mathf.Abs(step.y) > 0.0001f)
                {
                    Assert.That(movement.VisualRenderer.sprite, Is.SameAs(up));
                }
            }

            Object.Destroy(player);
            Object.Destroy(idle.texture);
            Object.Destroy(right.texture);
            Object.Destroy(up.texture);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCardinalWalkAdvancesFramesAndStopsOnIdle()
        {
            GameObject player = new GameObject("CardinalAnimationProbe");
            Sprite idle = CreateProbeSprite(Color.blue);
            SpriteRenderer rootRenderer = player.AddComponent<SpriteRenderer>();
            rootRenderer.sprite = idle;
            player.AddComponent<PlayerStats>();
            player.AddComponent<CircleCollider2D>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            PlayerCharacterAnimator animator =
                player.AddComponent<PlayerCharacterAnimator>();

            Sprite[] cardinalFrames =
            {
                CreateProbeSprite(Color.red),
                CreateProbeSprite(Color.yellow),
                CreateProbeSprite(Color.green),
                CreateProbeSprite(Color.cyan),
                CreateProbeSprite(Color.magenta),
                CreateProbeSprite(Color.white)
            };
            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            typeof(PlayerCharacterAnimator).GetField("idleSprite", Flags)
                ?.SetValue(animator, idle);
            typeof(PlayerCharacterAnimator).GetField("walkRightFrames", Flags)
                ?.SetValue(animator, cardinalFrames);

            SetClickDestination(movement, new Vector2(100f, 0f));
            yield return null;

            HashSet<Sprite> displayedFrames = new HashSet<Sprite>();
            for (int i = 0; i < 35; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                Sprite current = movement.VisualRenderer != null
                    ? movement.VisualRenderer.sprite
                    : null;
                if (current != null && current != idle)
                {
                    displayedFrames.Add(current);
                }
            }

            Assert.That(displayedFrames.Count, Is.GreaterThanOrEqualTo(3),
                "A held cardinal direction must advance through foot poses.");

            typeof(PlayerMovement).GetField("hasClickDestination", Flags)
                ?.SetValue(movement, false);
            yield return null;
            Assert.That(movement.VisualRenderer.sprite, Is.SameAs(idle),
                "Releasing movement must restore the authored idle immediately.");

            Object.Destroy(player);
            Object.Destroy(idle.texture);
            foreach (Sprite frame in cardinalFrames)
            {
                Object.Destroy(frame.texture);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementRoutesThroughMazeWithoutDiagonalOrIdleTicks()
        {
            PlayerMovement movement = CreateMovementProbe(
                "MazeRouteProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);

            // The route planner uses a 0.5-unit cardinal grid. These staggered
            // walls force the shortest route to weave down, right, up, right,
            // then down again instead of falling back to a single L-shaped path.
            const float NavigationCellSize = 0.5f;
            GameObject upperWall = new GameObject("UpperMazeWall");
            upperWall.transform.position = new Vector3(
                NavigationCellSize * 2.5f,
                NavigationCellSize * 2.5f,
                0f);
            BoxCollider2D upperWallCollider =
                upperWall.AddComponent<BoxCollider2D>();
            upperWallCollider.size = new Vector2(
                NavigationCellSize * 0.5f,
                NavigationCellSize * 7f);

            GameObject lowerWall = new GameObject("LowerMazeWall");
            lowerWall.transform.position = new Vector3(
                NavigationCellSize * 6.5f,
                -NavigationCellSize * 2.5f,
                0f);
            BoxCollider2D lowerWallCollider =
                lowerWall.AddComponent<BoxCollider2D>();
            lowerWallCollider.size = new Vector2(
                NavigationCellSize * 0.5f,
                NavigationCellSize * 7f);
            Physics2D.SyncTransforms();

            Vector2 destination = new Vector2(
                NavigationCellSize * 9f, 0f);
            SetClickDestination(movement, destination);
            Assert.That(movement.IsFollowingClickPath, Is.True,
                "A reachable maze click should activate its route immediately.");

            Vector2 previousPosition = body.position;
            int previousAxis = 0;
            int axisChanges = 0;
            for (int i = 0; i < 320 && movement.IsFollowingClickPath; i++)
            {
                bool routeWasActive = movement.IsFollowingClickPath;
                yield return new WaitForFixedUpdate();

                Vector2 currentPosition = body.position;
                Vector2 displacement = currentPosition - previousPosition;
                previousPosition = currentPosition;
                bool movedHorizontally =
                    Mathf.Abs(displacement.x) > 0.0001f;
                bool movedVertically =
                    Mathf.Abs(displacement.y) > 0.0001f;

                Assert.That(movedHorizontally && movedVertically, Is.False,
                    "Every maze-route physics step must stay on one cardinal axis.");
                if (routeWasActive && movement.IsFollowingClickPath)
                {
                    Assert.That(displacement.sqrMagnitude,
                        Is.GreaterThan(0.00000001f),
                        "An active route must not insert an idle physics tick at a corner.");
                    Assert.That(movement.IsMoving, Is.True,
                        "The movement state must remain active between route legs.");
                }

                if (movedHorizontally || movedVertically)
                {
                    int currentAxis = movedHorizontally ? 1 : 2;
                    if (previousAxis != 0 && currentAxis != previousAxis)
                    {
                        axisChanges++;
                    }

                    previousAxis = currentAxis;
                }

                Assert.That(Physics2D.Distance(
                    playerCollider, upperWallCollider).isOverlapped, Is.False,
                    "The planned route must never penetrate the upper maze wall.");
                Assert.That(Physics2D.Distance(
                    playerCollider, lowerWallCollider).isOverlapped, Is.False,
                    "The planned route must never penetrate the lower maze wall.");
            }

            Assert.That(movement.IsFollowingClickPath, Is.False,
                "The route should complete within the physics-step budget.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.03f),
                "The player should reach the requested destination around the walls.");
            Assert.That(axisChanges, Is.GreaterThanOrEqualTo(3),
                "The staggered maze must exercise a route with at least three turns.");

            Object.Destroy(upperWall);
            Object.Destroy(lowerWall);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementRoutesThroughMixedOrganicCorridorAtTwoGridPhases()
        {
            GameObject obstacleRoot = new GameObject("MixedOrganicCorridor");

            BoxCollider2D topBoundary = CreateStaticBoxCollider(
                "OrganicTopBoundary", obstacleRoot.transform,
                new Vector2(3.75f, 3f), new Vector2(10f, 0.5f));
            BoxCollider2D bottomBoundary = CreateStaticBoxCollider(
                "OrganicBottomBoundary", obstacleRoot.transform,
                new Vector2(3.75f, -3f), new Vector2(10f, 0.5f));
            CircleCollider2D upperBulge = CreateStaticCircleCollider(
                "OrganicUpperBulge", obstacleRoot.transform,
                new Vector2(2f, 1.65f), 1.25f);
            BoxCollider2D lowerBulge = CreateStaticBoxCollider(
                "OrganicLowerBulge", obstacleRoot.transform,
                new Vector2(5.2f, -1.65f), new Vector2(0.8f, 2.5f));
            Collider2D[] blockers =
                { topBoundary, bottomBoundary, upperBulge, lowerBulge };
            Physics2D.SyncTransforms();

            float[] gridPhases = { 0f, 0.25f };
            foreach (float phase in gridPhases)
            {
                PlayerMovement movement = CreateMovementProbe(
                    $"OrganicCorridorPhase_{phase:0.00}", out Rigidbody2D body,
                    out CircleCollider2D playerCollider);
                SetMovementSpeed(movement, 10f);
                body.position = new Vector2(0f, phase);
                Physics2D.SyncTransforms();

                Vector2 destination = new Vector2(7.5f, phase);
                SetClickDestination(movement, destination);
                Vector2 previousPosition = body.position;
                float lowestY = body.position.y;
                int previousAxis = 0;
                int axisChanges = 0;

                for (int i = 0; i < 100 && movement.IsFollowingClickPath; i++)
                {
                    bool routeWasActive = movement.IsFollowingClickPath;
                    yield return new WaitForFixedUpdate();

                    Vector2 currentPosition = body.position;
                    Vector2 displacement = currentPosition - previousPosition;
                    previousPosition = currentPosition;
                    lowestY = Mathf.Min(lowestY, currentPosition.y);
                    bool movedHorizontally =
                        Mathf.Abs(displacement.x) > 0.0001f;
                    bool movedVertically =
                        Mathf.Abs(displacement.y) > 0.0001f;

                    Assert.That(movedHorizontally && movedVertically, Is.False,
                        $"Grid phase {phase}: organic routing moved diagonally.");
                    if (routeWasActive && movement.IsFollowingClickPath)
                    {
                        Assert.That(displacement.sqrMagnitude,
                            Is.GreaterThan(0.00000001f),
                            $"Grid phase {phase}: a route corner inserted an idle tick.");
                    }

                    if (movedHorizontally || movedVertically)
                    {
                        int currentAxis = movedHorizontally ? 1 : 2;
                        if (previousAxis != 0 && currentAxis != previousAxis)
                        {
                            axisChanges++;
                        }

                        previousAxis = currentAxis;
                    }

                    foreach (Collider2D blocker in blockers)
                    {
                        Assert.That(Physics2D.Distance(
                            playerCollider, blocker).isOverlapped, Is.False,
                            $"Grid phase {phase}: Player overlapped {blocker.name}.");
                    }
                }

                Assert.That(movement.IsFollowingClickPath, Is.False,
                    $"Grid phase {phase}: organic corridor route did not finish.");
                Assert.That(Vector2.Distance(body.position, destination),
                    Is.LessThan(0.03f),
                    $"Grid phase {phase}: Player missed the corridor destination.");
                Assert.That(lowestY, Is.LessThan(phase - 0.2f),
                    $"Grid phase {phase}: route did not detour below the round bulge.");
                Assert.That(axisChanges, Is.GreaterThanOrEqualTo(2),
                    $"Grid phase {phase}: mixed corridor did not exercise its turns.");

                Object.Destroy(movement.gameObject);
                yield return null;
            }

            Object.Destroy(obstacleRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementDetoursMoreThanSixWorldUnitsAroundLongWall()
        {
            PlayerMovement movement = CreateMovementProbe(
                "LongDetourRouteProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            SetMovementSpeed(movement, 10f);

            GameObject wall = new GameObject("LongDetourWall");
            wall.transform.position = new Vector3(2f, 0f, 0f);
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(0.25f, 12f);
            Physics2D.SyncTransforms();

            Vector2 destination = new Vector2(4f, 0f);
            SetClickDestination(movement, destination);
            float furthestVerticalDetour = 0f;
            for (int i = 0; i < 150 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                furthestVerticalDetour = Mathf.Max(furthestVerticalDetour,
                    Mathf.Abs(body.position.y));
                Assert.That(Physics2D.Distance(
                    playerCollider, wallCollider).isOverlapped, Is.False,
                    "A long detour must never cut through the wall collider.");
            }

            Assert.That(furthestVerticalDetour, Is.GreaterThan(6f),
                "The route must search beyond the former six-world-unit padding.");
            Assert.That(movement.IsFollowingClickPath, Is.False,
                "The enlarged path search must finish instead of staying active.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.03f),
                "The player should reach the destination after the long detour.");

            Object.Destroy(wall);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickOnStaticColliderSnapsToNearbyGridCell()
        {
            PlayerMovement movement = CreateMovementProbe(
                "BlockedClickSnapProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            SetMovementSpeed(movement, 10f);

            GameObject obstacle = new GameObject("GridAlignedClickObstacle");
            obstacle.transform.position = new Vector3(3f, 0f, 0f);
            BoxCollider2D obstacleCollider =
                obstacle.AddComponent<BoxCollider2D>();
            obstacleCollider.size = Vector2.one;
            Physics2D.SyncTransforms();

            Vector2 clickedPosition = obstacle.transform.position;
            SetClickDestination(movement, clickedPosition);
            for (int i = 0; i < 60 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(Physics2D.Distance(
                    playerCollider, obstacleCollider).isOverlapped, Is.False,
                    "Snapping a blocked click must not move through its collider.");
            }

            float snapDistance = Vector2.Distance(body.position, clickedPosition);
            Assert.That(movement.IsFollowingClickPath, Is.False,
                "A blocked click should finish at its snapped interaction cell.");
            Assert.That(snapDistance, Is.GreaterThan(0.5f),
                "The route must not finish inside the clicked collider.");
            Assert.That(snapDistance, Is.LessThanOrEqualTo(1.5f),
                "The snapped cell must remain within interaction range.");
            Assert.That(body.position.x * 2f,
                Is.EqualTo(Mathf.Round(body.position.x * 2f)).Within(0.01f));
            Assert.That(body.position.y * 2f,
                Is.EqualTo(Mathf.Round(body.position.y * 2f)).Within(0.01f),
                "A grid-aligned blocked click should finish on the 0.5-unit grid.");

            Object.Destroy(obstacle);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickOnCircleColliderSnapsWithinInteractionRange()
        {
            PlayerMovement movement = CreateMovementProbe(
                "CircleBlockedClickSnapProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            SetMovementSpeed(movement, 10f);

            GameObject obstacle = new GameObject("CircleClickObstacle");
            obstacle.transform.position = new Vector3(3f, 0f, 0f);
            CircleCollider2D obstacleCollider =
                obstacle.AddComponent<CircleCollider2D>();
            obstacleCollider.radius = 0.65f;
            Physics2D.SyncTransforms();

            Vector2 clickedPosition = obstacle.transform.position;
            SetClickDestination(movement, clickedPosition);
            for (int i = 0; i < 60 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(Physics2D.Distance(
                    playerCollider, obstacleCollider).isOverlapped, Is.False,
                    "A snapped circle click must not penetrate the obstacle.");
            }

            float snapDistance = Vector2.Distance(body.position, clickedPosition);
            Assert.That(movement.IsFollowingClickPath, Is.False,
                "A circle-blocked click should finish at a reachable nearby cell.");
            Assert.That(snapDistance,
                Is.GreaterThan(obstacleCollider.radius + playerCollider.radius),
                "The snapped destination must remain outside both collider radii.");
            Assert.That(snapDistance, Is.LessThanOrEqualTo(1.5f),
                "The snapped circle destination must remain in interaction range.");

            Object.Destroy(obstacle);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickOutsideClosedBoundaryCannotEscape()
        {
            GameObject boundaryRoot = new GameObject(
                "ClosedOrganicNavigationBoundary");

            // Mirror the generated maps' orthogonal organic outline: many
            // colliders meet with a small overlap instead of one arena box.
            // A route search must not mistake any seam for a walkable opening.
            CreateStaticBoxCollider("BoundaryBottomLeft", boundaryRoot.transform,
                new Vector2(-2f, -2f), new Vector2(2.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryLowerLeftStep", boundaryRoot.transform,
                new Vector2(-1f, -2.5f), new Vector2(0.5f, 1.1f));
            CreateStaticBoxCollider("BoundaryBottomCenter", boundaryRoot.transform,
                new Vector2(0.5f, -3f), new Vector2(3.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryLowerRightStep", boundaryRoot.transform,
                new Vector2(2f, -2.5f), new Vector2(0.5f, 1.1f));
            CreateStaticBoxCollider("BoundaryBottomRight", boundaryRoot.transform,
                new Vector2(2.5f, -2f), new Vector2(1.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryRight", boundaryRoot.transform,
                new Vector2(3f, 0f), new Vector2(0.5f, 4.1f));
            CreateStaticBoxCollider("BoundaryTopRight", boundaryRoot.transform,
                new Vector2(2f, 2f), new Vector2(2.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryUpperRightStep", boundaryRoot.transform,
                new Vector2(1f, 2.5f), new Vector2(0.5f, 1.1f));
            CreateStaticBoxCollider("BoundaryTopCenter", boundaryRoot.transform,
                new Vector2(-0.5f, 3f), new Vector2(3.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryUpperLeftStep", boundaryRoot.transform,
                new Vector2(-2f, 2.5f), new Vector2(0.5f, 1.1f));
            CreateStaticBoxCollider("BoundaryTopLeft", boundaryRoot.transform,
                new Vector2(-2.5f, 2f), new Vector2(1.1f, 0.5f));
            CreateStaticBoxCollider("BoundaryLeft", boundaryRoot.transform,
                new Vector2(-3f, 0f), new Vector2(0.5f, 4.1f));

            PlayerMovement movement = CreateMovementProbe(
                "ClosedBoundaryProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetMovementSpeed(movement, 10f);
            Physics2D.SyncTransforms();

            Vector2 startPosition = body.position;
            bool accepted = movement.TrySetClickDestination(
                new Vector2(4f, 0f));
            Assert.That(accepted, Is.False,
                "A segmented organic boundary must not expose a route through " +
                "one of its collider seams.");
            Assert.That(movement.IsFollowingClickPath, Is.False);

            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Vector2.Distance(body.position, startPosition),
                Is.LessThan(0.001f),
                "A rejected outside click must leave the Player inside the boundary.");

            Object.Destroy(boundaryRoot);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickMovementRoutesAroundDynamicRigidbodyWithoutHanging()
        {
            PlayerMovement movement = CreateMovementProbe(
                "DynamicObstacleRouteProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            SetMovementSpeed(movement, 10f);

            GameObject obstacle = new GameObject("DynamicRouteObstacle");
            Rigidbody2D obstacleBody = obstacle.AddComponent<Rigidbody2D>();
            obstacleBody.bodyType = RigidbodyType2D.Dynamic;
            obstacleBody.gravityScale = 0f;
            obstacleBody.constraints = RigidbodyConstraints2D.FreezeAll;
            obstacleBody.position = new Vector2(2f, 0f);
            BoxCollider2D obstacleCollider =
                obstacle.AddComponent<BoxCollider2D>();
            obstacleCollider.size = new Vector2(0.5f, 0.5f);
            Physics2D.SyncTransforms();

            Vector2 obstacleStart = obstacleBody.position;
            Vector2 destination = new Vector2(4f, 0f);
            SetClickDestination(movement, destination);
            float furthestVerticalDetour = 0f;
            for (int i = 0; i < 90 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                furthestVerticalDetour = Mathf.Max(furthestVerticalDetour,
                    Mathf.Abs(body.position.y));
                Assert.That(Physics2D.Distance(
                    playerCollider, obstacleCollider).isOverlapped, Is.False,
                    "The route must avoid a dynamic Rigidbody2D collider.");
            }

            Assert.That(furthestVerticalDetour, Is.GreaterThan(0.7f),
                "The planned path must visibly detour around the dynamic body.");
            Assert.That(Vector2.Distance(obstacleBody.position, obstacleStart),
                Is.LessThan(0.01f),
                "Routing should avoid the dynamic obstacle instead of pushing it.");
            Assert.That(movement.IsFollowingClickPath, Is.False,
                "A dynamic blocker must not leave click movement permanently active.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.03f));

            Object.Destroy(obstacle);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickPathCacheRefreshesAfterDynamicBlockerMoves()
        {
            GameObject obstacleRoot = new GameObject(
                "MovingTraversalCacheCorridor");
            BoxCollider2D topBoundary = CreateStaticBoxCollider(
                "CacheCorridorTop", obstacleRoot.transform,
                new Vector2(3f, 2f), new Vector2(100f, 0.5f));
            BoxCollider2D bottomBoundary = CreateStaticBoxCollider(
                "CacheCorridorBottom", obstacleRoot.transform,
                new Vector2(3f, -2f), new Vector2(100f, 0.5f));
            BoxCollider2D centerBlocker = CreateStaticBoxCollider(
                "CacheCorridorCenter", obstacleRoot.transform,
                new Vector2(3f, 0f), new Vector2(0.5f, 0.5f));

            GameObject movingBlocker = new GameObject(
                "MovingTraversalCacheBlocker");
            movingBlocker.transform.SetParent(obstacleRoot.transform);
            Rigidbody2D blockerBody = movingBlocker.AddComponent<Rigidbody2D>();
            blockerBody.bodyType = RigidbodyType2D.Dynamic;
            blockerBody.gravityScale = 0f;
            blockerBody.constraints = RigidbodyConstraints2D.FreezeAll;
            blockerBody.position = new Vector2(3f, 1f);
            BoxCollider2D blockerCollider =
                movingBlocker.AddComponent<BoxCollider2D>();
            blockerCollider.size = new Vector2(0.5f, 0.5f);

            PlayerMovement movement = CreateMovementProbe(
                "MovingTraversalCacheProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            SetMovementSpeed(movement, 10f);
            Collider2D[] blockers =
                { topBoundary, bottomBoundary, centerBlocker, blockerCollider };
            Physics2D.SyncTransforms();

            Vector2 destination = new Vector2(6f, 0f);
            SetClickDestination(movement, destination);
            float lowestY = body.position.y;
            for (int i = 0; i < 120 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                lowestY = Mathf.Min(lowestY, body.position.y);
                foreach (Collider2D blocker in blockers)
                {
                    Assert.That(Physics2D.Distance(
                        playerCollider, blocker).isOverlapped, Is.False);
                }
            }

            Assert.That(movement.IsFollowingClickPath, Is.False,
                "The first route should finish through the open lower lane.");
            Assert.That(lowestY, Is.LessThan(-0.6f),
                "The upper blocker should force the first route below center.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.03f));

            body.position = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            blockerBody.position = new Vector2(3f, -1f);
            Physics2D.SyncTransforms();

            SetClickDestination(movement, destination);
            float highestY = body.position.y;
            for (int i = 0; i < 120 && movement.IsFollowingClickPath; i++)
            {
                yield return new WaitForFixedUpdate();
                highestY = Mathf.Max(highestY, body.position.y);
                foreach (Collider2D blocker in blockers)
                {
                    Assert.That(Physics2D.Distance(
                        playerCollider, blocker).isOverlapped, Is.False);
                }
            }

            Assert.That(movement.IsFollowingClickPath, Is.False,
                "A second search must not reuse passability from before the " +
                "dynamic blocker moved.");
            Assert.That(highestY, Is.GreaterThan(0.6f),
                "Moving the blocker below center should reopen the upper lane.");
            Assert.That(Vector2.Distance(body.position, destination),
                Is.LessThan(0.03f));

            Object.Destroy(obstacleRoot);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerAutoCombatCastsEquippedSkillAtEnemyInRange()
        {
            GameObject player = new GameObject("AutoSkillPlayerProbe");
            player.tag = "Player";
            player.AddComponent<PlayerStats>();
            player.AddComponent<CircleCollider2D>().radius = 0.48f;
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerHealth>();
            SkillInventory inventory = player.AddComponent<SkillInventory>();

            SkillData fireballData = ScriptableObject.CreateInstance<SkillData>();
            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            typeof(SkillData).GetField("skillId", Flags)?.SetValue(
                fireballData, SkillId.Fireball);
            typeof(SkillData).GetField("manaCost", Flags)?.SetValue(
                fireballData, 0f);
            typeof(SkillData).GetField("cooldown", Flags)?.SetValue(
                fireballData, 1f);
            typeof(SkillData).GetField("range", Flags)?.SetValue(
                fireballData, 6f);

            FireballSkill fireball = player.AddComponent<FireballSkill>();
            fireball.Initialize(fireballData, 1);
            inventory.AddOrUpgrade("Fireball");
            Assert.That(inventory.TryEquip("Fireball", 0), Is.True);
            SkillManager manager = player.AddComponent<SkillManager>();
            typeof(SkillManager).GetField("skillDatabase", Flags)?.SetValue(
                manager, new[] { fireballData });
            player.AddComponent<PlayerCombat>();

            GameObject target = new GameObject("AutoSkillTargetProbe");
            target.transform.position = new Vector3(3f, 0f, 0f);
            CircleCollider2D targetCollider = target.AddComponent<CircleCollider2D>();
            targetCollider.radius = 0.35f;
            targetCollider.isTrigger = true;
            EnemyHealth targetHealth = target.AddComponent<EnemyHealth>();
            targetHealth.Initialize(null);
            Physics2D.SyncTransforms();

            bool sawProjectile = false;
            float timeout = Time.time + 1.5f;
            while (Time.time < timeout && !sawProjectile &&
                targetHealth != null && targetHealth.IsAlive)
            {
                yield return null;
                sawProjectile = GameObject.Find("FireballProjectile2D") != null;
            }

            Assert.That(sawProjectile || targetHealth == null ||
                !targetHealth.IsAlive, Is.True,
                "Auto combat should cast an equipped offensive skill at range.");

            GameObject projectile = GameObject.Find("FireballProjectile2D");
            if (projectile != null) Object.Destroy(projectile);
            Object.Destroy(target);
            Object.Destroy(player);
            Object.Destroy(fireballData);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerAutoAttackTargetsNearestEnemyByDefault()
        {
            PlayerMovement movement = CreateMovementProbe(
                "AutoAttackPlayerProbe", out Rigidbody2D unusedBody,
                out CircleCollider2D unusedCollider);
            PlayerCombat combat = movement.gameObject.AddComponent<PlayerCombat>();

            GameObject target = new GameObject("AutoAttackTargetProbe");
            target.transform.position = new Vector3(0.9f, 0f, 0f);
            target.AddComponent<CircleCollider2D>().radius = 0.35f;
            EnemyHealth targetHealth = target.AddComponent<EnemyHealth>();
            targetHealth.Initialize(null);
            Physics2D.SyncTransforms();

            Assert.That(combat.AutoAttackEnabled, Is.True,
                "Auto attack should be enabled for a newly created Player.");

            float timeout = Time.time + 1.5f;
            while (targetHealth != null && targetHealth.IsAlive &&
                Time.time < timeout)
            {
                yield return null;
            }

            Assert.That(targetHealth == null || !targetHealth.IsAlive, Is.True,
                "The Player should automatically attack a living target in range.");

            Object.Destroy(target);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerAutoCombatWaitsUntilMovementStops()
        {
            PlayerMovement movement = CreateMovementProbe(
                "MoveBeforeAutoCombatProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetMovementSpeed(movement, 3f);
            PlayerCombat combat = movement.gameObject.AddComponent<PlayerCombat>();

            GameObject target = new GameObject("MoveBeforeAutoTargetProbe");
            target.layer = TopDownGame.Core.GameplayCollisionRules.EnemyLayer;
            target.transform.position = new Vector3(0f, 0.9f, 0f);
            target.AddComponent<CircleCollider2D>().radius = 0.35f;
            EnemyHealth targetHealth = target.AddComponent<EnemyHealth>();
            targetHealth.Initialize(null);
            Physics2D.SyncTransforms();

            SetClickDestination(movement, new Vector2(2.5f, 0f));
            float movementWindowEnd = Time.time + 0.35f;
            while (Time.time < movementWindowEnd)
            {
                Assert.That(targetHealth.IsAlive, Is.True,
                    "Auto combat must not attack while movement is active.");
                yield return null;
            }
            Assert.That(body.position.x, Is.GreaterThan(0.2f));

            movement.CancelMovement();
            target.transform.position = movement.transform.position +
                new Vector3(0f, 0.9f, 0f);
            Physics2D.SyncTransforms();
            float timeout = Time.time + 1.5f;
            while (targetHealth != null && targetHealth.IsAlive &&
                Time.time < timeout)
            {
                yield return null;
            }

            Assert.That(targetHealth == null || !targetHealth.IsAlive, Is.True,
                "Auto combat should resume after the player has stopped.");
            Assert.That(combat.AutoAttackEnabled, Is.True);

            Object.Destroy(target);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickMovementInterruptsAutomaticAttackWithoutLosingDestination()
        {
            PlayerMovement movement = CreateMovementProbe(
                "AutoAttackClickInterruptProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetMovementSpeed(movement, 3f);
            PlayerCombat combat = movement.gameObject.AddComponent<PlayerCombat>();

            GameObject target = new GameObject("AutoAttackInterruptTargetProbe");
            target.layer = GameplayCollisionRules.EnemyLayer;
            target.transform.position = new Vector3(0f, 0.9f, 0f);
            target.AddComponent<CircleCollider2D>().radius = 0.35f;
            EnemyHealth targetHealth = target.AddComponent<EnemyHealth>();
            targetHealth.Initialize(null);
            Physics2D.SyncTransforms();

            float attackTimeout = Time.time + 1f;
            while (!combat.IsAttacking && Time.time < attackTimeout)
            {
                yield return null;
            }
            Assert.That(combat.IsAttacking, Is.True,
                "The probe must enter an automatic attack before movement interrupts it.");

            SetClickDestination(movement, new Vector2(2.5f, 0f));
            float movementTimeout = Time.time + 0.55f;
            while (Time.time < movementTimeout)
            {
                yield return null;
            }

            Assert.That(body.position.x, Is.GreaterThan(0.65f),
                "A click during auto attack must move immediately instead of being erased by the windup lock.");

            Object.Destroy(target);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickPathAndBodyCastPassThroughEnemyLayer()
        {
            PlayerMovement movement = CreateMovementProbe(
                "ClickThroughEnemyProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetMovementSpeed(movement, 3f);

            GameObject enemy = new GameObject("ClickPathEnemyBlockerProbe");
            enemy.layer = GameplayCollisionRules.EnemyLayer;
            enemy.transform.position = new Vector3(1.1f, 0f, 0f);
            enemy.AddComponent<CircleCollider2D>().radius = 0.55f;
            Physics2D.SyncTransforms();

            Assert.That((movement.EffectiveMovementMask &
                (1 << GameplayCollisionRules.EnemyLayer)), Is.Zero,
                "Runtime movement casts must explicitly exclude enemy bodies.");
            SetClickDestination(movement, new Vector2(2.5f, 0f));
            float timeout = Time.time + 1.1f;
            while (body.position.x < 2.2f && Time.time < timeout)
            {
                yield return null;
            }

            Assert.That(body.position.x, Is.GreaterThan(2.2f),
                "The player should cross a solid enemy collider while following a click path.");

            Object.Destroy(enemy);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyKnockbackDoesNotEraseActiveClickDestination()
        {
            PlayerMovement movement = CreateMovementProbe(
                "ClickKnockbackResumeProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetMovementSpeed(movement, 3f);

            SetClickDestination(movement, new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();
            movement.AddImpulse(new Vector2(-2f, 1f));

            Assert.That(movement.IsFollowingClickPath, Is.True,
                "Enemy knockback must preserve the active click command.");

            float timeout = Time.time + 2f;
            while (movement.IsFollowingClickPath && Time.time < timeout)
            {
                yield return null;
            }

            Assert.That(body.position.x, Is.EqualTo(3f).Within(0.12f),
                "Movement should resume and reach the clicked X coordinate after knockback.");
            Assert.That(body.position.y, Is.EqualTo(0f).Within(0.12f),
                "Movement should return to the clicked Y coordinate after knockback.");

            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MiniMapBuildsLiveOverviewWithoutBlockingPointerInput()
        {
            GameObject environment = new GameObject("Environment");
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(environment.transform);
            CreateStaticBoxCollider("MiniMapWallTop", walls.transform,
                new Vector2(0f, 5f), new Vector2(20f, 0.5f));
            CreateStaticBoxCollider("MiniMapWallBottom", walls.transform,
                new Vector2(0f, -5f), new Vector2(20f, 0.5f));
            CreateStaticBoxCollider("MiniMapWallLeft", walls.transform,
                new Vector2(-10f, 0f), new Vector2(0.5f, 10f));
            CreateStaticBoxCollider("MiniMapWallRight", walls.transform,
                new Vector2(10f, 0f), new Vector2(0.5f, 10f));

            GameObject player = new GameObject("MiniMapPlayerProbe");
            player.tag = "Player";
            player.transform.position = new Vector3(-3f, -2f, 0f);

            GameObject door = new GameObject("MiniMapDoorProbe");
            door.transform.position = new Vector3(4f, 2f, 0f);
            door.AddComponent<BoxCollider2D>().isTrigger = true;
            door.AddComponent<MapDoor>();

            GameObject chest = new GameObject("MiniMapChestProbe");
            chest.transform.position = new Vector3(2f, -1f, 0f);
            chest.AddComponent<RewardChest>();

            GameObject canvasObject = new GameObject("MiniMapCanvasProbe");
            canvasObject.AddComponent<Canvas>().renderMode =
                RenderMode.ScreenSpaceOverlay;
            MiniMapController miniMap = canvasObject.AddComponent<MiniMapController>();
            yield return null;
            yield return null;

            Assert.That(miniMap.IsReady, Is.True);
            Assert.That(miniMap.WorldBounds.size.x, Is.GreaterThanOrEqualTo(20f));
            Assert.That(miniMap.WorldBounds.size.y, Is.GreaterThanOrEqualTo(10f));
            Assert.That(miniMap.MarkerCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(miniMap.Panel.GetComponentsInChildren<Graphic>(true),
                Has.All.Matches<Graphic>(graphic => !graphic.raycastTarget),
                "The minimap must never consume click-to-move pointer input.");

            Object.Destroy(canvasObject);
            Object.Destroy(chest);
            Object.Destroy(door);
            Object.Destroy(player);
            Object.Destroy(environment);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerMovementIgnoresTriggerColliders()
        {
            PlayerMovement movement = CreateMovementProbe(
                "TriggerPassThroughProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);

            GameObject trigger = new GameObject("InteractionTriggerProbe");
            trigger.transform.position = new Vector3(0.8f, 0f, 0f);
            BoxCollider2D triggerCollider = trigger.AddComponent<BoxCollider2D>();
            triggerCollider.size = new Vector2(0.2f, 3f);
            triggerCollider.isTrigger = true;
            Physics2D.SyncTransforms();

            SetClickDestination(movement, new Vector2(5f, 0f));
            yield return null;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.x, Is.GreaterThan(1.4f),
                "Portals, pickups and interaction triggers must not block movement.");

            Object.Destroy(trigger);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerClickDestinationStopsWithoutOvershoot()
        {
            PlayerMovement movement = CreateMovementProbe(
                "ClickArrivalProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            SetClickDestination(movement, Vector2.right);

            yield return null;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.x, Is.LessThanOrEqualTo(1f));
            Assert.That(Vector2.Distance(body.position, Vector2.right),
                Is.LessThan(0.015f));
            Assert.That(ReadClickDestinationState(movement), Is.False,
                "Arrival should clear the click target instead of reversing direction.");

            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDamageKnockbackUsesKinematicImpulse()
        {
            PlayerMovement movement = CreateMovementProbe(
                "KnockbackProbe", out Rigidbody2D body,
                out CircleCollider2D unusedCollider);
            PlayerHealth health = movement.gameObject.AddComponent<PlayerHealth>();
            yield return null;

            health.TakeDamage(new DamageInfo(1f, movement.gameObject,
                body.position, Vector2.right, 5f, ignoreArmor: true));
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.x, Is.GreaterThan(0.1f),
                "Kinematic players must still react to combat knockback.");

            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IceDashStopsAtSolidWall()
        {
            PlayerMovement movement = CreateMovementProbe(
                "IceDashWallProbe", out Rigidbody2D body,
                out CircleCollider2D playerCollider);
            IceDashSkill dash = movement.gameObject.AddComponent<IceDashSkill>();
            SkillData data = ScriptableObject.CreateInstance<SkillData>();
            dash.Initialize(data, 1);

            GameObject wall = new GameObject("IceDashObstacle");
            wall.transform.position = new Vector3(1f, 0f, 0f);
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(0.2f, 10f);
            Physics2D.SyncTransforms();

            dash.Cast();
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            ColliderDistance2D separation = Physics2D.Distance(
                playerCollider, wallCollider);
            Assert.That(separation.isOverlapped, Is.False);
            Assert.That(body.position.x, Is.LessThan(0.4f),
                "Ice Dash must use the same collision solver as walking.");
            Assert.That(movement.enabled, Is.True,
                "Normal movement should be restored after the dash ends.");

            Object.Destroy(wall);
            Object.Destroy(data);
            Object.Destroy(movement.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrystalScorpionUsesDistanceBasedMovementBands()
        {
            EnemyData data = CreateCrystalScorpionData();
            CrystalScorpionEnemy scorpion = CreateCrystalScorpionProbe(data);
            GameObject target = new GameObject("CrystalScorpionMovementTargetProbe");
            target.transform.position = new Vector3(6f, 0f, 0f);

            yield return null;
            SetEnemyTarget(scorpion, target.transform);
            SetNextAttackTime(scorpion, float.PositiveInfinity);
            yield return null;

            Assert.That(scorpion.CurrentState,
                Is.EqualTo(CrystalScorpionCombatState.Pursuing),
                "A detected target beyond the orbit band should be pursued.");

            target.transform.position = new Vector3(3f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(scorpion.CurrentState,
                Is.EqualTo(CrystalScorpionCombatState.Orbiting),
                "A target between 2.2 and 4.0 units should be orbited.");

            target.transform.position = new Vector3(1f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(scorpion.CurrentState,
                Is.EqualTo(CrystalScorpionCombatState.Retreating),
                "A target closer than 2.2 units should force a retreat.");

            Object.Destroy(target);
            Object.Destroy(scorpion.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrystalScorpionTailSpikeDamagesOnlyAfterTelegraph()
        {
            EnemyData data = CreateCrystalScorpionData();
            CrystalScorpionEnemy scorpion = CreateCrystalScorpionProbe(data);

            GameObject player = new GameObject("CrystalScorpionSpikeTargetProbe");
            player.transform.position = new Vector3(3f, 0f, 0f);
            player.AddComponent<PlayerStats>();
            PlayerHealth health = player.AddComponent<PlayerHealth>();

            yield return null;
            SetEnemyTarget(scorpion, player.transform);
            SetPrivateField(scorpion, "attackPatternIndex", 2);
            SetNextAttackTime(scorpion, 0f);
            float healthBeforeAttack = health.CurrentHealth;

            yield return null;
            Assert.That(scorpion.CurrentState,
                Is.EqualTo(CrystalScorpionCombatState.Telegraphing),
                "The third attack should start as a locked tail-spike telegraph.");

            yield return new WaitForSeconds(0.5f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBeforeAttack),
                "Tail Crystal Spike must not damage during its 0.72-second telegraph.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthBeforeAttack),
                "Tail Crystal Spike should damage a target that remains in the locked area.");

            Object.Destroy(player);
            Object.Destroy(scorpion.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyMovementSteersThroughDirectionChangesWithoutSnapping()
        {
            GameObject enemy = new GameObject("SmoothEnemySteeringProbe");
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            movement.MoveTowards(new Vector2(10f, 0f), 4f);
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movement.CurrentVelocity.x, Is.GreaterThan(2f));
            movement.MoveTowards(body.position + Vector2.up * 10f, 4f);
            yield return new WaitForFixedUpdate();

            Vector2 firstTurnVelocity = movement.CurrentVelocity;
            Assert.That(firstTurnVelocity.x, Is.GreaterThan(1f),
                "A 90-degree AI direction change must retain forward momentum " +
                "for a short steering arc instead of snapping instantly.");
            Assert.That(firstTurnVelocity.y, Is.GreaterThan(0f));

            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movement.CurrentVelocity.y,
                Is.GreaterThan(Mathf.Abs(movement.CurrentVelocity.x)),
                "Steering should settle cleanly into the new direction.");

            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyArrivalSteeringBrakesBeforeAttackRange()
        {
            GameObject enemy = new GameObject("EnemyArrivalSteeringProbe");
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            movement.Arrive(new Vector2(10f, 0f), 4f, 1.1f, 1.35f);
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float cruisingSpeed = movement.Speed;
            Assert.That(cruisingSpeed, Is.GreaterThan(3.5f));

            movement.Arrive(body.position + new Vector2(1.25f, 0f),
                4f, 1.1f, 1.35f);
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movement.Speed, Is.LessThan(cruisingSpeed - 1f),
                "Arrival steering must start braking before the attack boundary.");
            Assert.That(movement.Speed, Is.GreaterThanOrEqualTo(0f));

            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyOrbitSteeringCorrectsItsCombatRing()
        {
            GameObject enemy = new GameObject("EnemyOrbitSteeringProbe");
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            movement.Orbit(new Vector2(4f, 0f), 3f, 2f, 1f);
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movement.CurrentVelocity.x, Is.GreaterThan(0f),
                "An enemy outside its combat ring should orbit while closing in.");
            Assert.That(movement.CurrentVelocity.y, Is.GreaterThan(0f),
                "Orbit steering should preserve tangential movement.");

            movement.StopImmediately();
            body.position = Vector2.zero;
            movement.Orbit(new Vector2(0.8f, 0f), 3f, 2f, 1f);
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movement.CurrentVelocity.x, Is.LessThan(0f),
                "An enemy inside its combat ring should orbit while opening space.");
            Assert.That(movement.CurrentVelocity.y, Is.GreaterThan(0f));

            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyMovementSlidesAlongBlockingSurfaces()
        {
            GameObject enemy = new GameObject("EnemySurfaceSlideProbe");
            enemy.transform.position = Vector2.zero;
            enemy.AddComponent<CircleCollider2D>().radius = 0.35f;
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            GameObject wall = new GameObject("EnemySurfaceSlideWall");
            wall.transform.position = new Vector2(1.05f, 0f);
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(0.35f, 4f);
            Physics2D.SyncTransforms();

            movement.MoveTowards(new Vector2(4f, 2.2f), 4f);
            for (int i = 0; i < 22; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.x, Is.LessThan(0.56f),
                "Surface sliding must not tunnel the enemy through a wall.");
            Assert.That(body.position.y, Is.GreaterThan(0.2f),
                "The enemy should preserve tangential motion along the wall.");
            Assert.That(movement.IsAvoidingObstacle, Is.True);
            Assert.That(Mathf.Abs(movement.LastResolvedVelocity.y),
                Is.GreaterThan(0.1f));

            Object.Destroy(wall);
            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyLingeringHazardKeepsPressureAfterImpact()
        {
            GameObject source = new GameObject("EnemyHazardSourceProbe");
            GameObject player = CreateDamageTargetProbe(
                "EnemyHazardTargetProbe", Vector2.zero,
                out PlayerHealth health);
            player.tag = "Player";
            yield return null;
            float healthBefore = health.CurrentHealth;

            EnemyHazardZone zone = EnemyHazardZone.Spawn(Vector2.zero,
                1.2f, 0.8f, 0.42f, 7f, source,
                new Color(0.3f, 1f, 0.2f, 0.5f));
            Assert.That(zone, Is.Not.Null);
            Assert.That(EnemyHazardZone.ActiveCount, Is.GreaterThanOrEqualTo(1));

            yield return new WaitForSeconds(0.18f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthBefore),
                "A player who remains in the lingering zone should take a pulse.");
            float healthAfterFirstPulse = health.CurrentHealth;
            yield return new WaitForSeconds(0.46f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthAfterFirstPulse),
                "The zone should continue pressuring the same area after impact.");

            Object.Destroy(player);
            Object.Destroy(source);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SlimeSignaturePounceRespectsItsTelegraph()
        {
            EnemyData data = CreateCombatEnemyData("signature_slime_test",
                EnemyArchetype.Slime, 90f, 10f, 2f, 1.2f);
            GameObject enemyObject = CreateEnemyProbeRoot(
                "SignatureSlimeCombatProbe");
            MeleeEnemy slime = enemyObject.AddComponent<MeleeEnemy>();
            ConfigureEnemyProbe(slime, data);
            enemyObject.SetActive(true);

            GameObject player = CreateDamageTargetProbe(
                "SignatureSlimeTargetProbe", new Vector2(1f, 0f),
                out PlayerHealth health);
            yield return null;
            SetEnemyTarget(slime, player.transform);
            EnemySignatureAbility signature =
                slime.GetComponent<EnemySignatureAbility>();
            SetPrivateField(signature, "nextReadyTime", 0f);
            float healthBefore = health.CurrentHealth;

            yield return null;
            Assert.That(signature.IsPerforming, Is.True);
            Assert.That(signature.Skill,
                Is.EqualTo(EnemySignatureSkill.ToxicPounce));
            yield return new WaitForSeconds(0.35f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore),
                "The signature pounce must stay harmless during its telegraph.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthBefore),
                "The pounce should damage a player who remains in the impact area.");

            Object.Destroy(player);
            Object.Destroy(enemyObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeepForestEnemiesUseFootworkAndOrganicTelegraphs()
        {
            EnemyData goblinData = CreateCombatEnemyData(
                "forest_goblin_test", EnemyArchetype.Goblin,
                140f, 12f, 3.2f, 1.2f);
            GameObject goblinObject = CreateEnemyProbeRoot(
                "ForestGoblinFootworkProbe");
            ForestGoblinEnemy goblin =
                goblinObject.AddComponent<ForestGoblinEnemy>();
            ConfigureEnemyProbe(goblin, goblinData);
            goblinObject.SetActive(true);
            GameObject target = CreateDamageTargetProbe(
                "DeepForestTargetProbe", new Vector2(4f, 0f),
                out PlayerHealth unusedHealth);

            yield return null;
            SetEnemyTarget(goblin, target.transform);
            SetPrivateField(goblin.GetComponent<EnemySignatureAbility>(),
                "nextReadyTime", float.PositiveInfinity);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(goblin.CurrentForestState,
                Is.EqualTo(ForestGoblinCombatState.Flanking));
            Assert.That(goblin.GetComponent<EnemyMovement>().CurrentVelocity
                .magnitude, Is.GreaterThan(0.05f));
            Assert.That(goblin.GetComponent<EnemyCharacterAnimator>()
                .LocomotionStyle, Is.EqualTo(EnemyLocomotionStyle.Scurry));

            target.transform.position = new Vector2(0.85f, 0f);
            Physics2D.SyncTransforms();
            SetPrivateField(goblin, "attackPatternIndex", 2);
            SetNextAttackTime(goblin, 0f);
            yield return null;
            Assert.That(goblin.CurrentForestState,
                Is.EqualTo(ForestGoblinCombatState.LeafDashing));
            int footprints = System.Array.FindAll(
                Object.FindObjectsByType<EnemyCombatVfx>(), effect =>
                    effect.gameObject.activeInHierarchy &&
                    effect.gameObject.name == "Forest Dash Footprint").Length;
            Assert.That(footprints, Is.EqualTo(6));
            AssertNoForestLineOrRingWarnings();

            Object.Destroy(goblinObject);
            Object.Destroy(goblinData);
            yield return new WaitForSeconds(0.38f);

            EnemyData archerData = CreateCombatEnemyData(
                "forest_archer_test", EnemyArchetype.Archer,
                100f, 10f, 2.2f, 7f);
            GameObject archerObject = CreateEnemyProbeRoot(
                "ForestArcherFootworkProbe");
            ForestArcherEnemy archer =
                archerObject.AddComponent<ForestArcherEnemy>();
            ConfigureEnemyProbe(archer, archerData);
            archerObject.SetActive(true);
            target.transform.position = new Vector2(5f, 0f);

            yield return null;
            SetEnemyTarget(archer, target.transform);
            SetPrivateField(archer.GetComponent<EnemySignatureAbility>(),
                "nextReadyTime", float.PositiveInfinity);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(archer.CurrentForestState,
                Is.EqualTo(ForestArcherCombatState.Repositioning));
            Assert.That(archer.GetComponent<EnemyMovement>().CurrentVelocity
                .magnitude, Is.GreaterThan(0.02f));
            Assert.That(archer.GetComponent<EnemyCharacterAnimator>()
                .LocomotionStyle, Is.EqualTo(EnemyLocomotionStyle.Skirmish));

            SetPrivateField(archer, "shotPatternIndex", 2);
            SetPrivateField(archer, "nextRainTime", float.PositiveInfinity);
            SetNextAttackTime(archer, 0f);
            yield return null;
            Assert.That(archer.CurrentForestState,
                Is.EqualTo(ForestArcherCombatState.LeafVolley));
            int aimGlints = System.Array.FindAll(
                Object.FindObjectsByType<EnemyCombatVfx>(), effect =>
                    effect.gameObject.activeInHierarchy &&
                    effect.gameObject.name == "Forest Arrow Aim Glint").Length;
            Assert.That(aimGlints, Is.EqualTo(6));
            AssertNoForestLineOrRingWarnings();

            Object.Destroy(target);
            Object.Destroy(archerObject);
            Object.Destroy(archerData);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForestAnimatorSwapsVisibleFeetAndHandActionFrames()
        {
            Texture2D texture = new Texture2D(6, 4, TextureFormat.RGBA32,
                false);
            Color[] pixels = new Color[24];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sheet = Sprite.Create(texture, new Rect(0f, 0f, 6f, 4f),
                new Vector2(0.5f, 0.5f), 2f, 0, SpriteMeshType.FullRect);

            GameObject enemy = new GameObject("ForestArticulatedAnimationProbe");
            enemy.SetActive(false);
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sheet;
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            EnemyCharacterAnimator animator =
                enemy.GetComponent<EnemyCharacterAnimator>();
            if (animator == null)
            {
                animator = enemy.AddComponent<EnemyCharacterAnimator>();
            }
            SetPrivateField(animator, "articulatedSheet", sheet);
            SetPrivateField(animator, "articulatedPixelsPerUnit", 2f);
            enemy.SetActive(true);

            yield return null;
            Assert.That(animator.HasArticulatedFrames, Is.True);

            SetPrivateField(movement, "currentVelocity", Vector2.right * 3f);
            SetPrivateField(animator, "locomotionBlend", 1f);
            InvokeAnimatorUpdate(animator);
            Assert.That(animator.CurrentArticulatedFrame,
                Is.EqualTo(0).Or.EqualTo(1),
                "Walking must visibly alternate the planted foot sprite.");

            animator.PlayAttackPhased(Vector2.right, 0.16f, 0.12f, 0.1f);
            InvokeAnimatorUpdate(animator);
            Assert.That(animator.CurrentArticulatedFrame, Is.EqualTo(2),
                "Attack windup must pull the weapon hand back.");
            yield return new WaitForSeconds(0.18f);
            InvokeAnimatorUpdate(animator);
            Assert.That(animator.CurrentArticulatedFrame, Is.EqualTo(3),
                "Attack release must visibly sweep the weapon hand forward.");

            animator.PlaySkillPhased(Vector2.right, 0.16f, 0.12f, 0.1f,
                new Color(0.55f, 1f, 0.2f));
            InvokeAnimatorUpdate(animator);
            Assert.That(animator.CurrentArticulatedFrame, Is.EqualTo(4),
                "Skill windup must use its dedicated two-hand pose.");
            yield return new WaitForSeconds(0.18f);
            InvokeAnimatorUpdate(animator);
            Assert.That(animator.CurrentArticulatedFrame, Is.EqualTo(5),
                "Skill release must use its dedicated hand-action pose.");

            Object.Destroy(enemy);
            yield return null;
            Object.Destroy(sheet);
            Object.Destroy(texture);
        }

        private static void InvokeAnimatorUpdate(EnemyCharacterAnimator animator)
        {
            MethodInfo update = typeof(EnemyCharacterAnimator).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(animator, null);
        }

        private static void AssertNoForestLineOrRingWarnings()
        {
            int forbidden = System.Array.FindAll(
                Object.FindObjectsByType<EnemyCombatVfx>(), effect =>
                {
                    if (!effect.gameObject.activeInHierarchy) return false;
                    Sprite sprite = effect.GetComponent<SpriteRenderer>().sprite;
                    return effect.gameObject.name == "Enemy Line Warning" ||
                        sprite == RuntimeSpriteFactory.WhiteSprite ||
                        sprite == RuntimeSpriteFactory.RingSprite;
                }).Length;
            Assert.That(forbidden, Is.Zero,
                "Deep Forest enemy skills must use footprints, leaf arcs and " +
                "arrow glints instead of straight lines or circular rings.");
        }

        [UnityTest]
        public IEnumerator SlimeAdvancesWithDiscreteVisibleHops()
        {
            EnemyData data = CreateCombatEnemyData("hopping_slime_test",
                EnemyArchetype.Slime, 90f, 10f, 2f, 1.1f);
            GameObject enemyObject = CreateEnemyProbeRoot(
                "HoppingSlimeMovementProbe");
            SlimeEnemy slime = enemyObject.AddComponent<SlimeEnemy>();
            ConfigureEnemyProbe(slime, data);
            enemyObject.SetActive(true);

            GameObject target = new GameObject("HoppingSlimeTargetProbe");
            target.transform.position = new Vector2(6f, 0f);
            yield return null;
            SetEnemyTarget(slime, target.transform);
            SetPrivateField(slime.GetComponent<EnemySignatureAbility>(),
                "nextReadyTime", float.PositiveInfinity);
            SetPrivateField(slime, "nextHopTime", 0f);
            Vector2 startPosition = slime.transform.position;

            yield return null;
            Assert.That(slime.CurrentState,
                Is.EqualTo(SlimeCombatState.HopWindup));
            EnemyCharacterAnimator animator =
                slime.GetComponent<EnemyCharacterAnimator>();
            Assert.That(animator.IsHopping, Is.True);
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup));

            yield return new WaitForSeconds(0.2f);
            Assert.That(slime.CurrentState,
                Is.EqualTo(SlimeCombatState.Airborne));
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Active));
            yield return new WaitForSeconds(0.34f);

            Assert.That(slime.CompletedHops, Is.GreaterThanOrEqualTo(1));
            Assert.That(slime.transform.position.x,
                Is.GreaterThan(startPosition.x + 0.35f),
                "A completed hop must visibly advance the Slime toward its target.");
            int activeRings = System.Array.FindAll(
                Object.FindObjectsByType<EnemyCombatVfx>(), effect =>
                    effect.gameObject.activeInHierarchy &&
                    effect.GetComponent<SpriteRenderer>().sprite ==
                        RuntimeSpriteFactory.RingSprite).Length;
            Assert.That(activeRings, Is.Zero,
                "Slime landing must use small splashes instead of circular shockwaves.");

            Object.Destroy(target);
            Object.Destroy(enemyObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SlimePoisonSprayUsesAimedFanAfterTelegraph()
        {
            EnemyData data = CreateCombatEnemyData("poison_spray_slime_test",
                EnemyArchetype.Slime, 90f, 10f, 2f, 1.1f);
            GameObject enemyObject = CreateEnemyProbeRoot(
                "PoisonSpraySlimeCombatProbe");
            SlimeEnemy slime = enemyObject.AddComponent<SlimeEnemy>();
            ConfigureEnemyProbe(slime, data);
            enemyObject.SetActive(true);

            GameObject player = CreateDamageTargetProbe(
                "PoisonSprayTargetProbe", new Vector2(0.8f, 0f),
                out PlayerHealth health);
            yield return null;
            SetEnemyTarget(slime, player.transform);
            SetPrivateField(slime.GetComponent<EnemySignatureAbility>(),
                "nextReadyTime", float.PositiveInfinity);
            SetPrivateField(slime, "attackPatternIndex", 1);
            SetPrivateField(slime, "poisonApplicationChance", 1f);
            SetNextAttackTime(slime, 0f);
            float healthBefore = health.CurrentHealth;

            yield return null;
            Assert.That(slime.CurrentState,
                Is.EqualTo(SlimeCombatState.CastingPoisonSpray));
            Assert.That(slime.NewSkillName,
                Is.EqualTo("Phun \u0110\u1ED9c \u0102n M\u00F2n"));
            yield return null;
            EnemyCombatVfx[] warningEffects =
                Object.FindObjectsByType<EnemyCombatVfx>();
            int mistWarnings = System.Array.FindAll(warningEffects, effect =>
                effect.gameObject.activeInHierarchy &&
                effect.gameObject.name == "Poison Mist Warning" &&
                effect.GetComponent<SpriteRenderer>().sprite ==
                    RuntimeSpriteFactory.SoftGlowSprite).Length;
            int lineWarnings = System.Array.FindAll(warningEffects, effect =>
                effect.gameObject.activeInHierarchy &&
                (effect.gameObject.name == "Enemy Line Warning" ||
                 effect.GetComponent<SpriteRenderer>().sprite ==
                    RuntimeSpriteFactory.WhiteSprite)).Length;
            Assert.That(mistWarnings, Is.EqualTo(12),
                "Poison Spray must warn with an organic fan of mist puffs.");
            Assert.That(lineWarnings, Is.Zero,
                "Poison Spray must not draw stretched straight warning lines.");
            yield return new WaitForSeconds(0.15f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore),
                "Poison Spray must remain harmless throughout its aimed warning.");

            yield return new WaitForSeconds(0.12f);
            Assert.That(slime.PoisonSprayCasts, Is.EqualTo(1));
            Assert.That(health.CurrentHealth, Is.LessThan(healthBefore),
                "Poison Spray should damage a target that remains inside its cone.");
            PlayerPoisonStatus poison =
                player.GetComponent<PlayerPoisonStatus>();
            Assert.That(poison, Is.Not.Null);
            Assert.That(poison.IsPoisoned, Is.True,
                "A successful Poison Spray roll must poison the player.");
            Assert.That(poison.Applications, Is.EqualTo(1),
                "One spray cast should resolve its poison chance only once.");
            int activeDroplets = System.Array.FindAll(
                Object.FindObjectsByType<EnemyProjectile>(), projectile =>
                    projectile.gameObject.activeInHierarchy).Length;
            Assert.That(activeDroplets, Is.GreaterThanOrEqualTo(5),
                "The spray must be rendered as a fan of moving poison droplets.");

            float healthBeforePoisonTick = health.CurrentHealth;
            enemyObject.SetActive(false);
            yield return new WaitForSeconds(0.65f);
            Assert.That(poison.CompletedTicks, Is.GreaterThanOrEqualTo(1));
            Assert.That(health.CurrentHealth, Is.LessThan(healthBeforePoisonTick),
                "Poison must continue dealing damage after the initial hit.");

            Object.Destroy(player);
            Object.Destroy(enemyObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExperimentalVitalsIgnoreLowLegacySaveValues()
        {
            GameObject player = new GameObject("ExperimentalVitalsProbe");
            PlayerStats stats = player.AddComponent<PlayerStats>();
            SetPrivateField(stats, "experimentalVitals", true);
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            stats.LoadProgress(100f, 15f, 0, 20f, 5f, 1f, 1, 0);
            health.LoadProgress(stats.MaxHealth);
            yield return null;

            Assert.That(stats.MaxHealth,
                Is.EqualTo(PlayerStats.ExperimentalMaxHealth));
            Assert.That(health.CurrentHealth,
                Is.EqualTo(PlayerStats.ExperimentalMaxHealth));
            Assert.That(stats.MaxMana,
                Is.EqualTo(PlayerStats.ExperimentalMaxMana));
            Assert.That(stats.CurrentMana,
                Is.EqualTo(PlayerStats.ExperimentalMaxMana));

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyAnimatorTracksAuthoredActionPhases()
        {
            GameObject enemy = new GameObject("EnemyActionPhaseProbe");
            enemy.AddComponent<SpriteRenderer>().sprite =
                CreateProbeSprite(Color.magenta);
            enemy.AddComponent<EnemyMovement>();
            EnemyCharacterAnimator animator =
                enemy.GetComponent<EnemyCharacterAnimator>();

            yield return null;
            animator.PlaySkillPhased(Vector2.right, 0.18f, 0.12f, 0.18f,
                new Color(0.8f, 0.25f, 1f));
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup));

            yield return new WaitForSeconds(0.2f);
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Active));

            yield return new WaitForSeconds(0.13f);
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Recovery));

            yield return new WaitForSeconds(0.2f);
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.None));

            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaveMageUsesPursueOrbitAndRetreatBands()
        {
            EnemyData data = CreateCombatEnemyData("cave_mage_test",
                EnemyArchetype.Mage, 120f, 16f, 1.8f, 8f);
            MageEnemy mage = CreateMageProbe(data);
            GameObject target = new GameObject("CaveMageMovementTargetProbe");
            target.transform.position = new Vector3(7f, 0f, 0f);

            yield return null;
            SetEnemyTarget(mage, target.transform);
            SetNextAttackTime(mage, float.PositiveInfinity);
            yield return null;
            Assert.That(mage.CurrentState,
                Is.EqualTo(CaveMageCombatState.Pursuing));

            target.transform.position = new Vector3(4.5f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(mage.CurrentState,
                Is.EqualTo(CaveMageCombatState.Orbiting));

            target.transform.position = new Vector3(2f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(mage.CurrentState,
                Is.EqualTo(CaveMageCombatState.Retreating));

            Object.Destroy(target);
            Object.Destroy(mage.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaveMageGreaterRuneDamagesOnlyAfterTelegraph()
        {
            EnemyData data = CreateCombatEnemyData("cave_mage_rune_test",
                EnemyArchetype.Mage, 120f, 16f, 1.8f, 8f);
            MageEnemy mage = CreateMageProbe(data);
            GameObject player = CreateDamageTargetProbe(
                "CaveMageRuneTargetProbe", new Vector2(4f, 0f),
                out PlayerHealth health);

            yield return null;
            SetEnemyTarget(mage, player.transform);
            SetPrivateField(mage, "castPatternIndex", 2);
            SetNextAttackTime(mage, 0f);
            float healthBeforeAttack = health.CurrentHealth;

            yield return null;
            Assert.That(mage.CurrentState,
                Is.EqualTo(CaveMageCombatState.Telegraphing));
            EnemyCharacterAnimator animator =
                mage.GetComponent<EnemyCharacterAnimator>();
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup));
            yield return new WaitForSeconds(0.6f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBeforeAttack),
                "Greater Rune must not damage during its telegraph.");
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup),
                "The release pose must not play before Greater Rune damage.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthBeforeAttack),
                "Greater Rune should damage after its locked telegraph expires.");
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Active)
                    .Or.EqualTo(EnemyActionPhase.Recovery));

            Object.Destroy(player);
            Object.Destroy(mage.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaveTankPursuesCirclesAndBracesByDistance()
        {
            EnemyData data = CreateCombatEnemyData("cave_tank_test",
                EnemyArchetype.Tank, 360f, 25f, 1f, 1.5f);
            TankEnemy tank = CreateTankProbe(data);
            GameObject target = new GameObject("CaveTankMovementTargetProbe");
            target.transform.position = new Vector3(4f, 0f, 0f);

            yield return null;
            SetEnemyTarget(tank, target.transform);
            SetNextAttackTime(tank, float.PositiveInfinity);
            yield return null;
            Assert.That(tank.CurrentState,
                Is.EqualTo(TankCombatState.Pursuing));

            target.transform.position = new Vector3(2f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(tank.CurrentState,
                Is.EqualTo(TankCombatState.Circling));

            target.transform.position = new Vector3(1f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(tank.CurrentState,
                Is.EqualTo(TankCombatState.Bracing));

            Object.Destroy(target);
            Object.Destroy(tank.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaveTankGroundSlamDamagesOnlyAfterTelegraph()
        {
            EnemyData data = CreateCombatEnemyData("cave_tank_slam_test",
                EnemyArchetype.Tank, 360f, 25f, 1f, 1.5f);
            TankEnemy tank = CreateTankProbe(data);
            GameObject player = CreateDamageTargetProbe(
                "CaveTankSlamTargetProbe", new Vector2(1f, 0f),
                out PlayerHealth health);

            yield return null;
            SetEnemyTarget(tank, player.transform);
            SetPrivateField(tank, "attackPatternIndex", 2);
            SetNextAttackTime(tank, 0f);
            float healthBeforeAttack = health.CurrentHealth;

            yield return null;
            Assert.That(tank.CurrentState,
                Is.EqualTo(TankCombatState.Telegraphing));
            EnemyCharacterAnimator animator =
                tank.GetComponent<EnemyCharacterAnimator>();
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup));
            yield return new WaitForSeconds(0.5f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBeforeAttack),
                "Ground Slam must not damage during its 0.78-second telegraph.");
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Windup),
                "The slam pose must stay in anticipation until impact.");

            yield return new WaitForSeconds(0.35f);
            Assert.That(health.CurrentHealth, Is.LessThan(healthBeforeAttack),
                "Ground Slam should damage after its locked telegraph expires.");
            Assert.That(animator.CurrentActionPhase,
                Is.EqualTo(EnemyActionPhase.Active)
                    .Or.EqualTo(EnemyActionPhase.Recovery));

            Object.Destroy(player);
            Object.Destroy(tank.gameObject);
            Object.Destroy(data);
            yield return null;
        }

        private static EnemyData CreateCombatEnemyData(string enemyId,
            EnemyArchetype archetype, float health, float damage,
            float moveSpeed, float attackRange)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(data, "enemyId", enemyId);
            SetPrivateField(data, "archetype", archetype);
            SetPrivateField(data, "maxHealth", health);
            SetPrivateField(data, "damage", damage);
            SetPrivateField(data, "moveSpeed", moveSpeed);
            SetPrivateField(data, "detectionRange", 10f);
            SetPrivateField(data, "attackRange", attackRange);
            SetPrivateField(data, "attackCooldown", 2.4f);
            return data;
        }

        private static MageEnemy CreateMageProbe(EnemyData data)
        {
            GameObject enemy = CreateEnemyProbeRoot("CaveMageCombatProbe");
            MageEnemy mage = enemy.AddComponent<MageEnemy>();
            ConfigureEnemyProbe(mage, data);
            enemy.SetActive(true);
            return mage;
        }

        private static TankEnemy CreateTankProbe(EnemyData data)
        {
            GameObject enemy = CreateEnemyProbeRoot("CaveTankCombatProbe");
            TankEnemy tank = enemy.AddComponent<TankEnemy>();
            ConfigureEnemyProbe(tank, data);
            enemy.SetActive(true);
            return tank;
        }

        private static GameObject CreateEnemyProbeRoot(string name)
        {
            GameObject enemy = new GameObject(name);
            enemy.SetActive(false);
            enemy.transform.position = Vector3.zero;
            enemy.AddComponent<CircleCollider2D>().radius = 0.55f;
            return enemy;
        }

        private static void ConfigureEnemyProbe(EnemyBase enemy,
            EnemyData data)
        {
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            SetPrivateField(typeof(EnemyBase), enemy, "data", data);
            SetNextAttackTime(enemy, float.PositiveInfinity);
        }

        private static GameObject CreateDamageTargetProbe(string name,
            Vector2 position, out PlayerHealth health)
        {
            GameObject player = new GameObject(name);
            player.transform.position = position;
            player.AddComponent<PlayerStats>();
            health = player.AddComponent<PlayerHealth>();
            return player;
        }

        private static EnemyData CreateCrystalScorpionData()
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(data, "enemyId", "crystal_scorpion_test");
            SetPrivateField(data, "archetype", EnemyArchetype.CrystalScorpion);
            SetPrivateField(data, "maxHealth", 210f);
            SetPrivateField(data, "damage", 18f);
            SetPrivateField(data, "moveSpeed", 2.5f);
            SetPrivateField(data, "detectionRange", 10f);
            SetPrivateField(data, "attackRange", 4.6f);
            SetPrivateField(data, "attackCooldown", 2.4f);
            return data;
        }

        private static CrystalScorpionEnemy CreateCrystalScorpionProbe(
            EnemyData data)
        {
            GameObject enemy = new GameObject("CrystalScorpionCombatProbe");
            enemy.SetActive(false);
            enemy.transform.position = Vector3.zero;
            enemy.AddComponent<CircleCollider2D>().radius = 0.55f;
            CrystalScorpionEnemy scorpion =
                enemy.AddComponent<CrystalScorpionEnemy>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            SetPrivateField(typeof(EnemyBase), scorpion, "data", data);
            SetNextAttackTime(scorpion, float.PositiveInfinity);
            enemy.SetActive(true);
            return scorpion;
        }

        private static void SetEnemyTarget(EnemyBase enemy,
            Transform target)
        {
            SetPrivateField(typeof(EnemyBase), enemy,
                "<Target>k__BackingField", target);
        }

        private static void SetNextAttackTime(EnemyBase enemy,
            float value)
        {
            SetPrivateField(typeof(EnemyBase), enemy,
                "<NextAttackTime>k__BackingField", value);
        }

        private static void SetPrivateField(object target, string fieldName,
            object value)
        {
            SetPrivateField(target.GetType(), target, fieldName, value);
        }

        private static void SetPrivateField(System.Type declaringType,
            object target, string fieldName, object value)
        {
            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            FieldInfo field = declaringType.GetField(fieldName, Flags);
            Assert.That(field, Is.Not.Null,
                $"Expected private field '{fieldName}' on {declaringType.Name}.");
            field.SetValue(target, value);
        }

        private static PlayerMovement CreateMovementProbe(string name,
            out Rigidbody2D body, out CircleCollider2D movementCollider)
        {
            GameObject player = new GameObject(name);
            player.AddComponent<PlayerStats>();
            movementCollider = player.AddComponent<CircleCollider2D>();
            movementCollider.radius = 0.48f;
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            body = player.GetComponent<Rigidbody2D>();
            body.position = Vector2.zero;
            return movement;
        }

        private static BoxCollider2D CreateStaticBoxCollider(string name,
            Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obstacle = new GameObject(name);
            obstacle.transform.SetParent(parent);
            obstacle.transform.position = position;
            BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }

        private static CircleCollider2D CreateStaticCircleCollider(string name,
            Transform parent, Vector2 position, float radius)
        {
            GameObject obstacle = new GameObject(name);
            obstacle.transform.SetParent(parent);
            obstacle.transform.position = position;
            CircleCollider2D collider = obstacle.AddComponent<CircleCollider2D>();
            collider.radius = radius;
            return collider;
        }

        private static void SetMovementSpeed(PlayerMovement movement,
            float speed)
        {
            const BindingFlags Flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            FieldInfo speedField = typeof(PlayerStats).GetField(
                "moveSpeed", Flags);
            Assert.That(speedField, Is.Not.Null);
            speedField.SetValue(movement.GetComponent<PlayerStats>(), speed);
        }

        private static void SetClickDestination(PlayerMovement movement,
            Vector2 destination)
        {
            Assert.That(movement.TrySetClickDestination(destination), Is.True,
                "The click destination should produce a reachable cardinal route.");
        }

        private static bool ReadClickDestinationState(PlayerMovement movement)
        {
            return movement.IsFollowingClickPath;
        }

        private static Sprite CreateProbeSprite(Color color)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f), 2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CleanupRuntimeProbes();

            if (GameProgressManager.Instance != null)
            {
                Object.Destroy(GameProgressManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey(TestSaveKey);
            PlayerPrefs.DeleteKey(TestGoldGrantKey);
            PlayerPrefs.Save();
            yield return null;
        }

        private static void CleanupRuntimeProbes()
        {
            PlayerMovement[] movementProbes =
                Object.FindObjectsByType<PlayerMovement>(
                    FindObjectsInactive.Include);
            foreach (PlayerMovement movement in movementProbes)
            {
                if (movement != null &&
                    (movement.name.EndsWith("Probe") ||
                     movement.name.StartsWith("OrganicCorridorPhase_")))
                {
                    Object.Destroy(movement.gameObject);
                }
            }

            string[] obstacleRoots =
            {
                "UpperMazeWall",
                "LowerMazeWall",
                "MixedOrganicCorridor",
                "LongDetourWall",
                "GridAlignedClickObstacle",
                "CircleClickObstacle",
                "ClosedOrganicNavigationBoundary",
                "DynamicRouteObstacle",
                "MovingTraversalCacheCorridor",
                "AutoAttackTargetProbe",
                "AutoSkillTargetProbe",
                "FireballProjectile2D",
                "InteractionTriggerProbe",
                "IceDashObstacle",
                "CrystalScorpionCombatProbe",
                "CrystalScorpionMovementTargetProbe",
                "CrystalScorpionSpikeTargetProbe",
                "CaveMageCombatProbe",
                "CaveMageMovementTargetProbe",
                "CaveMageRuneTargetProbe",
                "CaveTankCombatProbe",
                "CaveTankMovementTargetProbe",
                "CaveTankSlamTargetProbe",
                "SmoothEnemySteeringProbe",
                "EnemyArrivalSteeringProbe",
                "EnemyOrbitSteeringProbe",
                "EnemySurfaceSlideProbe",
                "EnemySurfaceSlideWall",
                "EnemyHazardSourceProbe",
                "EnemyHazardTargetProbe",
                "Enemy Lingering Hazard",
                "SignatureSlimeCombatProbe",
                "SignatureSlimeTargetProbe",
                "ForestGoblinFootworkProbe",
                "ForestArcherFootworkProbe",
                "DeepForestTargetProbe",
                "EnemyActionPhaseProbe"
            };
            foreach (string rootName in obstacleRoots)
            {
                GameObject root = GameObject.Find(rootName);
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }

            EnemyCombatVfx[] combatEffects =
                Object.FindObjectsByType<EnemyCombatVfx>(
                    FindObjectsInactive.Include);
            foreach (EnemyCombatVfx effect in combatEffects)
            {
                if (effect != null && effect.gameObject.activeSelf)
                {
                    effect.gameObject.SetActive(false);
                }
            }
        }
    }
}
