using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class FireballVisualEffect : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float pulseSpeed = 8f;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.1f;
        [SerializeField, Min(0f)] private float shakeAmount = 0.025f;
        [SerializeField, Min(0f)] private float shakeSpeed = 20f;

        private Vector3 originalScale;
        private Vector3 originalLocalPosition;

        private void Awake()
        {
            originalScale = transform.localScale;
            originalLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;

            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.2f) * shakeAmount;
            transform.localPosition = originalLocalPosition + new Vector3(shakeX, shakeY, 0f);
        }
    }
}
