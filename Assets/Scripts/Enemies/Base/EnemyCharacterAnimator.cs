using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum EnemyActionPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    public enum EnemyLocomotionStyle
    {
        Bounce,
        Scurry,
        Skirmish,
        Float,
        Stomp,
        Skitter,
        RiftGlide,
        Colossus
    }

    /// <summary>
    /// Adds weight and readable combat poses to single-sprite enemies without
    /// requiring a separate Animator Controller for every enemy type.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class EnemyCharacterAnimator : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField, Min(0.1f)] private float strideFrequency = 0.5f;
        [SerializeField, Range(0f, 0.2f)] private float walkBob = 0.02f;
        [SerializeField, Range(0f, 12f)] private float walkSway = 0.9f;
        [SerializeField, Range(0f, 0.15f)] private float squashAmount = 0.014f;
        [SerializeField, Min(1f)] private float locomotionFollow = 14f;
        [SerializeField, Min(1f)] private float actionPoseFollow = 28f;

        [Header("Articulated Sprite Frames")]
        [SerializeField] private Sprite articulatedSheet;
        [SerializeField] private Vector2Int articulatedGrid =
            new Vector2Int(3, 2);
        [SerializeField, Min(32f)] private float articulatedPixelsPerUnit = 320f;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float hitDuration = 0.2f;
        [SerializeField, Min(0.2f)] private float deathDuration = 0.48f;

        private EnemyMovement movement;
        private SpriteRenderer rootRenderer;
        private SpriteRenderer visualRenderer;
        private Transform visualTransform;
        private Sprite baseSprite;
        private Sprite[] articulatedFrames;
        private Sprite builtFromSheet;
        private Vector3 restPosition;
        private Vector3 restScale;
        private Color restColor;
        private float gaitPhase;
        private float locomotionBlend;
        private float attackStart;
        private float attackImpact;
        private float attackActiveEnd;
        private float attackEnd;
        private float skillStart;
        private float skillImpact;
        private float skillActiveEnd;
        private float skillEnd;
        private float hopStart;
        private float hopTakeoff;
        private float hopLand;
        private float hopEnd;
        private float hitStart;
        private float hitEnd;
        private Vector2 actionDirection = Vector2.right;
        private Vector2 hitDirection = Vector2.left;
        private Color skillColor = new Color(1f, 0.35f, 0.12f);
        private bool useLeftFoot;
        private bool dying;
        private float deathStart;

        public float DeathDuration => deathDuration;
        public EnemyLocomotionStyle LocomotionStyle { get; private set; } =
            EnemyLocomotionStyle.Scurry;
        public bool IsHopping => Time.time < hopEnd;
        public bool IsPerformingAction => Time.time < attackEnd ||
            Time.time < skillEnd || IsHopping;
        public bool HasArticulatedFrames => articulatedFrames != null &&
            articulatedFrames.Length >= 6;
        public int CurrentArticulatedFrame { get; private set; } = -1;
        public EnemyActionPhase CurrentActionPhase
        {
            get
            {
                if (Time.time < attackEnd)
                {
                    return ResolvePhase(attackImpact, attackActiveEnd, attackEnd);
                }

                if (Time.time < skillEnd)
                {
                    return ResolvePhase(skillImpact, skillActiveEnd, skillEnd);
                }

                if (Time.time < hopEnd)
                {
                    return ResolvePhase(hopTakeoff, hopLand, hopEnd);
                }

                return EnemyActionPhase.None;
            }
        }

        private void Awake()
        {
            movement = GetComponent<EnemyMovement>();
            PrepareVisual();
            gaitPhase = (float)(EntityId.ToULong(GetEntityId()) % 1000UL) / 1000f;
        }

        private void Update()
        {
            if (visualTransform == null || visualRenderer == null)
            {
                PrepareVisual();
                if (visualTransform == null) return;
            }

            if (dying)
            {
                UpdateDeath();
                return;
            }

            Vector2 velocity = movement != null ? movement.CurrentVelocity : Vector2.zero;
            float speed = velocity.magnitude;
            float targetMoveBlend = Mathf.InverseLerp(0.08f, 2f, speed);
            if (IsPerformingAction)
            {
                targetMoveBlend *= 0.08f;
            }
            locomotionBlend = Mathf.MoveTowards(locomotionBlend, targetMoveBlend,
                Time.deltaTime * (IsPerformingAction ? 10f : 4f));
            float moveBlend = locomotionBlend;
            float previousGait = gaitPhase;
            gaitPhase = Mathf.Repeat(gaitPhase + speed * strideFrequency * Time.deltaTime, 1f);

            float step = Mathf.Sin(gaitPhase * Mathf.PI * 2f);
            float groundedStep = 0.5f - 0.5f *
                Mathf.Cos(gaitPhase * Mathf.PI * 4f);
            Vector3 targetPosition = restPosition + Vector3.up *
                (groundedStep * walkBob * moveBlend);
            Vector3 targetScale = restScale;
            float squash = groundedStep * squashAmount * moveBlend;
            targetScale.x *= 1f + squash;
            targetScale.y *= 1f - squash;
            float targetAngle = -step * walkSway * moveBlend -
                Mathf.Clamp(velocity.x * 0.12f, -0.75f, 0.75f);

            ApplyLocomotionPersonality(step, groundedStep, moveBlend,
                ref targetPosition, ref targetScale, ref targetAngle);

            ApplyAttackPose(ref targetPosition, ref targetScale, ref targetAngle);
            ApplySkillPose(ref targetPosition, ref targetScale, ref targetAngle);
            ApplyHopPose(ref targetPosition, ref targetScale, ref targetAngle);
            ApplyHitPose(ref targetPosition, ref targetScale, ref targetAngle);
            UpdateArticulatedFrame(speed, moveBlend);

            bool crossedHalfStep = previousGait < 0.5f && gaitPhase >= 0.5f;
            bool wrappedStep = gaitPhase < previousGait;
            if (moveBlend > 0.55f && (crossedHalfStep || wrappedStep) &&
                Time.time >= attackEnd && Time.time >= skillEnd)
            {
                float size = Mathf.Clamp(visualRenderer.bounds.size.x * 0.65f,
                    0.65f, 1.4f);
                if (LocomotionStyle == EnemyLocomotionStyle.Scurry ||
                    LocomotionStyle == EnemyLocomotionStyle.Skirmish)
                {
                    EnemyCombatVfx.ForestFootstep(transform.position, velocity,
                        useLeftFoot, size);
                }
                else
                {
                    EnemyCombatVfx.Footstep(transform.position, velocity,
                        useLeftFoot, size);
                }
                useLeftFoot = !useLeftFoot;
            }

            float followSpeed = IsPerformingAction
                ? actionPoseFollow
                : locomotionFollow;
            float follow = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            visualTransform.localPosition = Vector3.Lerp(
                visualTransform.localPosition, targetPosition, follow);
            visualTransform.localScale = Vector3.Lerp(
                visualTransform.localScale, targetScale, follow);
            visualTransform.localRotation = Quaternion.Lerp(
                visualTransform.localRotation, Quaternion.Euler(0f, 0f, targetAngle), follow);

            if (!IsPerformingAction && Mathf.Abs(velocity.x) > 0.05f)
            {
                visualRenderer.flipX = velocity.x < 0f;
            }

            if (Time.time < hitEnd)
            {
                visualRenderer.color = Color.Lerp(Color.white,
                    new Color(1f, 0.28f, 0.22f), 0.55f);
            }
            else if (Time.time < skillEnd)
            {
                float pulse = 0.45f + Mathf.Sin(Time.time * 22f) * 0.12f;
                visualRenderer.color = Color.Lerp(restColor, skillColor, pulse);
            }
            else
            {
                visualRenderer.color = restColor;
            }
        }

        public void PlayAttack(Vector2 direction, float duration = 0.32f)
        {
            float safeDuration = Mathf.Max(0.12f, duration);
            PlayAttackPhased(direction, safeDuration * 0.34f,
                safeDuration * 0.28f, safeDuration * 0.38f);
        }

        public void PlayAttackPhased(Vector2 direction, float windup,
            float active, float recovery)
        {
            if (dying) return;
            actionDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            attackStart = Time.time;
            attackImpact = attackStart + Mathf.Max(0.01f, windup);
            attackActiveEnd = attackImpact + Mathf.Max(0.01f, active);
            attackEnd = attackActiveEnd + Mathf.Max(0.01f, recovery);
            skillEnd = 0f;
            if (visualRenderer != null && Mathf.Abs(actionDirection.x) > 0.05f)
            {
                visualRenderer.flipX = actionDirection.x < 0f;
            }
        }

        public void PlaySkill(Vector2 direction, float duration, Color color)
        {
            float safeDuration = Mathf.Max(0.25f, duration);
            PlaySkillPhased(direction, safeDuration * 0.72f,
                safeDuration * 0.1f, safeDuration * 0.18f, color);
        }

        public void PlaySkillPhased(Vector2 direction, float windup,
            float active, float recovery, Color color)
        {
            if (dying) return;
            actionDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            skillStart = Time.time;
            skillImpact = skillStart + Mathf.Max(0.01f, windup);
            skillActiveEnd = skillImpact + Mathf.Max(0.01f, active);
            skillEnd = skillActiveEnd + Mathf.Max(0.01f, recovery);
            attackEnd = 0f;
            skillColor = color;
            if (visualRenderer != null && Mathf.Abs(actionDirection.x) > 0.05f)
            {
                visualRenderer.flipX = actionDirection.x < 0f;
            }
        }

        public void PlayHop(Vector2 direction, float windup,
            float airTime, float landingRecovery)
        {
            if (dying) return;
            actionDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            hopStart = Time.time;
            hopTakeoff = hopStart + Mathf.Max(0.04f, windup);
            hopLand = hopTakeoff + Mathf.Max(0.08f, airTime);
            hopEnd = hopLand + Mathf.Max(0.04f, landingRecovery);
            attackEnd = 0f;
            skillEnd = 0f;
            if (visualRenderer != null && Mathf.Abs(actionDirection.x) > 0.05f)
            {
                visualRenderer.flipX = actionDirection.x < 0f;
            }
        }

        public void PlayHit(Vector2 direction)
        {
            if (dying) return;
            hitDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.left;
            hitStart = Time.time;
            hitEnd = Time.time + hitDuration;
        }

        public void ConfigureArchetype(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Slime:
                    LocomotionStyle = EnemyLocomotionStyle.Bounce;
                    strideFrequency = 0.7f;
                    walkBob = 0.055f;
                    walkSway = 1.2f;
                    squashAmount = 0.075f;
                    break;
                case EnemyArchetype.Goblin:
                    LocomotionStyle = EnemyLocomotionStyle.Scurry;
                    strideFrequency = 1.08f;
                    walkBob = 0.032f;
                    walkSway = 4.4f;
                    squashAmount = 0.034f;
                    break;
                case EnemyArchetype.Archer:
                    LocomotionStyle = EnemyLocomotionStyle.Skirmish;
                    strideFrequency = 0.84f;
                    walkBob = 0.022f;
                    walkSway = 2.2f;
                    squashAmount = 0.016f;
                    break;
                case EnemyArchetype.Mage:
                    LocomotionStyle = EnemyLocomotionStyle.Float;
                    strideFrequency = 0.38f;
                    walkBob = 0.012f;
                    walkSway = 0.55f;
                    squashAmount = 0.008f;
                    break;
                case EnemyArchetype.Tank:
                    LocomotionStyle = EnemyLocomotionStyle.Stomp;
                    strideFrequency = 0.42f;
                    walkBob = 0.035f;
                    walkSway = 1.15f;
                    squashAmount = 0.026f;
                    break;
                case EnemyArchetype.CrystalScorpion:
                    LocomotionStyle = EnemyLocomotionStyle.Skitter;
                    strideFrequency = 1.08f;
                    walkBob = 0.012f;
                    walkSway = 2.7f;
                    squashAmount = 0.012f;
                    break;
                case EnemyArchetype.VoidReaver:
                    LocomotionStyle = EnemyLocomotionStyle.RiftGlide;
                    strideFrequency = 0.58f;
                    walkBob = 0.016f;
                    walkSway = 1.45f;
                    squashAmount = 0.016f;
                    break;
                case EnemyArchetype.Boss:
                    LocomotionStyle = EnemyLocomotionStyle.Colossus;
                    strideFrequency = 0.3f;
                    walkBob = 0.045f;
                    walkSway = 0.75f;
                    squashAmount = 0.022f;
                    break;
            }
        }

        /// <summary>Clears a cancelled attack pose without reviving a dying character or changing its facing.</summary>
        public void CancelAction()
        {
            attackStart = attackImpact = attackActiveEnd = attackEnd = 0f;
            skillStart = skillImpact = skillActiveEnd = skillEnd = 0f;
            hopStart = hopTakeoff = hopLand = hopEnd = 0f;
        }

        public void PlayDeath()
        {
            if (dying) return;
            dying = true;
            deathStart = Time.time;
            if (movement != null) movement.Stop();
        }

        public void ResetForSpawn(Color tint, float scaleMultiplier = 1f)
        {
            PrepareVisual();
            dying = false;
            deathStart = 0f;
            attackStart = attackImpact = attackActiveEnd = attackEnd = 0f;
            skillStart = skillImpact = skillActiveEnd = skillEnd = 0f;
            hopStart = hopTakeoff = hopLand = hopEnd = 0f;
            hitStart = hitEnd = 0f;
            locomotionBlend = 0f;
            useLeftFoot = false;
            if (visualTransform != null)
            {
                visualTransform.localPosition = restPosition;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = restScale * Mathf.Max(0.5f, scaleMultiplier);
            }

            if (visualRenderer != null)
            {
                visualRenderer.sprite = baseSprite;
                restColor = tint;
                visualRenderer.color = tint;
            }

            CurrentArticulatedFrame = -1;
        }

        private void ApplyAttackPose(ref Vector3 position, ref Vector3 scale, ref float angle)
        {
            if (Time.time >= attackEnd) return;
            float anticipation = Time.time < attackImpact
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    attackStart, attackImpact, Time.time))
                : 0f;
            float strike = Time.time >= attackImpact && Time.time < attackActiveEnd
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    attackImpact, attackActiveEnd, Time.time))
                : 0f;
            float recovery = Time.time >= attackActiveEnd
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    attackActiveEnd, attackEnd, Time.time))
                : 0f;
            float reach = strike * 0.22f - anticipation * 0.08f + recovery * 0.035f;
            position += (Vector3)(actionDirection * reach);
            scale.x *= 1f + strike * 0.13f;
            scale.y *= 1f - strike * 0.09f + anticipation * 0.06f;
            angle += -actionDirection.x * (anticipation * 9f - strike * 13f);
        }

        private void ApplyLocomotionPersonality(float step, float groundedStep,
            float moveBlend, ref Vector3 position, ref Vector3 scale,
            ref float angle)
        {
            switch (LocomotionStyle)
            {
                case EnemyLocomotionStyle.Bounce:
                    float hop = Mathf.Max(0f, step) * moveBlend;
                    position.y += hop * 0.07f;
                    scale.x *= 1f + (groundedStep - hop) * 0.06f * moveBlend;
                    scale.y *= 1f - (groundedStep - hop) * 0.05f * moveBlend;
                    break;
                case EnemyLocomotionStyle.Scurry:
                    float scurryBeat = Mathf.Sin(gaitPhase * Mathf.PI * 4f);
                    position.x += scurryBeat * 0.025f * moveBlend;
                    position.y -= 0.018f * moveBlend;
                    scale.x *= 1f + 0.035f * moveBlend;
                    scale.y *= 1f - 0.045f * moveBlend;
                    angle += scurryBeat * 3.2f * moveBlend;
                    break;
                case EnemyLocomotionStyle.Skirmish:
                    float crossStep = Mathf.Sin(gaitPhase * Mathf.PI * 4f);
                    position.x += crossStep * 0.028f * moveBlend;
                    position.y -= groundedStep * 0.008f * moveBlend;
                    scale.x *= 1f + Mathf.Abs(crossStep) * 0.014f * moveBlend;
                    scale.y *= 1f - groundedStep * 0.018f * moveBlend;
                    angle -= crossStep * 1.7f * moveBlend;
                    break;
                case EnemyLocomotionStyle.Float:
                    position.y += (0.045f + Mathf.Sin(Time.time * 2.8f +
                        gaitPhase * Mathf.PI * 2f) * 0.028f) *
                        Mathf.Lerp(0.65f, 1f, moveBlend);
                    angle += Mathf.Sin(Time.time * 1.9f) * 0.65f;
                    break;
                case EnemyLocomotionStyle.Stomp:
                    scale.y *= 1f - groundedStep * 0.025f * moveBlend;
                    position.y -= groundedStep * 0.012f * moveBlend;
                    break;
                case EnemyLocomotionStyle.Skitter:
                    position.x += Mathf.Sin(gaitPhase * Mathf.PI * 4f) *
                        0.018f * moveBlend;
                    angle += step * 1.35f * moveBlend;
                    break;
                case EnemyLocomotionStyle.RiftGlide:
                    position.y += (0.025f + Mathf.Sin(Time.time * 4.2f +
                        gaitPhase * Mathf.PI * 2f) * 0.018f) *
                        Mathf.Lerp(0.55f, 1f, moveBlend);
                    scale.x *= 1f + Mathf.Sin(Time.time * 7.5f) *
                        0.012f * moveBlend;
                    break;
                case EnemyLocomotionStyle.Colossus:
                    position.y -= groundedStep * 0.018f * moveBlend;
                    scale.x *= 1f + groundedStep * 0.018f * moveBlend;
                    scale.y *= 1f - groundedStep * 0.015f * moveBlend;
                    break;
            }
        }

        private void ApplyHitPose(ref Vector3 position, ref Vector3 scale, ref float angle)
        {
            if (Time.time >= hitEnd) return;
            float t = Mathf.InverseLerp(hitStart, hitEnd, Time.time);
            float recoil = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.35f);
            float shake = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
            position += (Vector3)(hitDirection * recoil * 0.12f);
            position.x += shake * 0.035f;
            scale.x *= 1f + recoil * 0.08f;
            scale.y *= 1f - recoil * 0.1f;
            angle += -hitDirection.x * recoil * 9f;
        }

        private void ApplyHopPose(ref Vector3 position, ref Vector3 scale,
            ref float angle)
        {
            if (Time.time >= hopEnd) return;
            if (Time.time < hopTakeoff)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    hopStart, hopTakeoff, Time.time));
                position.y -= t * 0.055f;
                scale.x *= 1f + t * 0.14f;
                scale.y *= 1f - t * 0.15f;
                angle += actionDirection.x * t * 3f;
                return;
            }

            if (Time.time < hopLand)
            {
                float t = Mathf.InverseLerp(hopTakeoff, hopLand, Time.time);
                float arc = Mathf.Sin(t * Mathf.PI);
                float launchStretch = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(t * 2.4f));
                position.y += arc * 0.34f;
                scale.x *= 1f - launchStretch * 0.08f + arc * 0.025f;
                scale.y *= 1f + launchStretch * 0.16f - arc * 0.025f;
                angle += -actionDirection.x * Mathf.Sin(t * Mathf.PI) * 4f;
                return;
            }

            float recovery = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(hopLand, hopEnd, Time.time));
            position.y -= recovery * 0.035f;
            scale.x *= 1f + recovery * 0.16f;
            scale.y *= 1f - recovery * 0.14f;
        }

        private void ApplySkillPose(ref Vector3 position, ref Vector3 scale,
            ref float angle)
        {
            if (Time.time >= skillEnd) return;
            float charge = Time.time < skillImpact
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    skillStart, skillImpact, Time.time))
                : 0f;
            float release = Time.time >= skillImpact && Time.time < skillActiveEnd
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    skillImpact, skillActiveEnd, Time.time))
                : 0f;
            float recovery = Time.time >= skillActiveEnd
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                    skillActiveEnd, skillEnd, Time.time))
                : 0f;
            position += Vector3.down * (charge * 0.055f) +
                (Vector3)(actionDirection * (release * 0.18f + recovery * 0.025f));
            scale.x *= 1f + charge * 0.09f + release * 0.14f + recovery * 0.025f;
            scale.y *= 1f - charge * 0.07f - release * 0.1f - recovery * 0.015f;
            angle += -actionDirection.x * (release * 10f + recovery * 2f);
        }

        private static EnemyActionPhase ResolvePhase(float impact,
            float activeEnd, float end)
        {
            if (Time.time < impact) return EnemyActionPhase.Windup;
            if (Time.time < activeEnd) return EnemyActionPhase.Active;
            if (Time.time < end) return EnemyActionPhase.Recovery;
            return EnemyActionPhase.None;
        }

        private void UpdateDeath()
        {
            float t = Mathf.Clamp01((Time.time - deathStart) / deathDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            visualTransform.localPosition = restPosition + Vector3.down * (0.18f * eased);
            visualTransform.localRotation = Quaternion.Euler(0f, 0f,
                (visualRenderer.flipX ? 1f : -1f) * 82f * eased);
            visualTransform.localScale = Vector3.Lerp(restScale,
                new Vector3(restScale.x * 1.12f, restScale.y * 0.72f, restScale.z), eased);
            Color color = restColor;
            color.a *= 1f - t;
            visualRenderer.color = color;
        }

        private void UpdateArticulatedFrame(float speed, float moveBlend)
        {
            BuildArticulatedFrames();
            if (!HasArticulatedFrames || visualRenderer == null)
            {
                CurrentArticulatedFrame = -1;
                return;
            }

            int frame = -1;
            if (Time.time < skillEnd)
            {
                frame = Time.time < skillImpact ? 4 : 5;
            }
            else if (Time.time < attackEnd)
            {
                frame = Time.time < attackImpact ? 2 : 3;
            }
            else if (speed > 0.08f && moveBlend > 0.12f)
            {
                frame = gaitPhase < 0.5f ? 0 : 1;
            }

            CurrentArticulatedFrame = frame;
            visualRenderer.sprite = frame >= 0
                ? articulatedFrames[frame]
                : baseSprite;
        }

        private void BuildArticulatedFrames()
        {
            if (articulatedSheet == null || builtFromSheet == articulatedSheet &&
                articulatedFrames != null)
            {
                return;
            }

            ReleaseArticulatedFrames();
            int columns = Mathf.Max(1, articulatedGrid.x);
            int rows = Mathf.Max(1, articulatedGrid.y);
            if (columns * rows < 6 || articulatedSheet.texture == null)
            {
                return;
            }

            Rect sheetRect = articulatedSheet.textureRect;
            float cellWidth = sheetRect.width / columns;
            float cellHeight = sheetRect.height / rows;
            articulatedFrames = new Sprite[6];
            for (int index = 0; index < articulatedFrames.Length; index++)
            {
                int column = index % columns;
                int rowFromTop = index / columns;
                int rowFromBottom = rows - 1 - rowFromTop;
                Rect frameRect = new Rect(
                    sheetRect.x + column * cellWidth,
                    sheetRect.y + rowFromBottom * cellHeight,
                    cellWidth, cellHeight);
                Sprite frame = Sprite.Create(articulatedSheet.texture, frameRect,
                    new Vector2(0.5f, 0.5f), articulatedPixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
                frame.name = name + " Articulated Frame " + index;
                articulatedFrames[index] = frame;
            }

            builtFromSheet = articulatedSheet;
        }

        private void ReleaseArticulatedFrames()
        {
            if (articulatedFrames != null)
            {
                foreach (Sprite frame in articulatedFrames)
                {
                    if (frame == null) continue;
                    if (Application.isPlaying) Destroy(frame);
                    else DestroyImmediate(frame);
                }
            }

            articulatedFrames = null;
            builtFromSheet = null;
        }

        private void OnDestroy()
        {
            ReleaseArticulatedFrames();
        }

        private void PrepareVisual()
        {
            if (visualRenderer != null) return;
            rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer == null) return;

            Transform existing = transform.Find("AnimatedVisual");
            if (existing == null)
            {
                GameObject visualObject = new GameObject("AnimatedVisual");
                visualObject.transform.SetParent(transform, false);
                existing = visualObject.transform;
            }

            visualTransform = existing;
            visualRenderer = existing.GetComponent<SpriteRenderer>();
            if (visualRenderer == null) visualRenderer = existing.gameObject.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = rootRenderer.sprite;
            visualRenderer.color = rootRenderer.color;
            visualRenderer.sharedMaterial = rootRenderer.sharedMaterial;
            visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
            visualRenderer.sortingOrder = rootRenderer.sortingOrder;
            visualRenderer.flipX = rootRenderer.flipX;
            rootRenderer.enabled = false;

            baseSprite = rootRenderer.sprite;
            BuildArticulatedFrames();

            restPosition = visualTransform.localPosition;
            restScale = visualTransform.localScale;
            restColor = visualRenderer.color;
        }
    }
}
