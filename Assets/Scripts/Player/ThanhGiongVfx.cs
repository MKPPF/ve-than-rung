using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Spawns and animates runtime visual effects for all 5 moves of Thanh Giong:
    /// Iron Sword slash, Iron Sword + Iron Horse fire cleave, Single Bamboo smash,
    /// 360-degree Mounted Bamboo Bundle whirlwind, and Iron Horse + Bamboo Bundle charge.
    /// </summary>
    public sealed class ThanhGiongVfx : MonoBehaviour
    {
        private readonly List<GameObject> spawnedChildren = new List<GameObject>();

        public static void SpawnIronSwordSlash(Transform owner, Vector2 direction, float range)
        {
            if (owner == null)
            {
                return;
            }

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("ThanhGiong_IronSwordSlash");
            host.transform.position = owner.position;
            ThanhGiongVfx vfx = host.AddComponent<ThanhGiongVfx>();
            vfx.StartCoroutine(vfx.AnimateIronSwordSlash(owner, dir, range));
        }

        public static void SpawnSkill1IronSwordHorse(Transform owner, Vector2 direction, float range)
        {
            if (owner == null)
            {
                return;
            }

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("ThanhGiong_Skill1_IronSwordHorse");
            host.transform.position = owner.position;
            ThanhGiongVfx vfx = host.AddComponent<ThanhGiongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill1IronSwordHorse(owner, dir, range));
        }

        public static void SpawnSkill2SingleBambooSmash(Transform owner, Vector2 direction, float length)
        {
            if (owner == null)
            {
                return;
            }

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("ThanhGiong_Skill2_SingleBamboo");
            host.transform.position = owner.position;
            ThanhGiongVfx vfx = host.AddComponent<ThanhGiongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill2SingleBambooSmash(owner, dir, length));
        }

        public static void SpawnSkill3MountedBambooSpin360(Transform owner, float radius, float duration)
        {
            if (owner == null)
            {
                return;
            }

            GameObject host = new GameObject("ThanhGiong_Skill3_MountedSpin360");
            host.transform.position = owner.position;
            ThanhGiongVfx vfx = host.AddComponent<ThanhGiongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill3MountedBambooSpin360(owner, radius, duration));
        }

        public static void SpawnSkill4HorseBambooCharge(Transform owner, Vector2 direction, float duration)
        {
            if (owner == null)
            {
                return;
            }

            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject host = new GameObject("ThanhGiong_Skill4_HorseBambooCharge");
            host.transform.position = owner.position;
            ThanhGiongVfx vfx = host.AddComponent<ThanhGiongVfx>();
            vfx.StartCoroutine(vfx.AnimateSkill4HorseBambooCharge(owner, dir, duration));
        }

        private IEnumerator AnimateIronSwordSlash(Transform owner, Vector2 dir, float range)
        {
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            SpriteRenderer sword = CreateRenderer("IronSwordBlade", ThanhGiongVisualFactory.GetIronSwordSprite(), 18, Color.white);
            SpriteRenderer arc = CreateRenderer("IronSwordArc", ThanhGiongVisualFactory.GetSlashArcSprite(), 17, new Color(0.85f, 0.95f, 1f, 0.9f));

            float scale = Mathf.Max(0.85f, range * 0.58f);
            sword.transform.localScale = new Vector3(scale, scale, 1f);
            arc.transform.localScale = new Vector3(scale * 1.15f, scale * 1.15f, 1f);
            arc.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);

            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (owner != null)
                {
                    transform.position = owner.position;
                }

                float t = Mathf.Clamp01(elapsed / duration);
                float swingAngle = Mathf.Lerp(baseAngle + 65f, baseAngle - 65f, Mathf.SmoothStep(0f, 1f, t));
                sword.transform.position = transform.position + (Vector3)(dir * 0.18f);
                sword.transform.rotation = Quaternion.Euler(0f, 0f, swingAngle);
                arc.transform.position = transform.position + (Vector3)(dir * 0.35f);
                arc.color = new Color(0.9f, 0.96f, 1f, (1f - t) * 0.9f);
                sword.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - t) * 1.4f));

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill1IronSwordHorse(Transform owner, Vector2 dir, float range)
        {
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Phantom Iron Horse surging forward with fire breath + Giant Iron Sword cleave
            SpriteRenderer horsePhantom = CreateRenderer("IronHorsePhantom",
                ThanhGiongVisualFactory.GetMountedHeroSprite(1, true, false), 16, new Color(1f, 0.92f, 0.72f, 0.85f));
            horsePhantom.flipX = dir.x < -0.05f;
            horsePhantom.transform.localScale = Vector3.one * 1.05f;

            SpriteRenderer sword = CreateRenderer("GiantIronSword", ThanhGiongVisualFactory.GetIronSwordSprite(), 20, Color.white);
            SpriteRenderer fireArc = CreateRenderer("FireSwordArc", ThanhGiongVisualFactory.GetSlashArcSprite(), 19, new Color(1f, 0.65f, 0.15f, 0.95f));
            sword.transform.localScale = Vector3.one * (range * 0.68f);
            fireArc.transform.localScale = Vector3.one * (range * 0.82f);
            fireArc.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);

            // Fire breath particles in cone ahead
            SpriteRenderer[] flames = new SpriteRenderer[7];
            Vector2[] flameVelocities = new Vector2[flames.Length];
            for (int i = 0; i < flames.Length; i++)
            {
                flames[i] = CreateRenderer("HorseBreathFlame_" + i, ThanhGiongVisualFactory.GetFlameSprite(), 18, Color.white);
                float spread = (i - 3) * 13f;
                Vector2 flameDir = Quaternion.Euler(0f, 0f, spread) * dir;
                flameVelocities[i] = flameDir * (range * (1.8f + (i % 3) * 0.35f));
                flames[i].transform.position = transform.position + (Vector3)(dir * 0.45f);
                flames[i].transform.localScale = Vector3.one * 0.95f;
            }

            const float duration = 0.38f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (owner != null)
                {
                    transform.position = owner.position;
                }

                float t = Mathf.Clamp01(elapsed / duration);
                float swing = Mathf.Lerp(baseAngle + 80f, baseAngle - 80f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.35f)));
                horsePhantom.transform.position = transform.position + (Vector3)(dir * Mathf.Lerp(0.1f, 0.65f, t));
                horsePhantom.color = new Color(1f, 0.88f, 0.55f, (1f - t) * 0.78f);

                sword.transform.position = transform.position + (Vector3)(dir * 0.35f);
                sword.transform.rotation = Quaternion.Euler(0f, 0f, swing);
                sword.color = new Color(1f, 0.96f, 0.85f, Mathf.Clamp01((1f - t) * 1.4f));

                fireArc.transform.position = transform.position + (Vector3)(dir * (0.55f + t * 0.45f));
                fireArc.color = new Color(1f, 0.58f, 0.12f, (1f - t) * 0.95f);

                for (int i = 0; i < flames.Length; i++)
                {
                    flames[i].transform.position += (Vector3)(flameVelocities[i] * Time.deltaTime);
                    flames[i].transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.35f, t);
                    flames[i].color = new Color(1f, Mathf.Lerp(0.85f, 0.3f, t), 0.1f, 1f - t);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill2SingleBambooSmash(Transform owner, Vector2 dir, float length)
        {
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector3 origin = owner.position;

            // Giant Single Ivory Bamboo slamming onto enemies
            SpriteRenderer bamboo = CreateRenderer("SingleGiantBamboo", ThanhGiongVisualFactory.GetSingleBambooSprite(), 20, Color.white);
            SpriteRenderer ring = CreateRenderer("BambooShockwave", ThanhGiongVisualFactory.GetShockwaveRingSprite(), 14, new Color(0.88f, 0.95f, 0.35f, 0.9f));

            float bambooScale = Mathf.Max(1.25f, length / 2.8f);
            bamboo.transform.localScale = new Vector3(bambooScale, bambooScale * 1.15f, 1f);

            // Flying bamboo leaves
            SpriteRenderer[] leaves = new SpriteRenderer[10];
            Vector2[] leafVel = new Vector2[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                leaves[i] = CreateRenderer("BambooLeaf_" + i, ThanhGiongVisualFactory.GetBambooLeafSprite(), 21, Color.white);
                float frac = (i + 1f) / (leaves.Length + 1f);
                leaves[i].transform.position = origin + (Vector3)(dir * (length * frac));
                Vector2 perp = new Vector2(-dir.y, dir.x) * ((i % 2 == 0 ? 1f : -1f) * (1.4f + (i % 3) * 0.6f));
                leafVel[i] = dir * 1.8f + perp;
                leaves[i].transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + i * 32f);
            }

            const float duration = 0.42f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (owner != null)
                {
                    origin = owner.position;
                }

                float t = Mathf.Clamp01(elapsed / duration);
                // Fast whip-down in first 35%, then ground reverberation
                float swingT = Mathf.Clamp01(t / 0.35f);
                float angle = Mathf.Lerp(baseAngle + 55f, baseAngle, Mathf.SmoothStep(0f, 1f, swingT));
                bamboo.transform.position = origin + (Vector3)(dir * 0.25f);
                bamboo.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                bamboo.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - t) * 1.5f));

                ring.transform.position = origin + (Vector3)(dir * (length * 0.62f));
                float ringScale = Mathf.Lerp(0.4f, length * 0.55f, t);
                ring.transform.localScale = new Vector3(ringScale, ringScale * 0.55f, 1f);
                ring.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);
                ring.color = new Color(0.85f, 0.96f, 0.32f, (1f - t) * 0.85f);

                for (int i = 0; i < leaves.Length; i++)
                {
                    leaves[i].transform.position += (Vector3)(leafVel[i] * Time.deltaTime);
                    leaves[i].transform.Rotate(0f, 0f, 320f * Time.deltaTime);
                    leaves[i].color = new Color(1f, 1f, 1f, 1f - t);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill3MountedBambooSpin360(Transform owner, float radius, float duration)
        {
            // 3 bundles of Ivory Bamboo sweeping 360 degrees around the mounted hero + Iron Horse fire ring
            const int bundleCount = 3;
            SpriteRenderer[] bundles = new SpriteRenderer[bundleCount];
            float bundleScale = Mathf.Max(1.05f, radius / 2.65f);
            for (int i = 0; i < bundleCount; i++)
            {
                bundles[i] = CreateRenderer("SpinningBambooBundle_" + i, ThanhGiongVisualFactory.GetBambooBundleSprite(), 19, Color.white);
                bundles[i].transform.localScale = new Vector3(bundleScale, bundleScale, 1f);
            }

            SpriteRenderer outerRing = CreateRenderer("SpinWhirlwindRing", ThanhGiongVisualFactory.GetShockwaveRingSprite(), 13, new Color(0.88f, 0.94f, 0.32f, 0.75f));
            SpriteRenderer fireRing = CreateRenderer("HorseFireRing", ThanhGiongVisualFactory.GetShockwaveRingSprite(), 12, new Color(1f, 0.52f, 0.12f, 0.7f));

            const int leafCount = 12;
            SpriteRenderer[] leaves = new SpriteRenderer[leafCount];
            for (int i = 0; i < leafCount; i++)
            {
                leaves[i] = CreateRenderer("OrbitBambooLeaf_" + i, ThanhGiongVisualFactory.GetBambooLeafSprite(), 20, Color.white);
                leaves[i].transform.localScale = Vector3.one * 1.15f;
            }

            float elapsed = 0f;
            float spinDegrees = 0f;
            while (elapsed < duration && owner != null)
            {
                transform.position = owner.position;
                spinDegrees += 760f * Time.deltaTime;
                float fade = elapsed > duration - 0.2f ? Mathf.Clamp01((duration - elapsed) / 0.2f) : 1f;

                for (int i = 0; i < bundleCount; i++)
                {
                    float a = spinDegrees + i * (360f / bundleCount);
                    Vector2 offset = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 0.28f;
                    bundles[i].transform.position = transform.position + (Vector3)offset;
                    bundles[i].transform.rotation = Quaternion.Euler(0f, 0f, a);
                    bundles[i].color = new Color(1f, 1f, 1f, fade);
                }

                float pulse = 1f + Mathf.Sin(elapsed * 18f) * 0.08f;
                outerRing.transform.position = transform.position;
                outerRing.transform.localScale = Vector3.one * (radius * 0.85f * pulse);
                outerRing.color = new Color(0.84f, 0.95f, 0.28f, 0.65f * fade);

                fireRing.transform.position = transform.position;
                fireRing.transform.localScale = Vector3.one * (radius * 0.55f * (2f - pulse));
                fireRing.color = new Color(1f, 0.55f, 0.12f, 0.65f * fade);

                for (int i = 0; i < leafCount; i++)
                {
                    float leafAngle = (spinDegrees * 1.25f + i * (360f / leafCount)) * Mathf.Deg2Rad;
                    float r = Mathf.Lerp(radius * 0.45f, radius * 0.98f, (i % 3) * 0.5f);
                    leaves[i].transform.position = transform.position + new Vector3(Mathf.Cos(leafAngle) * r, Mathf.Sin(leafAngle) * r, 0f);
                    leaves[i].transform.rotation = Quaternion.Euler(0f, 0f, leafAngle * Mathf.Rad2Deg + 90f);
                    leaves[i].color = new Color(1f, 1f, 1f, fade);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateSkill4HorseBambooCharge(Transform owner, Vector2 dir, float duration)
        {
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Held Bamboo Bundle sweeping right in front of the charging Iron Horse
            SpriteRenderer heldBundle = CreateRenderer("ChargeBambooBundle", ThanhGiongVisualFactory.GetBambooBundleSprite(), 20, Color.white);
            heldBundle.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

            SpriteRenderer horseFireJet = CreateRenderer("HorseFireJet", ThanhGiongVisualFactory.GetSlashArcSprite(), 19, new Color(1f, 0.65f, 0.14f, 0.92f));
            horseFireJet.transform.localScale = Vector3.one * 1.35f;
            horseFireJet.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);

            const int trailCount = 12;
            SpriteRenderer[] trailFlames = new SpriteRenderer[trailCount];
            float[] trailSpawnTimes = new float[trailCount];
            int nextTrail = 0;
            for (int i = 0; i < trailCount; i++)
            {
                trailFlames[i] = CreateRenderer("HoofFireTrail_" + i, ThanhGiongVisualFactory.GetFlameSprite(), 9, Color.clear);
                trailSpawnTimes[i] = -10f;
            }

            float elapsed = 0f;
            float nextDropTime = 0f;
            while (elapsed < duration + 0.25f)
            {
                bool charging = elapsed <= duration && owner != null;
                if (charging)
                {
                    transform.position = owner.position;
                    float wave = Mathf.Sin(elapsed * 36f) * 18f;
                    heldBundle.transform.position = transform.position + (Vector3)(dir * 0.35f);
                    heldBundle.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + wave);
                    heldBundle.color = Color.white;

                    horseFireJet.transform.position = transform.position + (Vector3)(dir * 0.85f);
                    horseFireJet.color = new Color(1f, 0.62f, 0.12f, 0.9f);

                    if (elapsed >= nextDropTime)
                    {
                        nextDropTime = elapsed + 0.035f;
                        int idx = nextTrail % trailCount;
                        nextTrail++;
                        Vector2 side = new Vector2(-dir.y, dir.x) * ((idx % 2 == 0 ? 1f : -1f) * 0.28f);
                        trailFlames[idx].transform.position = transform.position - (Vector3)(dir * 0.25f) + (Vector3)side;
                        trailFlames[idx].transform.localScale = Vector3.one * 1.05f;
                        trailSpawnTimes[idx] = elapsed;
                    }
                }
                else
                {
                    heldBundle.color = Color.clear;
                    horseFireJet.color = Color.clear;
                }

                for (int i = 0; i < trailCount; i++)
                {
                    float age = elapsed - trailSpawnTimes[i];
                    if (age >= 0f && age <= 0.32f)
                    {
                        float t = age / 0.32f;
                        trailFlames[i].color = new Color(1f, Mathf.Lerp(0.85f, 0.25f, t), 0.1f, 1f - t);
                        trailFlames[i].transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.4f, t);
                    }
                    else
                    {
                        trailFlames[i].color = Color.clear;
                    }
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
