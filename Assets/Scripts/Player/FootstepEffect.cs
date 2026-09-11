using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Player
{
    public sealed class FootstepEffect : MonoBehaviour
    {
        private SpriteRenderer[] visuals;
        private Color[] startColors;
        private Vector3 startScale;
        private float lifetime;
        private float elapsed;

        public static void Spawn(Vector2 position, Vector2 moveDirection, bool leftFoot)
        {
            Vector2 direction = moveDirection.sqrMagnitude > 0.001f
                ? moveDirection.normalized
                : Vector2.down;
            Vector2 side = new Vector2(-direction.y, direction.x);

            GameObject stepObject = new GameObject(leftFoot ? "LeftFootstep" : "RightFootstep");
            stepObject.transform.position = position - direction * 0.2f +
                side * (leftFoot ? -0.13f : 0.13f);
            stepObject.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            stepObject.transform.localScale = Vector3.one;

            SpriteRenderer heel = CreateMark(stepObject.transform, "Heel",
                new Vector3(-0.055f, 0f, 0f), new Vector3(0.075f, 0.06f, 1f),
                new Color(0.16f, 0.11f, 0.06f, 0.2f));
            SpriteRenderer forefoot = CreateMark(stepObject.transform, "Forefoot",
                new Vector3(0.055f, 0f, 0f), new Vector3(0.1f, 0.052f, 1f),
                new Color(0.16f, 0.11f, 0.06f, 0.28f));

            FootstepEffect effect = stepObject.AddComponent<FootstepEffect>();
            effect.visuals = new[] { heel, forefoot };
            effect.startColors = new[] { heel.color, forefoot.color };
            effect.startScale = stepObject.transform.localScale;
            effect.lifetime = 0.45f;
        }

        private static SpriteRenderer CreateMark(Transform parent, string name,
            Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject mark = new GameObject(name);
            mark.transform.SetParent(parent, false);
            mark.transform.localPosition = localPosition;
            mark.transform.localScale = localScale;
            SpriteRenderer renderer = mark.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
            return renderer;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.06f, t);
            for (int i = 0; i < visuals.Length; i++)
            {
                Color color = startColors[i];
                color.a = startColors[i].a * (1f - t);
                visuals[i].color = color;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
