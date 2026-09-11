using TMPro;
using UnityEngine;

namespace TopDownGame.Combat
{
    public sealed class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.1f)] private float lifetime = 0.7f;
        [SerializeField] private float riseSpeed = 1.2f;

        private float remainingLifetime;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }

            remainingLifetime = lifetime;
        }

        private void Update()
        {
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            remainingLifetime -= Time.deltaTime;

            if (label != null)
            {
                Color color = label.color;
                color.a = Mathf.Clamp01(remainingLifetime / lifetime);
                label.color = color;
            }

            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(float amount, bool critical)
        {
            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }

            if (label != null)
            {
                label.text = Mathf.CeilToInt(amount).ToString();
                label.color = critical ? new Color(1f, 0.75f, 0.1f) : Color.white;
                label.fontSize = critical ? 5f : 4f;
            }
        }

        public static void Spawn(Vector3 position, float amount, bool critical, DamagePopup prefab)
        {
            if (prefab == null)
            {
                return;
            }

            DamagePopup popup = Instantiate(prefab, position, Quaternion.identity);
            popup.Initialize(amount, critical);
        }
    }
}
