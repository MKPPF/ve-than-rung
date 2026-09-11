using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Vector UI ornament stays crisp at every canvas scale instead of stretching a gameplay VFX sprite.</summary>
    public sealed class EquipmentMedallion : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.49f;
            Ring(mesh, center, radius, 1.25f, color);
            Color faint = color;
            faint.a *= 0.3f;
            Ring(mesh, center, radius - 10, 0.8f, faint);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                Vector2 start = center + direction * (radius - 4);
                Vector2 end = center + direction * (radius - (i % 3 == 0 ? 14 : 8));
                Quad(mesh, start - tangent * 0.65f, start + tangent * 0.65f,
                    end + tangent * 0.65f, end - tangent * 0.65f, color);
            }
        }

        private static void Ring(VertexHelper mesh, Vector2 center, float radius, float thickness, Color tint)
        {
            for (int i = 0; i < 128; i++)
            {
                float a = i * Mathf.PI * 2 / 128;
                float b = (i + 1) * Mathf.PI * 2 / 128;
                Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, center + first * radius, center + second * radius,
                    center + second * (radius - thickness), center + first * (radius - thickness), tint);
            }
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
