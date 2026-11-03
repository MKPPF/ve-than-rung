using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Spawns and animates runtime visual effects for An Duong Vuong wielding No Than Lien Chau:
    /// - Normal Arrow shot (Mouse Right / J)
    /// - Skill 1 (Q): Nhat Tien Thau Giap (Single high-speed piercing bronze arrow)
    /// - Skill 2 (E): Tam Tien Te Phat (3 arrows spreading out in 3 directions)
    /// - Skill 3 (R): Ho Thien Giang Tien (Frontal skyward arrow rain)
    /// - Skill 4 (F): Thien Tru Than Tien (Long-range celestial orbital arrow strike with longer cooldown)
    /// </summary>
    public sealed class AnDuongVuongVfx : MonoBehaviour
    {
        private readonly List<GameObject> spawnedChildren = new List<GameObject>();

        public static void SpawnNormalArrow(Transform owner, Vector2 direction, float range)
        {
            if (owner == null) return;
            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("ADV_Vfx_NormalArrow");
            host.transform.position = owner.position;
            AnDuongVuongVfx vfx = host.AddComponent<AnDuongVuongVfx>();
            vfx.StartCoroutine(vfx.AnimateNormalArrow(owner.position, dir, range));
        }

        public static void SpawnSkill1SingleArrow(Vector3 origin, Vector2 direction, float range, float flightDuration)
        {
            GameObject host = new GameObject("ADV_Vfx_Skill1_SingleArrow");
            host.transform.position = origin;
            AnDuongVuongVfx vfx = host.AddComponent<AnDuongVuongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill1SingleArrow(origin, direction, range, flightDuration));
        }

        public static void SpawnSkill2TripleArrows(Vector3 origin, Vector2 forwardDir, float spreadDeg, float range, float flightDuration)
        {
            GameObject host = new GameObject("ADV_Vfx_Skill2_TripleArrows");
            host.transform.position = origin;
            AnDuongVuongVfx vfx = host.AddComponent<AnDuongVuongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill2TripleArrows(origin, forwardDir, spreadDeg, range, flightDuration));
        }

        public static void SpawnSkill3FrontalArrowRain(Transform owner, Vector2 zoneCenter, float radius, float duration)
        {
            GameObject host = new GameObject("ADV_Vfx_Skill3_FrontalArrowRain");
            host.transform.position = zoneCenter;
            AnDuongVuongVfx vfx = host.AddComponent<AnDuongVuongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill3FrontalArrowRain(owner, zoneCenter, radius, duration));
        }

        public static void SpawnSkill4LongRangeSkyfall(Transform owner, Vector2 zoneCenter, float radius, float duration)
        {
            GameObject host = new GameObject("ADV_Vfx_Skill4_LongRangeSkyfall");
            host.transform.position = zoneCenter;
            AnDuongVuongVfx vfx = host.AddComponent<AnDuongVuongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill4LongRangeSkyfall(owner, zoneCenter, radius, duration));
        }

        // =====================================================================
        // ANIMATION COROUTINES
        // =====================================================================

        private IEnumerator AnimateNormalArrow(Vector3 origin, Vector2 dir, float range)
        {
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            SpriteRenderer arrow = CreateRenderer("Arrow", AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 22, Color.white);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            arrow.transform.localScale = Vector3.one * 0.85f;

            float speed = 22f;
            float duration = Mathf.Clamp(range / speed, 0.12f, 0.35f);
            float elapsed = 0f;
            Vector3 startPos = origin + (Vector3)(dir * 0.4f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                arrow.transform.position = startPos + (Vector3)(dir * (range * t));
                yield return null;
            }

            // Impact burst
            SpawnImpactBurst(arrow.transform.position, dir, 0.5f, 3);
            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill1SingleArrow(Vector3 origin, Vector2 dir, float range, float flightDuration)
        {
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Muzzle flash ring
            SpriteRenderer muzzleRing = CreateRenderer("MuzzleRing", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 20, new Color(1f, 0.85f, 0.3f, 0.9f));
            muzzleRing.transform.position = origin + (Vector3)(dir * 0.45f);
            muzzleRing.transform.localScale = Vector3.one * 0.35f;

            // Main bronze arrow
            SpriteRenderer arrow = CreateRenderer("PierceArrow", AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 24, Color.white);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            arrow.transform.localScale = Vector3.one * 1.15f;

            // Trail glow
            SpriteRenderer trail = CreateRenderer("ArrowTrail", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 21, new Color(0.3f, 0.9f, 1f, 0.65f));
            trail.transform.localScale = new Vector3(0.5f, 0.2f, 1f);

            Vector3 startPos = origin + (Vector3)(dir * 0.45f);
            Vector3 targetPos = origin + (Vector3)(dir * range);
            float elapsed = 0f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                arrow.transform.position = currentPos;
                trail.transform.position = currentPos - (Vector3)(dir * 0.25f);
                trail.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

                float mFade = Mathf.Clamp01(1f - (elapsed / 0.15f));
                muzzleRing.color = new Color(1f, 0.85f, 0.3f, mFade * 0.8f);
                muzzleRing.transform.localScale = Vector3.one * (0.35f + (1f - mFade) * 0.4f);

                yield return null;
            }

            // Impact explosion
            SpawnImpactBurst(targetPos, dir, 0.9f, 6);
            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill2TripleArrows(Vector3 origin, Vector2 forwardDir, float spreadDeg, float range, float flightDuration)
        {
            float[] angles = { -spreadDeg, 0f, spreadDeg };
            SpriteRenderer[] arrows = new SpriteRenderer[3];
            Vector2[] dirs = new Vector2[3];

            // Muzzle burst
            SpriteRenderer muzzleRing = CreateRenderer("TripleMuzzleRing", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 20, new Color(1f, 0.85f, 0.3f, 0.95f));
            muzzleRing.transform.position = origin + (Vector3)(forwardDir * 0.45f);
            muzzleRing.transform.localScale = Vector3.one * 0.45f;

            for (int i = 0; i < 3; i++)
            {
                dirs[i] = Quaternion.Euler(0f, 0f, angles[i]) * forwardDir;
                float angleDeg = Mathf.Atan2(dirs[i].y, dirs[i].x) * Mathf.Rad2Deg;
                arrows[i] = CreateRenderer("TripleArrow_" + i, AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 24, Color.white);
                arrows[i].transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                arrows[i].transform.localScale = Vector3.one * 1.0f;
                arrows[i].transform.position = origin + (Vector3)(dirs[i] * 0.45f);
            }

            float elapsed = 0f;
            Vector3 startPos = origin;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);

                for (int i = 0; i < 3; i++)
                {
                    Vector3 currentPos = startPos + (Vector3)(dirs[i] * (range * t + 0.45f));
                    arrows[i].transform.position = currentPos;
                }

                float mFade = Mathf.Clamp01(1f - (elapsed / 0.18f));
                muzzleRing.color = new Color(1f, 0.85f, 0.3f, mFade * 0.9f);
                muzzleRing.transform.localScale = Vector3.one * (0.45f + (1f - mFade) * 0.5f);

                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                SpawnImpactBurst(arrows[i].transform.position, dirs[i], 0.75f, 4);
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill3FrontalArrowRain(Transform owner, Vector2 zoneCenter, float radius, float duration)
        {
            // Caster skyward aura
            if (owner != null)
            {
                SpriteRenderer casterAura = CreateRenderer("CasterAura", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 19, new Color(0.3f, 0.92f, 1f, 0.8f));
                casterAura.transform.position = owner.position;
                casterAura.transform.localScale = Vector3.one * 0.75f;
                StartCoroutine(FadeAndScale(casterAura, 0.35f, 0.75f, 1.35f));
            }

            // Ground target Dong Son mandala ring
            SpriteRenderer groundRing = CreateRenderer("GroundMandala", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 8, new Color(1f, 0.82f, 0.25f, 0.85f));
            groundRing.transform.position = zoneCenter;
            float targetScale = (radius * 2f) / 1.28f; // Mandala diameter is ~1.28m at scale 1
            groundRing.transform.localScale = Vector3.one * targetScale;

            const int volleyWaves = 5;
            float waveInterval = duration / volleyWaves;

            for (int w = 0; w < volleyWaves; w++)
            {
                int arrowsPerWave = 3 + (w % 2);
                for (int a = 0; a < arrowsPerWave; a++)
                {
                    Vector2 randOffset = Random.insideUnitCircle * (radius * 0.85f);
                    Vector3 impactPoint = (Vector3)(zoneCenter + randOffset);
                    StartCoroutine(AnimateSingleFallingArrow(impactPoint, 0.18f));
                }

                yield return new WaitForSeconds(waveInterval);
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.25f)
            {
                fadeElapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - fadeElapsed / 0.25f);
                groundRing.color = new Color(1f, 0.82f, 0.25f, alpha * 0.85f);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill4LongRangeSkyfall(Transform owner, Vector2 zoneCenter, float radius, float duration)
        {
            // Caster massive skyward beam
            if (owner != null)
            {
                SpriteRenderer casterBeam = CreateRenderer("CasterBeam", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 20, new Color(1f, 0.88f, 0.32f, 0.95f));
                casterBeam.transform.position = owner.position;
                casterBeam.transform.localScale = Vector3.one * 1.2f;
                StartCoroutine(FadeAndScale(casterBeam, 0.5f, 1.2f, 2.2f));
            }

            // Distant target beacon - expanding Dong Son solar ring
            SpriteRenderer beacon = CreateRenderer("SkyfallBeacon", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 8, new Color(1f, 0.72f, 0.18f, 0.95f));
            beacon.transform.position = zoneCenter;
            float targetScale = (radius * 2f) / 1.28f;
            beacon.transform.localScale = Vector3.one * 0.2f;

            // Expansion & pulse
            float expandTime = 0.28f;
            float elapsed = 0f;
            while (elapsed < expandTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / expandTime);
                beacon.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, targetScale, t);
                yield return null;
            }

            // Pre-barrage warning arrows
            const int preVolleyCount = 8;
            for (int i = 0; i < preVolleyCount; i++)
            {
                Vector2 rOff = Random.insideUnitCircle * (radius * 0.75f);
                StartCoroutine(AnimateSingleFallingArrow((Vector3)(zoneCenter + rOff), 0.16f));
                if (i % 2 == 0) yield return new WaitForSeconds(0.04f);
            }

            yield return new WaitForSeconds(0.12f);

            // FINAL COLOSSAL DIVINE LANCE CRASH
            Vector3 crashPoint = zoneCenter;
            SpriteRenderer colossalLance = CreateRenderer("ColossalLance", AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 26, Color.white);
            colossalLance.transform.localScale = Vector3.one * 2.5f;
            colossalLance.transform.rotation = Quaternion.Euler(0f, 0f, -90f); // Pointing straight down

            Vector3 lanceStart = crashPoint + new Vector3(0f, 8f, 0f);
            float plungeTime = 0.14f;
            elapsed = 0f;
            while (elapsed < plungeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / plungeTime);
                colossalLance.transform.position = Vector3.Lerp(lanceStart, crashPoint, t * t);
                yield return null;
            }

            // Detonation!
            Destroy(colossalLance.gameObject);
            SpawnColossalImpactExplosion(crashPoint, radius);

            // Lingering aftershock
            float remain = Mathf.Max(0.2f, duration - 0.54f);
            yield return new WaitForSeconds(remain);

            Destroy(gameObject);
        }

        private IEnumerator AnimateSingleFallingArrow(Vector3 impactPoint, float fallTime)
        {
            SpriteRenderer arrow = CreateRenderer("FallingArrow", AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 23, Color.white);
            arrow.transform.localScale = Vector3.one * 0.95f;
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, -90f); // Pointing down

            Vector3 startPos = impactPoint + new Vector3(Random.Range(-0.4f, 0.4f), 5.5f, 0f);
            float elapsed = 0f;

            while (elapsed < fallTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallTime);
                arrow.transform.position = Vector3.Lerp(startPos, impactPoint, t * t);
                yield return null;
            }

            Destroy(arrow.gameObject);
            SpawnImpactBurst(impactPoint, Vector2.up, 0.6f, 3);
        }

        private void SpawnImpactBurst(Vector3 point, Vector2 normalDir, float scale, int shardCount)
        {
            GameObject burstHost = new GameObject("ADV_ImpactBurst");
            burstHost.transform.position = point;
            AnDuongVuongVfx burst = burstHost.AddComponent<AnDuongVuongVfx>();
            burst.StartCoroutine(burst.AnimateImpactBurst(point, normalDir, scale, shardCount));
        }

        private IEnumerator AnimateImpactBurst(Vector3 point, Vector2 normalDir, float scale, int shardCount)
        {
            SpriteRenderer ring = CreateRenderer("BurstRing", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 21, new Color(1f, 0.84f, 0.28f, 0.95f));
            ring.transform.position = point;
            ring.transform.localScale = Vector3.one * 0.2f;

            SpriteRenderer[] sparks = new SpriteRenderer[shardCount];
            Vector2[] sparkVel = new Vector2[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                sparks[i] = CreateRenderer("Spark_" + i, AnDuongVuongVisualFactory.GetBronzeArrowSprite(), 22, Color.white);
                sparks[i].transform.position = point;
                sparks[i].transform.localScale = Vector3.one * 0.4f * scale;

                float ang = (i * 360f / shardCount) + Random.Range(-15f, 15f);
                sparkVel[i] = (Quaternion.Euler(0f, 0f, ang) * normalDir) * (3.5f * scale);
                sparks[i].transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }

            const float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.95f * scale, t);
                ring.color = new Color(1f, 0.84f, 0.28f, (1f - t) * 0.95f);

                for (int i = 0; i < shardCount; i++)
                {
                    sparks[i].transform.position += (Vector3)(sparkVel[i] * Time.deltaTime);
                    sparks[i].color = new Color(1f, 1f, 1f, 1f - t);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void SpawnColossalImpactExplosion(Vector3 point, float radius)
        {
            GameObject blastHost = new GameObject("ADV_ColossalBlast");
            blastHost.transform.position = point;
            AnDuongVuongVfx blast = blastHost.AddComponent<AnDuongVuongVfx>();
            blast.StartCoroutine(blast.AnimateColossalBlast(point, radius));
        }

        private IEnumerator AnimateColossalBlast(Vector3 point, float radius)
        {
            // Colossal golden expanding shockwave
            SpriteRenderer blastRing = CreateRenderer("BlastRing", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 25, new Color(1f, 0.88f, 0.35f, 1f));
            blastRing.transform.position = point;
            blastRing.transform.localScale = Vector3.one * 0.3f;

            // Secondary cyan halo
            SpriteRenderer cyanHalo = CreateRenderer("CyanHalo", AnDuongVuongVisualFactory.GetDongSonAuraRingSprite(), 24, new Color(0.3f, 0.92f, 1f, 0.85f));
            cyanHalo.transform.position = point;
            cyanHalo.transform.localScale = Vector3.one * 0.2f;

            const float duration = 0.42f;
            float elapsed = 0f;
            float maxScale = (radius * 2.4f) / 1.28f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3);

                blastRing.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, maxScale, ease);
                blastRing.color = new Color(1f, 0.88f, 0.35f, (1f - t) * 0.95f);

                cyanHalo.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, maxScale * 0.8f, ease);
                cyanHalo.color = new Color(0.3f, 0.92f, 1f, (1f - t) * 0.85f);

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator FadeAndScale(SpriteRenderer r, float duration, float startScale, float endScale)
        {
            float elapsed = 0f;
            Color initCol = r.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                r.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
                r.color = new Color(initCol.r, initCol.g, initCol.b, initCol.a * (1f - t));
                yield return null;
            }
            Destroy(r.gameObject);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int sortingOrder, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            spawnedChildren.Add(obj);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            return sr;
        }

        private void OnDestroy()
        {
            foreach (GameObject child in spawnedChildren)
            {
                if (child != null)
                {
                    Destroy(child);
                }
            }
        }
    }
}
