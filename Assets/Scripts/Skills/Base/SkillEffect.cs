using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Lightweight runtime burst used by skills. It avoids external VFX packages
    /// while still giving every cast a readable color, scale and fade animation.
    /// </summary>
    public sealed class SkillEffect : MonoBehaviour
    {
        public static void SpawnBurst(Vector3 position, Color effectColor, float radius,
            float duration = 0.24f)
        {
            SpellPresentation.Impact(position,effectColor,radius,duration);
        }
    }
}
