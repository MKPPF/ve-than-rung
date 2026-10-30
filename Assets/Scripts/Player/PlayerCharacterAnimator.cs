using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Player
{
    /// <summary>Samples the interpolated body at presentation time; short directional settling avoids idle flashes between physics ticks.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerCharacterAnimator : MonoBehaviour
    {
        private enum WalkFacing
        {
            Down,
            Up,
            Left,
            Right
        }

        [Header("Animation Frames")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] walkLeftFrames;
        [SerializeField] private Sprite[] walkRightFrames;
        [SerializeField] private Sprite[] walkUpFrames;
        [SerializeField] private Sprite[] walkDownFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] skillCastFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] deathFrames;

        [Header("Direction")]
        [SerializeField, Min(0.01f)] private float walkStartSpeed = 0.08f;
        [SerializeField, Min(0.01f)] private float walkStopSpeed = 0.04f;
        [Header("Locomotion Continuity")]
        [SerializeField] private bool smoothLocomotion;
        [SerializeField, Range(0.04f, 0.2f)] private float stopSettleDuration = 0.1f;

        [Header("Timing")]
        [SerializeField, Min(0.15f)] private float attackDuration = 0.34f;
        [SerializeField, Min(0.15f)] private float skillCastDuration = 0.48f;
        [SerializeField, Min(0.1f)] private float hitDuration = 0.18f;
        [SerializeField, Min(0.2f)] private float deathDuration = 0.62f;
        private PlayerMovement movement;
        private SpriteRenderer visualRenderer;
        private float attackStartTime;
        private float attackEndTime;
        private float castStartTime;
        private float castEndTime;
        private float castWindup = 0.12f;
        private Vector2 castDirection;
        private Vector3 castRestPosition,castRestScale;
        private bool castPoseApplied;
        private TopDownGame.Skills.SwordComboProfile swordProfile;
        private Material swordPreviousMaterial;
        private float swordPoseStart,swordPoseEnd;
        private int swordBeat;
        private bool swordWindup;
        private TopDownGame.Skills.SkillPresentationProfile magicProfile;
        private Material magicPreviousMaterial;

        public void PlayPaintedCast(TopDownGame.Skills.SkillPresentationProfile profile,Vector2 aim,float duration,float windup)
        {
            PlaySkillCast(aim,duration,windup);
            if(profile==null || profile.ActorFrames==null || profile.ActorFrames.Length!=6 || visualRenderer==null) return;
            if(magicProfile==null) magicPreviousMaterial=visualRenderer.sharedMaterial;
            magicProfile=profile;visualRenderer.sharedMaterial=profile.ActorMaterial;
        }
        public void CancelMagicPose()
        {
            if(magicProfile==null) return;
            if(visualRenderer!=null) visualRenderer.sharedMaterial=magicPreviousMaterial;
            magicProfile=null;
        }

        public void PlaySwordWindup(TopDownGame.Skills.SwordComboProfile profile,Vector2 aim,float duration)
        {
            SetSwordPose(profile,0,aim,duration,true);
        }
        public void PlaySwordStrike(TopDownGame.Skills.SwordComboProfile profile,int beat,Vector2 aim,float duration)
        {
            SetSwordPose(profile,beat,aim,duration,false);
        }
        private void SetSwordPose(TopDownGame.Skills.SwordComboProfile profile,int beat,Vector2 aim,float duration,bool windup)
        {
            if(dying || profile==null || profile.ActorFrames.Length!=6) return;
            ResolveVisualRenderer();if(visualRenderer==null) return;
            if(swordProfile==null) swordPreviousMaterial=visualRenderer.sharedMaterial;
            swordProfile=profile;swordBeat=beat;swordWindup=windup;
            swordPoseStart=Time.time;swordPoseEnd=Time.time+duration;
            FaceDirection(aim);
            visualRenderer.sharedMaterial=profile.ActorMaterial;
            castEndTime=attackEndTime=0;
        }
        public void CancelSwordPose()
        {
            if(swordProfile==null) return;
            if(visualRenderer!=null) visualRenderer.sharedMaterial=swordPreviousMaterial;
            swordProfile=null;swordPoseEnd=0;
            hasPresentedWalk=false;settling=false;
        }
        private float hitStartTime;
        private float hitEndTime;
        private float deathStartTime;
        private Color restColor = Color.white;
        private bool dying;
        private bool walking;
        private WalkFacing walkFacing = WalkFacing.Down;
        private float displayedGaitPhase;
        private Vector2 lastPresentedPosition;
        private Vector2 presentedDirection;
        private float presentedDistance;
        private float lastPresentedMotionTime;
        private Sprite[] lastWalkFrames;
        private bool hasPresentedWalk;
        private bool settling;
        private float settleStartTime;
        private float settleStartPhase;
        private float settleAdvance;
        private float shotFacingUntil;
        private Vector2 shotDirection;

        public void PlayShot(Vector2 direction)
        {
            if (dying) return;
            ResolveVisualRenderer();
            shotDirection = direction;
            shotFacingUntil = Time.time + 0.2f;
            FaceDirection(direction);
        }

        public bool IsWalking => walking;
        private Vector2 facingDirection = Vector2.down;
        private Vector2 dodgeDirection;
        private float dodgeStartTime, dodgeEndTime;
        private bool dodgePoseApplied;
        private Vector3 dodgeRestPosition, dodgeRestScale;
        private Quaternion dodgeRestRotation;
        public Vector2 FacingDirection => facingDirection;
        public float PresentedGaitPhase => displayedGaitPhase;

        public float SkillCastDuration => skillCastDuration;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            lastPresentedPosition = transform.position;
            ResolveVisualRenderer();
            if (visualRenderer != null && idleSprite == null)
            {
                idleSprite = visualRenderer.sprite;
            }

            if (visualRenderer != null)
            {
                restColor = visualRenderer.color;
            }

            ShowFirstAvailableIdle();
        }

        private void Update()
        {
            if (!smoothLocomotion) UpdateVisual();
        }

        private void LateUpdate()
        {
            if(smoothLocomotion)
            {
                SamplePresentation();
                if (Time.deltaTime > 0f) UpdateVisual();
            }
            ApplyCastPose();
            ApplyDodgePose();
        }

        public void PlayDodge(Vector2 direction, float duration)
        {
            ResolveVisualRenderer();
            if (dying || visualRenderer == null) return;
            CancelDodgePose();
            dodgeDirection = direction.normalized;
            FaceDirection(dodgeDirection);
            dodgeStartTime = Time.time;
            dodgeEndTime = Time.time + duration;
            var visual = visualRenderer.transform;
            dodgeRestPosition = visual.localPosition;
            dodgeRestScale = visual.localScale;
            dodgeRestRotation = visual.localRotation;
            attackEndTime = castEndTime = hitEndTime = 0f;
        }

        private void ApplyDodgePose()
        {
            if (visualRenderer == null) return;
            if (Time.time >= dodgeEndTime || dying) { CancelDodgePose(); return; }
            float progress = Mathf.InverseLerp(dodgeStartTime, dodgeEndTime, Time.time);
            float lean = Mathf.Sin(progress * Mathf.PI);
            var visual = visualRenderer.transform;
            visual.localPosition = dodgeRestPosition + (Vector3)(dodgeDirection * lean * 0.08f);
            visual.localScale = Vector3.Scale(dodgeRestScale, new Vector3(1f + lean * 0.06f, 1f - lean * 0.12f, 1f));
            visual.localRotation = dodgeRestRotation * Quaternion.Euler(0, 0, -dodgeDirection.x * lean * 12f);
            dodgePoseApplied = true;
        }

        public void CancelDodgePose()
        {
            dodgeEndTime = Time.time;
            if (!dodgePoseApplied || visualRenderer == null) return;
            var visual = visualRenderer.transform;
            visual.localPosition = dodgeRestPosition;
            visual.localScale = dodgeRestScale;
            visual.localRotation = dodgeRestRotation;
            visualRenderer.color = restColor;
            dodgePoseApplied = false;
        }

        private void ApplyCastPose()
        {
            if(visualRenderer==null) return;
            var visual=visualRenderer.transform;
            if(Time.time<castEndTime && !dying)
            {
                float age=Time.time-castStartTime;
                float wind=Mathf.Clamp01(age/Mathf.Max(0.01f,castWindup));
                float release=Mathf.Clamp01((age-castWindup)/Mathf.Max(0.01f,castEndTime-castStartTime-castWindup));
                float lean=age<castWindup ? -Mathf.Sin(wind*Mathf.PI*.5f) : Mathf.Sin(release*Mathf.PI)*.8f;
                visual.localPosition=castRestPosition+(Vector3)(castDirection*lean*.085f);
                visual.localRotation=Quaternion.Euler(0,0,-castDirection.x*lean*9);
                visual.localScale=Vector3.Scale(castRestScale,new Vector3(1-lean*.025f,1+lean*.03f,1));
                castPoseApplied=true;
            }
            else if(castPoseApplied)
            {
                visual.localPosition=castRestPosition;visual.localScale=castRestScale;visual.localRotation=Quaternion.identity;
                castPoseApplied=false;
            }
        }

        private void OnEnable()
        {
            lastPresentedPosition = transform.position;
            presentedDistance = 0f;
            settling = false;
            walking = false;
        }

        private void SamplePresentation()
        {
            Vector2 position = transform.position;
            Vector2 displacement = position - lastPresentedPosition;
            lastPresentedPosition = position;
            presentedDistance = displacement.magnitude;
            if (Time.deltaTime <= 0f || presentedDistance > 1f)
            {
                presentedDistance = 0f;
                return;
            }
            if (presentedDistance > 0.00005f)
            {
                presentedDirection = displacement.normalized;
                lastPresentedMotionTime = Time.time;
            }
        }

        private void UpdateVisual()
        {
            ResolveVisualRenderer();
            if (visualRenderer == null)
            {
                return;
            }

            ThanhGiongHeroController thanhGiong = GetComponent<ThanhGiongHeroController>();
            if (thanhGiong != null && thanhGiong.IsThanhGiongActive)
            {
                return;
            }

            if(magicProfile!=null && (dying || Time.time>=castEndTime || Time.time<hitEndTime)) CancelMagicPose();
            if(swordProfile!=null)
            {
                if(!dying && Time.time<swordPoseEnd && Time.time>=hitEndTime)
                {
                    float t=Mathf.InverseLerp(swordPoseStart,swordPoseEnd,Time.time);
                    int phase=Mathf.Min(2,Mathf.FloorToInt(t*3));
                    int frame=swordWindup ? (t<.65f ? 0 : 1) :
                        swordBeat%2==0 ? new[]{2,3,4}[phase] : new[]{5,1,0}[phase];
                    visualRenderer.color=restColor;
                    ShowSprite(swordProfile.ActorFrames[frame]);
                    return;
                }
                CancelSwordPose();
            }

            if (dying)
            {
                visualRenderer.color = restColor;
                ShowTimedFrames(deathFrames, deathStartTime,
                    deathStartTime + deathDuration);
                return;
            }

            if (Time.time < dodgeEndTime)
            {
                // Pose and facing remain owned here; a dodge uses the existing directional frames.
                presentedDirection = dodgeDirection;
                Sprite[] frames = ResolveWalkFrames();
                float progress = Mathf.InverseLerp(dodgeStartTime, dodgeEndTime, Time.time);
                if (HasFrames(frames)) ShowSprite(frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length))]);
                visualRenderer.color = Color.Lerp(restColor, new Color(0.45f, 0.9f, 1f), 0.32f);
                return;
            }

            if (Time.time < hitEndTime)
            {
                visualRenderer.color = Color.Lerp(Color.white,
                    new Color(1f, 0.28f, 0.22f), 0.52f);
            }
            else if (Time.time < castEndTime)
            {
                float castProgress = Mathf.InverseLerp(castStartTime,
                    castEndTime, Time.time);
                float glow = 0.22f + Mathf.Sin(castProgress * Mathf.PI) * 0.25f;
                visualRenderer.color = Color.Lerp(restColor,
                    new Color(0.35f, 0.85f, 1f), glow);
            }
            else
            {
                visualRenderer.color = restColor;
            }

            if (Time.time < hitEndTime && HasFrames(hitFrames))
            {
                ShowTimedFrames(hitFrames, hitStartTime, hitEndTime);
                return;
            }

            if (Time.time < attackEndTime && HasFrames(attackFrames))
            {
                ShowTimedFrames(attackFrames, attackStartTime, attackEndTime);
                return;
            }

            if (Time.time < castEndTime)
            {
                if(magicProfile!=null)
                {
                    float age=Time.time-castStartTime;
                    int frame=age<castWindup*.6f ? 0 : age<castWindup ? 1 :
                        age<castWindup+.12f ? magicProfile.ReleasePose : 5;
                    visualRenderer.color=restColor;
                    ShowSprite(magicProfile.ActorFrames[frame]);
                }
                else if (HasFrames(skillCastFrames))
                {
                    float age = Time.time - castStartTime;
                    int frame = age < castWindup ? 0 :
                        age < castWindup + 0.09f ? Mathf.Min(1, skillCastFrames.Length - 1) : skillCastFrames.Length - 1;
                    ShowSprite(skillCastFrames[frame]);
                }
                else
                {
                    ShowFirstAvailableIdle();
                }

                return;
            }

            UpdateWalkingState();
            Sprite[] activeWalkFrames = walking ? ResolveWalkFrames() : null;
            if (!walking || !HasFrames(activeWalkFrames))
            {
                ShowStandingOrIdle();
                return;
            }

            int walkFrameIndex = ResolveWalkFrameIndex(displayedGaitPhase,
                activeWalkFrames.Length);
            if (smoothLocomotion)
            {
                lastWalkFrames = activeWalkFrames;
                hasPresentedWalk = true;
                settling = false;
            }
            ShowSprite(activeWalkFrames[walkFrameIndex]);
        }

        public void PlayAttack(Vector2 direction, float duration = 0f)
        {
            if (dying) return;
            ResolveVisualRenderer();
            FaceDirection(direction);
            attackStartTime = Time.time;
            attackEndTime = Time.time + (duration > 0f
                ? Mathf.Max(0.16f, duration)
                : attackDuration);
            castEndTime = 0f;
            if (HasFrames(attackFrames)) ShowSprite(attackFrames[0]);
        }

        public void PlaySkillCast()
        {
            Vector2 aim = GetComponent<PlayerCombat>()?.AimDirection ?? Vector2.right;
            PlaySkillCast(aim, skillCastDuration, 0.12f);
        }

        public void PlaySkillCast(Vector2 direction, float duration, float windup)
        {
            if (dying) return;
            ResolveVisualRenderer();
            FaceDirection(direction);
            castWindup = windup;
            castDirection=direction;
            if(visualRenderer!=null && !castPoseApplied)
            {
                castRestPosition=visualRenderer.transform.localPosition;
                castRestScale=visualRenderer.transform.localScale;
            }
            castStartTime = Time.time;
            castEndTime = Time.time + duration;
            hasPresentedWalk = false;
            settling = false;
            attackEndTime = 0f;
            if (HasFrames(skillCastFrames)) ShowSprite(skillCastFrames[0]);
            else ShowFirstAvailableIdle();
        }

        public void PlayHit(Vector2 direction)
        {
            if (dying) return;
            ResolveVisualRenderer();
            FaceDirection(direction);
            hitStartTime = Time.time;
            hitEndTime = Time.time + hitDuration;
            attackEndTime = 0f;
            castEndTime = 0f;
            if (HasFrames(hitFrames)) ShowSprite(hitFrames[0]);
        }

        public void PlayDeath()
        {
            if (dying) return;
            dying = true;
            CancelDodgePose();
            ResolveVisualRenderer();
            deathStartTime = Time.time;
            attackEndTime = castEndTime = hitEndTime = 0f;
            if (HasFrames(deathFrames)) ShowSprite(deathFrames[0]);
        }

        private void ResolveVisualRenderer()
        {
            if (visualRenderer != null)
            {
                return;
            }

            visualRenderer = movement != null ? movement.VisualRenderer : null;
            if (visualRenderer == null)
            {
                Transform visual = transform.Find("MovementVisual");
                visualRenderer = visual != null
                    ? visual.GetComponent<SpriteRenderer>()
                    : GetComponent<SpriteRenderer>();
            }

        }

        private void UpdateWalkingState()
        {
            if (movement == null)
            {
                walking = false;
                return;
            }

            if (smoothLocomotion)
            {
                if (!movement.enabled || movement.IsMovementLocked)
                {
                    walking = false;
                    return;
                }
                float renderedSpeed = presentedDistance / Mathf.Max(Time.deltaTime, 0.00001f);
                bool movingNow = renderedSpeed >= (walking ? walkStopSpeed : walkStartSpeed);
                // A display frame can land exactly on a fixed-step boundary.
                // Briefly retain the current pose, never insert front-facing
                // Idle_00 into an otherwise continuous walk.
                walking = movingNow || (walking && movement.WantsToMove &&
                    Time.time - lastPresentedMotionTime < Time.fixedDeltaTime * 1.5f);
                if (movingNow)
                    displayedGaitPhase = Mathf.Repeat(displayedGaitPhase +
                        presentedDistance * movement.GaitCyclesPerUnit, 1f);
                return;
            }
            float speed = movement.CurrentVelocity.magnitude;
            bool reachedWalkingSpeed = walking
                ? speed > walkStopSpeed
                : speed >= walkStartSpeed;
            walking = movement.IsMoving && reachedWalkingSpeed;
            if (walking)
            {
                displayedGaitPhase = movement.GaitPhase;
            }
        }

        private void FaceMousePointer()
        {
            if (visualRenderer == null || Mouse.current == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 mouseWorld = camera.ScreenToWorldPoint(
                Mouse.current.position.ReadValue());
            float horizontalDirection = mouseWorld.x - transform.position.x;
            if (Mathf.Abs(horizontalDirection) > 0.05f)
            {
                visualRenderer.flipX = horizontalDirection < 0f;
            }
        }

        private void FaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f) facingDirection = direction.normalized;
            if (visualRenderer != null && Mathf.Abs(direction.x) > 0.05f)
            {
                visualRenderer.flipX = direction.x < 0f;
            }
        }

        private Sprite[] ResolveWalkFrames()
        {
            // PlayerMovement only emits cardinal intent. Quantizing again here
            // keeps animation cardinal even if an external impulse reports a
            // diagonal observed velocity near a state transition.
            // Face the direction that was actually resolved by the most recent
            // physics step. At an L-shaped click corner, MoveDirection already
            // points down the next leg while Rigidbody interpolation is still
            // displaying the final pixels of the previous leg; using observed
            // velocity keeps the directional sheet synchronized with the body.
            Vector2 direction = Time.time < shotFacingUntil ? shotDirection :
                (smoothLocomotion ? presentedDirection : movement.CurrentVelocity);
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > 0.001f)
            {
                walkFacing = direction.x < 0f
                    ? WalkFacing.Left
                    : WalkFacing.Right;
            }
            else if (Mathf.Abs(direction.y) > 0.001f)
            {
                walkFacing = direction.y < 0f
                    ? WalkFacing.Down
                    : WalkFacing.Up;
            }
            if (direction.sqrMagnitude > 0.001f)
                facingDirection = walkFacing == WalkFacing.Left ? Vector2.left : walkFacing == WalkFacing.Right ? Vector2.right :
                    walkFacing == WalkFacing.Up ? Vector2.up : Vector2.down;

            Sprite[] directionalFrames;
            switch (walkFacing)
            {
                case WalkFacing.Up:
                    directionalFrames = walkUpFrames;
                    break;
                case WalkFacing.Left:
                    directionalFrames = walkLeftFrames;
                    break;
                case WalkFacing.Right:
                    directionalFrames = walkRightFrames;
                    break;
                default:
                    directionalFrames = walkDownFrames;
                    break;
            }

            if (HasFrames(directionalFrames))
            {
                visualRenderer.flipX = walkFacing == WalkFacing.Left;
                return directionalFrames;
            }

            // If only one horizontal set is assigned, mirror it for the other
            // side before falling back to the generic run animation.
            if (walkFacing == WalkFacing.Right && HasFrames(walkLeftFrames))
            {
                visualRenderer.flipX = false;
                return walkLeftFrames;
            }

            if (walkFacing == WalkFacing.Left && HasFrames(walkRightFrames))
            {
                visualRenderer.flipX = true;
                return walkRightFrames;
            }

            visualRenderer.flipX = direction.x < 0f;
            return walkFrames;
        }

        private void ShowTimedFrames(Sprite[] frames, float startTime, float endTime)
        {
            if (!HasFrames(frames)) return;
            float progress = Mathf.Clamp01(Mathf.InverseLerp(startTime, endTime, Time.time));
            int index = Mathf.Min(Mathf.FloorToInt(progress * frames.Length),
                frames.Length - 1);
            ShowSprite(frames[index]);
        }

        private static int ResolveWalkFrameIndex(float phase, int frameCount)
        {
            // All authored poses describe equal gait intervals. Unequal weights
            // made frame exposure alternate between roughly 4 and 6 display
            // refreshes, so an otherwise steady loop visibly beat and hitched.
            return Mathf.FloorToInt(Mathf.Repeat(phase, 1f) * frameCount) %
                frameCount;
        }

        private void ShowStandingOrIdle()
        {
            if (Time.time < shotFacingUntil)
            {
                Sprite[] facingFrames = ResolveWalkFrames();
                if (HasFrames(facingFrames))
                {
                    // Brief firing stance; always release back to neutral idle.
                    ShowSprite(facingFrames[Mathf.Min(facingFrames.Length - 1,
                        facingFrames.Length / 4)]);
                    return;
                }
            }
            if (smoothLocomotion && hasPresentedWalk && HasFrames(lastWalkFrames))
            {
                if (!settling)
                {
                    settling = true;
                    settleStartTime = Time.time;
                    settleStartPhase = displayedGaitPhase;
                    float restingPose = NearestRestPhase(displayedGaitPhase);
                    settleAdvance = Mathf.DeltaAngle(displayedGaitPhase * 360f,
                        restingPose * 360f) / 360f;
                }
                float t = Mathf.Clamp01((Time.time - settleStartTime) /
                    Mathf.Max(0.01f, stopSettleDuration));
                if (t >= 1f)
                {
                    // Passing poses still contain a lifted foot. They are only
                    // a brief transition, never a persistent standing pose.
                    hasPresentedWalk = false;
                    settling = false;
                    ShowFirstAvailableIdle();
                    return;
                }
                displayedGaitPhase = Mathf.Repeat(settleStartPhase + settleAdvance *
                    Mathf.SmoothStep(0f, 1f, t), 1f);
                visualRenderer.flipX = walkFacing == WalkFacing.Left;
                ShowSprite(lastWalkFrames[ResolveWalkFrameIndex(displayedGaitPhase + 0.00001f, lastWalkFrames.Length)]);
                return;
            }
            // Never keep a contact/recoil frame after movement stops. The
            // authored neutral sprite is the character's actual idle pose.
            ShowFirstAvailableIdle();
        }

        public static float NearestRestPhase(float phase)
        {
            phase = Mathf.Repeat(phase, 1f);
            // Passing poses are used only during the short stop transition;
            // the authored neutral idle must take over once settling ends.
            return phase < 0.5f ? 0.25f : 0.75f;
        }

        private void ShowFirstAvailableIdle()
        {
            if (HasFrames(idleFrames)) ShowSprite(idleFrames[0]);
            else ShowSprite(idleSprite);
        }

        private void ShowSprite(Sprite sprite)
        {
            if (visualRenderer != null && sprite != null &&
                visualRenderer.sprite != sprite)
            {
                visualRenderer.sprite = sprite;
            }
        }

        private static bool HasFrames(Sprite[] frames)
        {
            return frames != null && frames.Length > 0;
        }
    }
}
