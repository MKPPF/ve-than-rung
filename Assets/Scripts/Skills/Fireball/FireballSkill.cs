using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class FireballSkill : SkillBase
    {
        [SerializeField, Min(1f)] private float projectileSpeed = 8f;
        [SerializeField, Min(0f)] private float explosionRadius = 1.5f;
        [SerializeField] private LayerMask enemyLayers = ~0;
        [SerializeField] private LayerMask collisionLayers;

        public override void Cast()
        {
            PlayerCombat combat = GetComponent<PlayerCombat>();
            Vector2 direction = combat != null ? combat.AimDirection : Vector2.right;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject projectileObject = new GameObject("FireballProjectile2D");
            projectileObject.transform.SetPositionAndRotation(transform.position,
                Quaternion.Euler(0f, 0f, angle));

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.28f;
            Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Projectile,transform.position,1.65f,4f,projectileObject.transform,direction);
            TrailRenderer trail = projectileObject.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.minVertexDistance = 0.06f;
            trail.startWidth = 0.28f;
            trail.endWidth = 0.02f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.sortingOrder = 26;
            trail.startColor = new Color(1f, 0.64f, 0.12f, 0.6f);
            trail.endColor = new Color(1f, 0.12f, 0.01f, 0f);
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.color = Color.white;
            trail.material = trailMaterial;

            FireballProjectile2D projectile = projectileObject.AddComponent<FireballProjectile2D>();
            projectile.SetPresentation(Data.Presentation);
            projectile.OwnRuntimeMaterial(trailMaterial);
            projectile.Initialize(direction, projectileSpeed, ScaledDamage(), explosionRadius,
                enemyLayers, collisionLayers, gameObject);
            SkillEffect.SpawnBurst(transform.position, Data.EffectColor, 0.6f, 0.16f);
        }
    }
}
