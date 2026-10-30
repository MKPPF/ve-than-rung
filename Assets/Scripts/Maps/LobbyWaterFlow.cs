using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Refracts only authored water ribbons over the original painting; banks and walkable scenery remain perfectly still.</summary>
    public sealed class LobbyWaterFlow : MonoBehaviour
    {
        [Header("Builder References")]
        [SerializeField] private Sprite sourceArt;
        [SerializeField] private Shader flowShader;
        [SerializeField] private Vector2[] streamCenters;
        [SerializeField] private Vector2 waterfallTop;
        [SerializeField] private Vector2 waterfallBottom;
        [Header("Presentation")]
        [SerializeField, Range(0.2f, 2f)] private float flowSpeed = 1f;
        private GameObject visualRoot;
        private Material streamMaterial, fallMaterial;
        private Mesh streamMesh, fallMesh;
        private SpriteRenderer[] foam;
        private SpriteRenderer[] spray;
        private float elapsed;
        private static readonly int FlowTime = Shader.PropertyToID("_FlowTime");
        private static readonly float[] HalfWidths = { 0.25f,1.0f,1.65f,2.0f,1.65f,0.55f };

        public bool IsReady => streamMaterial != null && fallMaterial != null;
        public float AnimationTime => elapsed;
        public int VisualCount => visualRoot != null ? visualRoot.transform.childCount : 0;

        private void Start()
        {
            if (sourceArt == null || flowShader == null || !flowShader.isSupported ||
                streamCenters == null || streamCenters.Length != HalfWidths.Length)
            {
                Debug.LogWarning("Lobby water needs its authored source sprite and supported shader.", this);
                enabled = false;
                return;
            }
            visualRoot = new GameObject("LivingWater");
            visualRoot.transform.SetParent(transform, false);
            streamMaterial = new Material(flowShader) { name = "LobbyStream_Runtime" };
            streamMaterial.mainTexture = sourceArt.texture;
            fallMaterial = new Material(streamMaterial) { name = "LobbyCascade_Runtime" };
            fallMaterial.SetFloat("_Fall",1);
            streamMesh = BuildRibbon(streamCenters, false);
            Vector2[] waterfall = { waterfallTop, Vector2.Lerp(waterfallTop,waterfallBottom,0.4f), waterfallBottom };
            fallMesh = BuildRibbon(waterfall, true);
            AddMesh("FlowingStream",streamMesh,streamMaterial);
            AddMesh("FallingWater",fallMesh,fallMaterial);
            foam = new SpriteRenderer[14];
            for (int i=0;i<foam.Length;i++) foam[i] = Create("Foam_" + i,RuntimeSpriteFactory.SoftGlowSprite);
            spray = new SpriteRenderer[12];
            for (int i=0;i<spray.Length;i++) spray[i] = Create("Spray_" + i,RuntimeSpriteFactory.CircleSprite);
        }

        private Mesh BuildRibbon(Vector2[] centers, bool waterfall)
        {
            const int Across = 12;
            int length = centers.Length;
            var vertices = new Vector3[length*(Across+1)];
            var uv = new Vector2[vertices.Length];
            var river = new Vector2[vertices.Length];
            var triangles = new int[(length-1)*Across*6];
            int index=0;
            for (int row=0;row<length;row++)
            {
                float width = waterfall ? 0.25f : HalfWidths[Mathf.Min(row,HalfWidths.Length-1)];
                for (int x=0;x<=Across;x++)
                {
                    int vertex=row*(Across+1)+x;
                    Vector2 position=centers[row]+Vector2.right*((x/(float)Across*2-1)*width);
                    vertices[vertex]=position;
                    uv[vertex]=new Vector2(position.x/26f+0.5f,position.y/15.6f+0.5f);
                    river[vertex]=new Vector2(x/(float)Across,row/(float)(length-1));
                    if (row==length-1 || x==Across) continue;
                    triangles[index++]=vertex; triangles[index++]=vertex+Across+1; triangles[index++]=vertex+1;
                    triangles[index++]=vertex+1; triangles[index++]=vertex+Across+1; triangles[index++]=vertex+Across+2;
                }
            }
            var mesh=new Mesh {name=waterfall ? "CascadeRibbon_Runtime" : "StreamRibbon_Runtime"};
            mesh.vertices=vertices; mesh.uv=uv; mesh.uv2=river; mesh.triangles=triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddMesh(string name,Mesh mesh,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(visualRoot.transform,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.sortingOrder=-19;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }

        private SpriteRenderer Create(string name,Sprite sprite)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));
            obj.transform.SetParent(visualRoot.transform,false);
            var renderer=obj.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.sortingOrder=-17;
            return renderer;
        }

        private void Update()
        {
            if (!IsReady || streamMaterial == null || fallMaterial == null || streamCenters == null || streamCenters.Length < 2) return;
            elapsed += Time.deltaTime * flowSpeed;
            streamMaterial.SetFloat(FlowTime, elapsed);
            fallMaterial.SetFloat(FlowTime, elapsed);
            if (foam != null)
            {
                for (int i = 0; i < foam.Length; i++)
                {
                    if (foam[i] == null) continue;
                    float t = Mathf.Repeat(elapsed * 0.23f + i * 0.618034f, 1);
                    float progress = t * (streamCenters.Length - 1);
                    int segment = Mathf.Min(Mathf.FloorToInt(progress), streamCenters.Length - 2);
                    Vector2 center = Vector2.Lerp(streamCenters[segment], streamCenters[segment + 1], progress - segment);
                    float side = Mathf.Sin(i * 8.1f) * 0.45f;
                    center.x += side * Mathf.Lerp(HalfWidths[segment], HalfWidths[segment + 1], progress - segment);
                    foam[i].transform.localPosition = center;
                    foam[i].transform.localScale = new Vector3(0.10f + 0.08f * Mathf.Sin(i * 1.5f), 0.022f, 1);
                    foam[i].color = new Color(0.72f, 0.96f, 1f, Mathf.Sin(t * Mathf.PI) * 0.35f);
                }
            }
            if (spray != null && spray.Length > 0)
            {
                for (int i = 0; i < spray.Length; i++)
                {
                    if (spray[i] == null) continue;
                    float t = Mathf.Repeat(elapsed * 0.82f + i / (float)spray.Length, 1);
                    float side = Mathf.Sin(i * 2.4f);
                    spray[i].transform.localPosition = waterfallBottom + new Vector2(side * t * 0.28f, Mathf.Sin(t * Mathf.PI) * 0.2f - t * 0.16f);
                    spray[i].transform.localScale = Vector3.one * (0.018f + 0.014f * (i % 3));
                    spray[i].color = new Color(0.8f, 0.96f, 1f, Mathf.Sin(t * Mathf.PI) * 0.35f);
                }
            }
        }

        private void OnEnable() { if(visualRoot!=null) visualRoot.SetActive(true); }
        private void OnDisable() { if(visualRoot!=null) visualRoot.SetActive(false); }
        private void OnDestroy()
        {
            if(visualRoot!=null) Destroy(visualRoot);
            if(streamMaterial!=null) Destroy(streamMaterial);
            if(fallMaterial!=null) Destroy(fallMaterial);
            if(streamMesh!=null) Destroy(streamMesh);
            if(fallMesh!=null) Destroy(fallMesh);
        }
    }
}
