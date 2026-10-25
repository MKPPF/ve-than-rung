using System.Collections;
using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Chiêu F: Sấm sét – Gọi sét từ trời giáng xuống vị trí chỉ định,
    /// gây sát thương trong phạm vi. Hiệu ứng bao gồm:
    /// 1) Chỉ báo vị trí (indicator nhấp nháy)
    /// 2) Nhiều tia sét chính từ trời đánh xuống
    /// 3) Tia sét nhánh lan tỏa từ điểm va chạm
    /// 4) Vết nứt đất lan tỏa từ điểm va chạm (thay cho glow)
    /// 5) Nhấp nháy sét + fade out
    /// 6) Vết nứt tồn tại rồi mờ dần
    /// Tất cả dùng LineRenderer procedural, không cần prefab/VFX package.
    /// </summary>
    public sealed class LightningStrikeSkill : SkillBase, ITargetedSkill
    {
        [Header("Damage")]
        [SerializeField, Min(0.5f)] private float radius = 2f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        [Header("Lightning Bolts")]
        [SerializeField] private int mainBoltSegments = 18;
        [SerializeField] private int mainBoltCount = 3;
        [SerializeField] private float mainBoltSpread = 0.6f;
        [SerializeField] private int branchBoltCount = 5;
        [SerializeField] private int branchSegments = 8;
        [SerializeField] private float zigzagStrength = 0.4f;
        [SerializeField] private float branchZigzag = 0.25f;
        [SerializeField] private float strikeHeight = 6f;
        [SerializeField] private float mainLineWidth = 0.22f;
        [SerializeField] private float branchLineWidth = 0.09f;

        [Header("Ground Cracks")]
        [SerializeField] private int crackCount = 10;
        [SerializeField] private int crackSegments = 7;
        [SerializeField] private float crackMaxLength = 3.5f;
        [SerializeField] private float crackWidth = 0.12f;
        [SerializeField] private float crackJaggedness = 0.25f;
        [SerializeField] private float crackExpandDuration = 0.12f;
        [SerializeField] private float crackLingerDuration = 1.2f;
        [SerializeField] private float crackFadeDuration = 0.6f;

        [Header("Timing")]
        [SerializeField] private float preStrikeDelay = 0.18f;
        [SerializeField] private float strikeDuration = 0.4f;
        [SerializeField] private float flickerInterval = 0.03f;
        private bool hasQueuedTarget;
        private Vector2 queuedTarget;

        public void SetNextTarget(Vector2 targetPosition)
        {
            queuedTarget = targetPosition;
            hasQueuedTarget = true;
        }

        public override void Cast()
        {
            Camera camera = Camera.main;
            Vector2 position = transform.position;
            if (hasQueuedTarget)
            {
                position = queuedTarget;
                hasQueuedTarget = false;
            }
            else if (camera != null && Mouse.current != null)
            {
                position = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }

            MonoBehaviourHelper.Instance.StartCoroutine(LightningStrikeSequence(position));
        }

        // ─────────────────────────────────────────────────────
        //  Main coroutine – chạy toàn bộ hiệu ứng theo trình tự
        // ─────────────────────────────────────────────────────
        private IEnumerator LightningStrikeSequence(Vector2 targetPosition)
        {
            // ── Phase 1: Chỉ báo vị trí (indicator nhấp nháy) ──
            GameObject indicator = CreateGroundIndicator(targetPosition);
            yield return MonoBehaviourHelper.Instance.StartCoroutine(
                FlickerIndicator(indicator, preStrikeDelay));
            if (indicator != null) Object.Destroy(indicator);

            // ── Phase 2: Sét đánh + gây sát thương ──
            DealAreaDamage(targetPosition);
            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,targetPosition,radius*2,0.55f);
            if(Data.Presentation!=null && Data.Presentation.HasPaintedArt) yield break;

            // ── Phase 3: Tạo các tia sét ──
            Vector3 groundPos = new Vector3(targetPosition.x, targetPosition.y, -0.2f);

            // Tia sét chính từ trời đánh xuống
            List<BoltData> allBolts = new List<BoltData>();
            for (int i = 0; i < mainBoltCount; i++)
            {
                float offsetX = (i - (mainBoltCount - 1) * 0.5f) * mainBoltSpread
                    + Random.Range(-0.15f, 0.15f);
                Vector3 skyPos = new Vector3(
                    targetPosition.x + offsetX,
                    targetPosition.y + strikeHeight + Random.Range(-0.5f, 0.5f),
                    -0.2f);
                BoltData bolt = CreateLightningBolt(skyPos, groundPos,
                    mainBoltSegments, mainLineWidth * (i == 0 ? 1f : 0.65f), true);
                allBolts.Add(bolt);
            }

            // Tia sét nhánh lan tỏa từ điểm va chạm
            for (int i = 0; i < branchBoltCount; i++)
            {
                float angle = (360f / branchBoltCount) * i + Random.Range(-25f, 25f);
                float branchLen = Random.Range(radius * 0.4f, radius * 1.3f);
                Vector3 branchEnd = groundPos + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * branchLen,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * branchLen, 0f);
                BoltData branch = CreateLightningBolt(groundPos, branchEnd,
                    branchSegments, branchLineWidth, false);
                allBolts.Add(branch);
            }

            // Nhánh phụ trên bầu trời (sét rẽ nhánh)
            for (int i = 0; i < 3; i++)
            {
                float offsetX = Random.Range(-1.2f, 1.2f);
                float splitHeight = Random.Range(0.4f, 0.75f);
                Vector3 splitStart = new Vector3(
                    targetPosition.x + offsetX * 0.4f,
                    targetPosition.y + strikeHeight * splitHeight, -0.2f);
                Vector3 splitEnd = new Vector3(
                    targetPosition.x + offsetX,
                    targetPosition.y + strikeHeight * (splitHeight - 0.25f), -0.2f);
                BoltData skyBranch = CreateLightningBolt(splitStart, splitEnd,
                    branchSegments, branchLineWidth * 0.7f, false);
                allBolts.Add(skyBranch);
            }

            // ── Phase 4: Tạo vết nứt đất (thay cho glow) ──
            List<CrackData> cracks = CreateGroundCracks(targetPosition);

            // Animate vết nứt mở rộng
            MonoBehaviourHelper.Instance.StartCoroutine(
                AnimateGroundCracks(cracks));

            // ── Phase 5: Nhấp nháy sét + fade out ──
            float elapsed = 0f;
            while (elapsed < strikeDuration)
            {
                float fadeT = elapsed / strikeDuration;
                float alpha = Mathf.Lerp(1f, 0f, fadeT * fadeT);

                for (int b = 0; b < allBolts.Count; b++)
                {
                    BoltData bolt = allBolts[b];
                    if (bolt.lineRenderer == null) continue;

                    bool isMain = b < mainBoltCount;

                    // Redraw zigzag mỗi frame → nhấp nháy tự nhiên
                    float zig = isMain ? zigzagStrength : branchZigzag;
                    DrawZigzag(bolt.lineRenderer, bolt.start, bolt.end, bolt.segments, zig);

                    // Nhánh ngẫu nhiên tắt/bật
                    if (!isMain)
                    {
                        bolt.lineRenderer.enabled = Random.value > 0.25f;
                    }

                    // Fade alpha + width
                    float boltAlpha = isMain ? alpha : alpha * 0.8f;
                    Color sc = bolt.lineRenderer.startColor;
                    bolt.lineRenderer.startColor = new Color(sc.r, sc.g, sc.b, boltAlpha);
                    bolt.lineRenderer.endColor = new Color(
                        bolt.endColor.r, bolt.endColor.g, bolt.endColor.b, boltAlpha);

                    float baseWidth = isMain
                        ? mainLineWidth * (b == 0 ? 1f : 0.65f)
                        : branchLineWidth;
                    bolt.lineRenderer.startWidth = baseWidth * alpha;
                    bolt.lineRenderer.endWidth = baseWidth * (isMain ? 0.35f : 0.5f) * alpha;
                }

                elapsed += flickerInterval;
                yield return new WaitForSeconds(flickerInterval);
            }

            // ── Phase 6: Dọn dẹp tia sét ──
            foreach (BoltData bolt in allBolts)
            {
                if (bolt.gameObject != null) Object.Destroy(bolt.gameObject);
                if (bolt.material != null) Object.Destroy(bolt.material);
            }

            // Vết nứt đất được quản lý bởi AnimateGroundCracks coroutine
        }

        // ─────────────────────────────────────────────────────
        //  Gây sát thương vùng
        // ─────────────────────────────────────────────────────
        private void DealAreaDamage(Vector2 targetPosition)
        {
            TopDownGame.Core.CombatFeedback.WorldImpact(targetPosition,0.065f);
            HashSet<int> hitTargets = new HashSet<int>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(targetPosition, radius, enemyLayers))
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                int id = hit.transform.root.gameObject.GetHashCode();
                if (!hit.CompareTag("Player") && target != null && target.IsAlive && hitTargets.Add(id))
                {
                    target.TakeDamage(new DamageInfo(ScaledDamage(1.3f), gameObject,
                        hit.ClosestPoint(targetPosition),
                        hit.transform.position - (Vector3)targetPosition,
                        1f, false, true));
                }
            }
        }

        // ─────────────────────────────────────────────────────
        //  Cấu trúc dữ liệu tia sét
        // ─────────────────────────────────────────────────────
        private struct BoltData
        {
            public GameObject gameObject;
            public LineRenderer lineRenderer;
            public Material material;
            public Vector3 start;
            public Vector3 end;
            public int segments;
            public Color endColor;
        }

        // ─────────────────────────────────────────────────────
        //  Tạo một tia sét bằng LineRenderer
        // ─────────────────────────────────────────────────────
        private BoltData CreateLightningBolt(Vector3 start, Vector3 end,
            int segments, float width, bool isMainBolt)
        {
            GameObject lineGO = new GameObject(isMainBolt ? "MainBolt" : "BranchBolt");
            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = segments + 1;
            lr.startWidth = width;
            lr.endWidth = width * (isMainBolt ? 0.35f : 0.5f);
            lr.numCornerVertices = 3;
            lr.numCapVertices = 3;
            lr.sortingOrder = isMainBolt ? 22 : 21;

            Color skillColor = Data != null ? Data.EffectColor : new Color(0.6f, 0.85f, 1f);
            Color brightColor = new Color(skillColor.r, skillColor.g,
                skillColor.b, 1f);
            Color yellowCore = Color.Lerp(Color.white, brightColor, 0.72f);

            lr.startColor = yellowCore;
            lr.endColor = isMainBolt
                ? brightColor
                : new Color(brightColor.r, brightColor.g, brightColor.b, 0.7f);

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = isMainBolt ? yellowCore : brightColor;
            lr.material = mat;

            float zigzag = isMainBolt ? zigzagStrength : branchZigzag;
            DrawZigzag(lr, start, end, segments, zigzag);

            return new BoltData
            {
                gameObject = lineGO,
                lineRenderer = lr,
                material = mat,
                start = start,
                end = end,
                segments = segments,
                endColor = lr.endColor
            };
        }

        // ─────────────────────────────────────────────────────
        //  Vẽ đường zigzag cho một LineRenderer
        // ─────────────────────────────────────────────────────
        private void DrawZigzag(LineRenderer lr, Vector3 start, Vector3 end,
            int segments, float strength)
        {
            Vector3 direction = end - start;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = Vector3.Lerp(start, end, t);

                if (i > 0 && i < segments)
                {
                    float envelope = 1f - Mathf.Abs(2f * t - 1f);
                    float offset = Random.Range(-strength, strength) * (0.5f + envelope * 0.5f);
                    point += perpendicular * offset;
                }

                point.z = -0.2f;
                lr.SetPosition(i, point);
            }
        }

        // ─────────────────────────────────────────────────────
        //  Chỉ báo vị trí (vòng tròn nhấp nháy trước khi sét đánh)
        // ─────────────────────────────────────────────────────
        private GameObject CreateGroundIndicator(Vector2 position)
        {
            GameObject indicatorGO = new GameObject("StrikeIndicator");
            indicatorGO.transform.position = new Vector3(position.x, position.y, -0.15f);
            indicatorGO.transform.localScale = Vector3.one * (radius * 1.2f);

            SpriteRenderer sr = indicatorGO.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.RingSprite;
            sr.sortingOrder = 19;
            Color skillColor = Data != null ? Data.EffectColor : Color.cyan;
            sr.color = new Color(skillColor.r, skillColor.g, skillColor.b, 0.4f);

            return indicatorGO;
        }

        private IEnumerator FlickerIndicator(GameObject indicator, float duration)
        {
            if (indicator == null) yield break;

            SpriteRenderer sr = indicator.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            float elapsed = 0f;
            Color baseColor = sr.color;
            while (elapsed < duration && indicator != null)
            {
                elapsed += Time.deltaTime;
                float frequency = Mathf.Lerp(6f, 20f, elapsed / duration);
                float flicker = (Mathf.Sin(elapsed * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                    Mathf.Lerp(0.1f, 0.45f, flicker));

                float scale = radius * 2f * (1f + flicker * 0.08f);
                indicator.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  HIỆU ỨNG VẾT NỨT ĐẤT (Ground Cracks)
        //  Thay thế hoàn toàn cho glow/burst
        // ═══════════════════════════════════════════════════════

        private struct CrackData
        {
            public GameObject gameObject;
            public LineRenderer lineRenderer;
            public Material material;
            public Vector3[] fullPositions;   // vị trí đầy đủ khi crack mở rộng hết
            public float length;              // chiều dài tổng của crack
        }

        /// <summary>
        /// Tạo nhiều vết nứt tỏa ra từ tâm theo các hướng ngẫu nhiên.
        /// Mỗi vết nứt là 1 LineRenderer với đường zigzag nhỏ trông giống nứt đất.
        /// </summary>
        private List<CrackData> CreateGroundCracks(Vector2 center)
        {
            List<CrackData> cracks = new List<CrackData>();

            // Màu vết nứt: tối, ấm (như đất nứt bị nhiệt/sét đánh)
            Color crackColorInner = new Color(1f, 0.85f, 0.5f, 0.9f);    // vàng cam sáng ở tâm
            Color crackColorOuter = new Color(0.35f, 0.2f, 0.1f, 0.7f);  // nâu tối ở ngoài

            for (int c = 0; c < crackCount; c++)
            {
                // Góc tỏa ra, phân bố đều + nhiễu nhẹ
                float baseAngle = (360f / crackCount) * c;
                float angle = (baseAngle + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;

                // Chiều dài ngẫu nhiên cho mỗi crack
                float length = Random.Range(crackMaxLength * 0.5f, crackMaxLength);

                // Tính các điểm dọc crack
                int segs = crackSegments + Random.Range(-1, 2);
                segs = Mathf.Max(3, segs);
                Vector3[] positions = new Vector3[segs + 1];

                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                for (int i = 0; i <= segs; i++)
                {
                    float t = i / (float)segs;
                    Vector2 basePoint = center + direction * (length * t);

                    if (i > 0 && i < segs)
                    {
                        // Zigzag nhỏ, tăng dần khi ra xa → vết nứt bất quy tắc hơn
                        float jag = crackJaggedness * (0.5f + t * 0.5f);
                        basePoint += perpendicular * Random.Range(-jag, jag);
                    }

                    positions[i] = new Vector3(basePoint.x, basePoint.y, -0.1f);
                }

                // Tạo LineRenderer cho crack
                GameObject crackGO = new GameObject("GroundCrack");
                LineRenderer lr = crackGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;  // Bắt đầu với 2 điểm (sẽ expand dần)
                lr.SetPosition(0, positions[0]);
                lr.SetPosition(1, positions[0]);  // Bắt đầu từ tâm

                // Width: dày ở tâm, mỏng dần ra ngoài
                float innerWidth = crackWidth * Random.Range(1f, 1.6f);
                float outerWidth = crackWidth * Random.Range(0.2f, 0.5f);
                lr.startWidth = innerWidth;
                lr.endWidth = outerWidth;
                lr.numCornerVertices = 2;
                lr.numCapVertices = 1;
                lr.sortingOrder = 15;

                // Gradient màu: vàng cam ở tâm → nâu tối ra ngoài
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(crackColorInner, 0f),
                        new GradientColorKey(
                            new Color(0.7f, 0.45f, 0.2f), 0.3f),
                        new GradientColorKey(crackColorOuter, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.9f, 0f),
                        new GradientAlphaKey(0.7f, 0.5f),
                        new GradientAlphaKey(0.3f, 1f)
                    }
                );
                lr.colorGradient = gradient;

                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = Color.white;
                lr.material = mat;

                cracks.Add(new CrackData
                {
                    gameObject = crackGO,
                    lineRenderer = lr,
                    material = mat,
                    fullPositions = positions,
                    length = length
                });

                // Thêm nhánh phụ cho một số crack → trông tự nhiên hơn
                if (Random.value > 0.45f && segs > 3)
                {
                    int branchStart = Random.Range(2, segs - 1);
                    float branchAngle = angle + Random.Range(-0.6f, 0.6f);
                    float branchLength = length * Random.Range(0.2f, 0.45f);
                    int branchSegs = Random.Range(2, 4);
                    Vector2 branchDir = new Vector2(
                        Mathf.Cos(branchAngle), Mathf.Sin(branchAngle));
                    Vector2 branchPerp = new Vector2(-branchDir.y, branchDir.x);
                    Vector2 branchOrigin = positions[branchStart];

                    Vector3[] branchPositions = new Vector3[branchSegs + 1];
                    for (int j = 0; j <= branchSegs; j++)
                    {
                        float bt = j / (float)branchSegs;
                        Vector2 bp = branchOrigin + branchDir * (branchLength * bt);
                        if (j > 0 && j < branchSegs)
                        {
                            bp += branchPerp * Random.Range(
                                -crackJaggedness * 0.7f, crackJaggedness * 0.7f);
                        }
                        branchPositions[j] = new Vector3(bp.x, bp.y, -0.1f);
                    }

                    GameObject branchGO = new GameObject("CrackBranch");
                    LineRenderer blr = branchGO.AddComponent<LineRenderer>();
                    blr.useWorldSpace = true;
                    blr.positionCount = 2;
                    blr.SetPosition(0, branchPositions[0]);
                    blr.SetPosition(1, branchPositions[0]);
                    blr.startWidth = outerWidth;
                    blr.endWidth = outerWidth * 0.5f;
                    blr.numCornerVertices = 2;
                    blr.numCapVertices = 1;
                    blr.sortingOrder = 15;
                    blr.colorGradient = gradient;

                    Material bmat = new Material(Shader.Find("Sprites/Default"));
                    bmat.color = Color.white;
                    blr.material = bmat;

                    cracks.Add(new CrackData
                    {
                        gameObject = branchGO,
                        lineRenderer = blr,
                        material = bmat,
                        fullPositions = branchPositions,
                        length = branchLength
                    });
                }
            }

            return cracks;
        }

        /// <summary>
        /// Animate vết nứt: mở rộng nhanh → tồn tại → mờ dần
        /// </summary>
        private IEnumerator AnimateGroundCracks(List<CrackData> cracks)
        {
            // ── Expand phase: vết nứt lan ra nhanh từ tâm ──
            float expandElapsed = 0f;
            while (expandElapsed < crackExpandDuration)
            {
                expandElapsed += Time.deltaTime;
                float expandT = Mathf.Clamp01(expandElapsed / crackExpandDuration);
                // Ease-out: lan nhanh lúc đầu, chậm dần
                float easeT = 1f - (1f - expandT) * (1f - expandT);

                foreach (CrackData crack in cracks)
                {
                    if (crack.lineRenderer == null) continue;

                    // Hiện dần các segment theo thời gian
                    int totalSegs = crack.fullPositions.Length;
                    int visibleCount = Mathf.Clamp(
                        Mathf.CeilToInt(easeT * totalSegs), 2, totalSegs);

                    crack.lineRenderer.positionCount = visibleCount;
                    for (int i = 0; i < visibleCount; i++)
                    {
                        if (i < visibleCount - 1)
                        {
                            crack.lineRenderer.SetPosition(i, crack.fullPositions[i]);
                        }
                        else
                        {
                            // Điểm cuối cùng nội suy mượt
                            float segProgress = (easeT * totalSegs) - (visibleCount - 1);
                            segProgress = Mathf.Clamp01(segProgress);
                            int prevIdx = Mathf.Max(0, visibleCount - 2);
                            Vector3 interpPos = Vector3.Lerp(
                                crack.fullPositions[prevIdx],
                                crack.fullPositions[Mathf.Min(i, totalSegs - 1)],
                                segProgress);
                            crack.lineRenderer.SetPosition(i, interpPos);
                        }
                    }
                }

                yield return null;
            }

            // Đặt tất cả positions đầy đủ
            foreach (CrackData crack in cracks)
            {
                if (crack.lineRenderer == null) continue;
                crack.lineRenderer.positionCount = crack.fullPositions.Length;
                for (int i = 0; i < crack.fullPositions.Length; i++)
                {
                    crack.lineRenderer.SetPosition(i, crack.fullPositions[i]);
                }
            }

            // ── Linger phase: vết nứt tồn tại ──
            yield return new WaitForSeconds(crackLingerDuration);

            // ── Fade phase: mờ dần ──
            float fadeElapsed = 0f;
            while (fadeElapsed < crackFadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float fadeT = Mathf.Clamp01(fadeElapsed / crackFadeDuration);

                foreach (CrackData crack in cracks)
                {
                    if (crack.lineRenderer == null) continue;

                    // Fade alpha qua gradient
                    Gradient current = crack.lineRenderer.colorGradient;
                    GradientColorKey[] colorKeys = current.colorKeys;
                    GradientAlphaKey[] alphaKeys = current.alphaKeys;
                    float fadeAlpha = 1f - fadeT;
                    for (int a = 0; a < alphaKeys.Length; a++)
                    {
                        alphaKeys[a].alpha *= fadeAlpha;
                    }
                    Gradient faded = new Gradient();
                    faded.SetKeys(colorKeys, alphaKeys);
                    crack.lineRenderer.colorGradient = faded;

                    // Width cũng giảm dần
                    crack.lineRenderer.startWidth *= (1f - fadeT * Time.deltaTime * 2f);
                    crack.lineRenderer.endWidth *= (1f - fadeT * Time.deltaTime * 2f);
                }

                yield return null;
            }

            // ── Cleanup ──
            foreach (CrackData crack in cracks)
            {
                if (crack.gameObject != null) Object.Destroy(crack.gameObject);
                if (crack.material != null) Object.Destroy(crack.material);
            }
        }
    }
}
