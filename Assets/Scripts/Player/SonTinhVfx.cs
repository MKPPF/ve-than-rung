using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Spawns and animates runtime visual effects for all 5 moves of Son Tinh:
    /// Stone-fist normal strike, Overhead Giant Boulder throw (Q), Skyward-palm Mountain Eruption
    /// at enemy zone (E), Ground-slam Cascading Mountain Ridges forward (R), and 360-degree
    /// Surrounding Mountain Fortress (F).
    /// </summary>
    public sealed class SonTinhVfx : MonoBehaviour
    {
        private readonly List<GameObject> spawnedChildren = new List<GameObject>();

        public static void SpawnNormalRockStrike(Transform owner, Vector2 direction, float range)
        {
            if (owner == null)
            {
                return;
            }

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("SonTinh_NormalRockStrike");
            host.transform.position = owner.position;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateNormalRockStrike(owner, dir, range));
        }

        public static void SpawnSkill1BoulderThrow(Vector3 origin, Vector2 targetPoint, float flightDuration, float impactRadius)
        {
            GameObject host = new GameObject("SonTinh_Skill1_BoulderThrow");
            host.transform.position = origin;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill1BoulderThrow(origin, targetPoint, flightDuration, impactRadius));
        }

        public static void SpawnSkill2RaiseMountainAtEnemy(Transform owner, Vector2 targetCenter, float radius, float preTremorDuration)
        {
            GameObject host = new GameObject("SonTinh_Skill2_RaiseMountain");
            host.transform.position = targetCenter;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill2RaiseMountain(owner, targetCenter, radius, preTremorDuration));
        }

        public static void SpawnSkill3GroundSlamOrigin(Vector2 origin, Vector2 direction)
        {
            GameObject host = new GameObject("SonTinh_Skill3_GroundSlamOrigin");
            host.transform.position = origin;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateGroundSlamOrigin(origin, direction));
        }

        public static void SpawnSkill3MountainRidgeWave(Vector2 ridgeCenter, Vector2 forwardDir, float ridgeWidth, int waveIndex)
        {
            GameObject host = new GameObject("SonTinh_Skill3_MountainRidge_" + waveIndex);
            host.transform.position = ridgeCenter;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill3MountainRidgeWave(ridgeCenter, forwardDir, ridgeWidth, waveIndex));
        }

        public static void SpawnSkill4SurroundingMountains(Transform owner, float radius, float duration)
        {
            if (owner == null)
            {
                return;
            }

            GameObject host = new GameObject("SonTinh_Skill4_SurroundingMountains");
            host.transform.position = owner.position;
            SonTinhVfx vfx = host.AddComponent<SonTinhVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill4SurroundingMountains(owner, radius, duration));
        }

        private IEnumerator AnimateNormalRockStrike(Transform owner, Vector2 dir, float range)
        {
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            SpriteRenderer fistRock = CreateRenderer("RockFistMass", SonTinhVisualFactory.GetGiantBoulderSprite(), 18, Color.white);
            SpriteRenderer ring = CreateRenderer("RockShockRing", SonTinhVisualFactory.GetShockwaveRingSprite(), 17, new Color(1f, 0.85f, 0.35f, 0.9f));

            fistRock.transform.localScale = Vector3.one * 0.62f;
            ring.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            SpriteRenderer[] shards = new SpriteRenderer[5];
            Vector2[] shardVel = new Vector2[shards.Length];
            for (int i = 0; i < shards.Length; i++)
            {
                shards[i] = CreateRenderer("RockShard_" + i, SonTinhVisualFactory.GetRockShardSprite(), 19, Color.white);
                float spread = (i - 2) * 18f;
                Vector2 sDir = Quaternion.Euler(0f, 0f, spread) * dir;
                shardVel[i] = sDir * (range * (2.2f + (i % 2) * 0.4f));
                shards[i].transform.position = transform.position + (Vector3)(dir * 0.45f);
                shards[i].transform.localScale = Vector3.one * 0.85f;
            }

            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (owner != null)
                {
                    transform.position = owner.position;
                }

                float t = Mathf.Clamp01(elapsed / duration);
                fistRock.transform.position = transform.position + (Vector3)(dir * Mathf.Lerp(0.45f, range * 0.68f, t));
                fistRock.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg - t * 90f);
                fistRock.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - t) * 1.4f));

                ring.transform.position = transform.position + (Vector3)(dir * (range * 0.55f));
                float rScale = Mathf.Lerp(0.35f, range * 0.55f, t);
                ring.transform.localScale = new Vector3(rScale, rScale * 0.72f, 1f);
                ring.color = new Color(1f, 0.82f, 0.28f, (1f - t) * 0.85f);

                for (int i = 0; i < shards.Length; i++)
                {
                    shards[i].transform.position += (Vector3)(shardVel[i] * Time.deltaTime);
                    shards[i].transform.Rotate(0f, 0f, 360f * Time.deltaTime);
                    shards[i].color = new Color(1f, 1f, 1f, 1f - t);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill1BoulderThrow(Vector3 origin, Vector2 targetPoint, float flightDuration, float impactRadius)
        {
            Vector2 startGround = origin;
            Vector2 startAirOffset = new Vector2(0f, 0.92f);

            SpriteRenderer shadow = CreateRenderer("BoulderShadow", SonTinhVisualFactory.GetEarthFissureSprite(), 6, new Color(0f, 0f, 0f, 0.45f));
            shadow.transform.localScale = new Vector3(0.95f, 0.55f, 1f);

            SpriteRenderer boulder = CreateRenderer("FlyingGiantBoulder", SonTinhVisualFactory.GetGiantBoulderSprite(), 22, Color.white);
            boulder.transform.localScale = Vector3.one * 1.38f;

            float safeFlight = Mathf.Max(0.08f, flightDuration);
            float elapsed = 0f;
            while (elapsed < safeFlight)
            {
                float t = Mathf.Clamp01(elapsed / safeFlight);
                Vector2 groundPos = Vector2.Lerp(startGround, targetPoint, t);
                float arcHeight = Mathf.Lerp(startAirOffset.y, 0f, t) + Mathf.Sin(t * Mathf.PI) * 1.45f;

                shadow.transform.position = groundPos;
                float shadowScale = Mathf.Lerp(0.85f, 1.25f, t);
                shadow.transform.localScale = new Vector3(shadowScale, shadowScale * 0.55f, 1f);

                boulder.transform.position = groundPos + new Vector2(0f, arcHeight);
                boulder.transform.rotation = Quaternion.Euler(0f, 0f, -t * 280f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Heavy Impact Shatter!
            shadow.color = Color.clear;
            boulder.transform.position = targetPoint;
            boulder.transform.localScale = new Vector3(1.55f, 1.15f, 1f);

            SpriteRenderer crater = CreateRenderer("ImpactCrater", SonTinhVisualFactory.GetEarthFissureSprite(), 7, Color.white);
            crater.transform.position = targetPoint;
            crater.transform.localScale = Vector3.one * (impactRadius * 0.92f);

            SpriteRenderer shockwave = CreateRenderer("ImpactShockwave", SonTinhVisualFactory.GetShockwaveRingSprite(), 20, new Color(1f, 0.84f, 0.28f, 0.95f));
            shockwave.transform.position = targetPoint;

            const int shardCount = 9;
            SpriteRenderer[] shards = new SpriteRenderer[shardCount];
            Vector2[] shardVel = new Vector2[shardCount];
            for (int i = 0; i < shardCount; i++)
            {
                shards[i] = CreateRenderer("BoulderDebris_" + i, SonTinhVisualFactory.GetRockShardSprite(), 23, Color.white);
                float ang = (i * 360f / shardCount + 15f) * Mathf.Deg2Rad;
                shardVel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (impactRadius * (1.7f + (i % 3) * 0.45f));
                shards[i].transform.position = targetPoint;
                shards[i].transform.localScale = Vector3.one * (0.95f + (i % 2) * 0.35f);
            }

            const float impactFadeDuration = 0.42f;
            elapsed = 0f;
            while (elapsed < impactFadeDuration)
            {
                float t = Mathf.Clamp01(elapsed / impactFadeDuration);
                boulder.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t * 2.2f));
                boulder.transform.localScale = Vector3.Lerp(new Vector3(1.55f, 1.15f, 1f), new Vector3(1.8f, 0.7f, 1f), t);

                float ringScale = Mathf.Lerp(0.35f, impactRadius * 0.95f, Mathf.SmoothStep(0f, 1f, t));
                shockwave.transform.localScale = Vector3.one * ringScale;
                shockwave.color = new Color(1f, 0.80f, 0.25f, (1f - t) * 0.95f);

                crater.color = new Color(1f, 1f, 1f, 1f - t * 0.85f);

                for (int i = 0; i < shardCount; i++)
                {
                    shards[i].transform.position += (Vector3)(shardVel[i] * Time.deltaTime);
                    shards[i].transform.Rotate(0f, 0f, 420f * Time.deltaTime);
                    shards[i].color = new Color(1f, 1f, 1f, 1f - t);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill2RaiseMountain(Transform owner, Vector2 targetCenter, float radius, float preTremorDuration)
        {
            // Golden divine uplift ring at Son Tinh's skyward palms
            SpriteRenderer casterAura = CreateRenderer("CasterSkywardAura", SonTinhVisualFactory.GetShockwaveRingSprite(), 19, new Color(1f, 0.88f, 0.32f, 0.85f));
            if (owner != null)
            {
                casterAura.transform.position = owner.position + new Vector3(0f, 0.35f, 0f);
            }

            // Seismic warning crack under enemy cluster
            SpriteRenderer fissure = CreateRenderer("EnemyZoneFissure", SonTinhVisualFactory.GetEarthFissureSprite(), 7, Color.white);
            fissure.transform.position = targetCenter;
            fissure.transform.localScale = Vector3.one * (radius * 0.9f);

            SpriteRenderer tremorRing = CreateRenderer("TremorRing", SonTinhVisualFactory.GetShockwaveRingSprite(), 14, new Color(1f, 0.72f, 0.18f, 0.9f));
            tremorRing.transform.position = targetCenter;

            float preTime = Mathf.Max(0.06f, preTremorDuration);
            float elapsed = 0f;
            while (elapsed < preTime)
            {
                float t = Mathf.Clamp01(elapsed / preTime);
                if (owner != null)
                {
                    casterAura.transform.position = owner.position + new Vector3(0f, 0.2f + t * 0.45f, 0f);
                    casterAura.transform.localScale = new Vector3(0.95f, 0.55f, 1f) * Mathf.Lerp(0.6f, 1.15f, t);
                }

                tremorRing.transform.localScale = Vector3.one * Mathf.Lerp(radius * 0.85f, radius * 0.35f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Colossal 3-Peak Mountain Massif Erupts Upward at Enemy Area!
            SpriteRenderer leftPeak = CreateRenderer("LeftMountainPeak", SonTinhVisualFactory.GetMountainPeakSprite(1), 16, Color.white);
            SpriteRenderer rightPeak = CreateRenderer("RightMountainPeak", SonTinhVisualFactory.GetMountainPeakSprite(2), 16, Color.white);
            SpriteRenderer centerPeak = CreateRenderer("CenterMountainPeak", SonTinhVisualFactory.GetMountainPeakSprite(0), 17, Color.white);

            Vector2 leftPos = targetCenter + new Vector2(-radius * 0.36f, -0.08f);
            Vector2 rightPos = targetCenter + new Vector2(radius * 0.36f, -0.08f);
            Vector2 mainPos = targetCenter + new Vector2(0f, 0.05f);

            leftPeak.transform.position = leftPos;
            rightPeak.transform.position = rightPos;
            centerPeak.transform.position = mainPos;

            float mainTargetScale = Mathf.Max(1.25f, radius * 0.56f);
            float sideTargetScale = mainTargetScale * 0.80f;

            const int debrisCount = 8;
            SpriteRenderer[] debris = new SpriteRenderer[debrisCount];
            Vector2[] debrisVel = new Vector2[debrisCount];
            for (int i = 0; i < debrisCount; i++)
            {
                debris[i] = CreateRenderer("MountainDebris_" + i, SonTinhVisualFactory.GetRockShardSprite(), 21, Color.white);
                float ang = (i * 360f / debrisCount) * Mathf.Deg2Rad;
                debrisVel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.7f + 0.45f) * (radius * 1.4f);
                debris[i].transform.position = targetCenter;
            }

            const float totalDuration = 1.15f;
            elapsed = 0f;
            while (elapsed < totalDuration)
            {
                float t = Mathf.Clamp01(elapsed / totalDuration);
                // Fast upward eruption in first 16%, hold strong until 76%, then sink into earth
                float riseFactor;
                if (t < 0.16f)
                {
                    float u = t / 0.16f;
                    riseFactor = Mathf.SmoothStep(0.18f, 1.12f, u);
                }
                else if (t < 0.76f)
                {
                    riseFactor = Mathf.Lerp(1.12f, 1.0f, (t - 0.16f) / 0.60f);
                }
                else
                {
                    riseFactor = Mathf.Lerp(1.0f, 0.08f, (t - 0.76f) / 0.24f);
                }

                float alpha = t > 0.75f ? Mathf.Clamp01((1f - t) / 0.25f) : 1f;
                centerPeak.transform.localScale = new Vector3(mainTargetScale, mainTargetScale * riseFactor, 1f);
                leftPeak.transform.localScale = new Vector3(sideTargetScale, sideTargetScale * Mathf.Clamp01(riseFactor * 0.95f), 1f);
                rightPeak.transform.localScale = new Vector3(sideTargetScale, sideTargetScale * Mathf.Clamp01(riseFactor * 0.95f), 1f);

                Color peakColor = new Color(1f, 1f, 1f, alpha);
                centerPeak.color = peakColor;
                leftPeak.color = peakColor;
                rightPeak.color = peakColor;
                fissure.color = new Color(1f, 1f, 1f, alpha * 0.9f);

                casterAura.color = new Color(1f, 0.88f, 0.32f, Mathf.Clamp01(1f - t * 2f));
                tremorRing.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, radius * 0.95f, Mathf.Clamp01(t * 1.8f));
                tremorRing.color = new Color(1f, 0.78f, 0.24f, Mathf.Clamp01(1f - t * 1.6f));

                for (int i = 0; i < debrisCount; i++)
                {
                    debris[i].transform.position += (Vector3)(debrisVel[i] * Time.deltaTime);
                    debris[i].transform.Rotate(0f, 0f, 300f * Time.deltaTime);
                    debris[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t * 1.4f));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateGroundSlamOrigin(Vector2 origin, Vector2 dir)
        {
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            SpriteRenderer crack = CreateRenderer("SlamOriginCrack", SonTinhVisualFactory.GetEarthFissureSprite(), 8, Color.white);
            crack.transform.position = origin + dir * 0.35f;
            crack.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            crack.transform.localScale = new Vector3(1.3f, 1.1f, 1f);

            SpriteRenderer ring = CreateRenderer("SlamOriginRing", SonTinhVisualFactory.GetShockwaveRingSprite(), 18, new Color(1f, 0.72f, 0.18f, 0.95f));
            ring.transform.position = origin + dir * 0.35f;

            const float duration = 0.38f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.45f, t);
                ring.color = new Color(1f, 0.76f, 0.22f, 1f - t);
                crack.color = new Color(1f, 1f, 1f, 1f - t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill3MountainRidgeWave(Vector2 ridgeCenter, Vector2 forwardDir, float ridgeWidth, int waveIndex)
        {
            Vector2 dir = forwardDir.sqrMagnitude > 0.001f ? forwardDir.normalized : Vector2.right;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            SpriteRenderer fissure = CreateRenderer("RidgeFissure", SonTinhVisualFactory.GetEarthFissureSprite(), 7, Color.white);
            fissure.transform.position = ridgeCenter;
            fissure.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg + 90f);
            fissure.transform.localScale = new Vector3(ridgeWidth * 0.65f, 0.95f, 1f);

            SpriteRenderer shock = CreateRenderer("RidgeShock", SonTinhVisualFactory.GetShockwaveRingSprite(), 14, new Color(1f, 0.76f, 0.22f, 0.85f));
            shock.transform.position = ridgeCenter;

            // 3 peaks across the ridge width, growing taller with each advancing wave!
            float waveGrowth = 1f + waveIndex * 0.08f;
            float baseScale = Mathf.Max(0.95f, ridgeWidth * 0.42f) * waveGrowth;

            SpriteRenderer[] peaks = new SpriteRenderer[3];
            float[] peakScales = { baseScale * 0.82f, baseScale * 1.08f, baseScale * 0.82f };
            for (int i = 0; i < 3; i++)
            {
                peaks[i] = CreateRenderer("RidgePeak_" + i, SonTinhVisualFactory.GetMountainPeakSprite((waveIndex + i) % 3), 16 + (i == 1 ? 1 : 0), Color.white);
                Vector2 offset = perp * ((i - 1) * (ridgeWidth * 0.34f));
                peaks[i].transform.position = ridgeCenter + offset;
            }

            const float duration = 0.88f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float rise = t < 0.18f
                    ? Mathf.SmoothStep(0.18f, 1.12f, t / 0.18f)
                    : (t < 0.74f ? 1.04f : Mathf.Lerp(1.04f, 0.08f, (t - 0.74f) / 0.26f));
                float alpha = t > 0.76f ? Mathf.Clamp01((1f - t) / 0.24f) : 1f;

                for (int i = 0; i < 3; i++)
                {
                    peaks[i].transform.localScale = new Vector3(peakScales[i], peakScales[i] * rise, 1f);
                    peaks[i].color = new Color(1f, 1f, 1f, alpha);
                }

                fissure.color = new Color(1f, 1f, 1f, alpha * 0.9f);
                shock.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, ridgeWidth * 0.62f, Mathf.Clamp01(t * 2f));
                shock.color = new Color(1f, 0.76f, 0.22f, Mathf.Clamp01(1f - t * 1.8f));

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill4SurroundingMountains(Transform owner, float radius, float duration)
        {
            Vector3 center = owner.position;

            SpriteRenderer innerRing = CreateRenderer("FortressInnerShock", SonTinhVisualFactory.GetShockwaveRingSprite(), 12, new Color(1f, 0.85f, 0.28f, 0.85f));
            SpriteRenderer outerRing = CreateRenderer("FortressOuterShock", SonTinhVisualFactory.GetShockwaveRingSprite(), 12, new Color(1f, 0.68f, 0.18f, 0.85f));
            innerRing.transform.position = center;
            outerRing.transform.position = center;

            // Inner ring (6 mountain peaks) + Outer ring (10 mountain peaks) encircling Son Tinh 360°!
            const int innerCount = 6;
            const int outerCount = 10;
            int totalPeaks = innerCount + outerCount;
            SpriteRenderer[] peaks = new SpriteRenderer[totalPeaks];
            float[] peakDelays = new float[totalPeaks];
            float[] peakTargetScales = new float[totalPeaks];

            for (int i = 0; i < innerCount; i++)
            {
                float ang = (i * 360f / innerCount) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * (radius * 0.50f);
                int order = pos.y < center.y ? 19 : 15;
                peaks[i] = CreateRenderer("SurroundingInnerPeak_" + i, SonTinhVisualFactory.GetMountainPeakSprite(i % 3), order, Color.white);
                peaks[i].transform.position = pos;
                peakDelays[i] = 0.02f * (i % 3);
                peakTargetScales[i] = 0.92f;
            }

            for (int j = 0; j < outerCount; j++)
            {
                int idx = innerCount + j;
                float ang = ((j + 0.5f) * 360f / outerCount) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * (radius * 0.86f);
                int order = pos.y < center.y ? 20 : 14;
                peaks[idx] = CreateRenderer("SurroundingOuterPeak_" + j, SonTinhVisualFactory.GetMountainPeakSprite((j + 1) % 3), order, Color.white);
                peaks[idx].transform.position = pos;
                peakDelays[idx] = 0.08f + 0.015f * (j % 4);
                peakTargetScales[idx] = 1.16f;
            }

            float safeDuration = Mathf.Max(1.35f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                float globalT = Mathf.Clamp01(elapsed / safeDuration);
                float pulse = 1f + Mathf.Sin(elapsed * 16f) * 0.07f;
                float fade = globalT > 0.80f ? Mathf.Clamp01((1f - globalT) / 0.20f) : 1f;

                innerRing.transform.localScale = Vector3.one * (radius * 0.52f * pulse);
                innerRing.color = new Color(1f, 0.86f, 0.30f, 0.70f * fade);

                outerRing.transform.localScale = Vector3.one * (radius * 0.90f * (2f - pulse));
                outerRing.color = new Color(1f, 0.66f, 0.16f, 0.72f * fade);

                for (int i = 0; i < totalPeaks; i++)
                {
                    float localElapsed = Mathf.Max(0f, elapsed - peakDelays[i]);
                    float rise = localElapsed < 0.14f
                        ? Mathf.SmoothStep(0.20f, 1.08f, localElapsed / 0.14f)
                        : (globalT > 0.80f ? Mathf.Lerp(1.0f, 0.08f, (globalT - 0.80f) / 0.20f) : 1.0f);
                    float sc = peakTargetScales[i];
                    peaks[i].transform.localScale = new Vector3(sc, sc * rise, 1f);
                    peaks[i].color = new Color(1f, 1f, 1f, fade);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private SpriteRenderer CreateRenderer(string childName, Sprite sprite, int sortingOrder, Color color)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            spawnedChildren.Add(child);
            return sr;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < spawnedChildren.Count; i++)
            {
                if (spawnedChildren[i] != null)
                {
                    Destroy(spawnedChildren[i]);
                }
            }

            spawnedChildren.Clear();
        }
    }
}
