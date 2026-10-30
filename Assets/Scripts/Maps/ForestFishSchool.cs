using System.Collections.Generic;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>
    /// Decorative fish reuse the project's lightweight body/tail geometry style.
    /// Inset authored habitats keep the entire silhouette in water; fish do not
    /// have colliders, loot or save state. No per-frame object allocation.
    /// </summary>
    public sealed class ForestFishSchool : MonoBehaviour
    {
        [Header("Builder Water Habitats")]
        [SerializeField] private Vector2[] habitatCenters;
        [SerializeField] private Vector2[] habitatRadii;
        [SerializeField, Min(0)] private int pondHabitatCount = 2;
        [SerializeField, Range(1,6)] private int fishPerPondHabitat = 4;
        [SerializeField, Range(1,4)] private int fishPerRiverHabitat = 2;
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private GameObject visualRoot;
        private Transform[] fish, tails;
        private SpriteRenderer[] wakes;
        private int[] habitats;
        private float[] lengths;
        private Material material;
        private float elapsed;
        public float AnimationTime => elapsed;
        public int FishCount => fish != null ? fish.Length : 0;
        public int PondFishCount => Mathf.Min(pondHabitatCount, HabitatCount) * fishPerPondHabitat;
        public int RiverFishCount => FishCount - PondFishCount;
        public int HabitatCount => habitatCenters == null || habitatRadii == null ? 0 : Mathf.Min(habitatCenters.Length, habitatRadii.Length);
        public bool FishStayInWater
        {
            get
            {
                if (fish == null) return false;
                for (int i = 0; i < fish.Length; i++)
                {
                    Vector2 p = fish[i].localPosition;
                    Vector2 r = habitatRadii[habitats[i]];
                    Vector2 d = p - habitatCenters[habitats[i]];
                    // Conservative body bounding circle leaves a shore margin.
                    float radius = lengths[i] * 0.5f;
                    float x = d.x / Mathf.Max(0.01f, r.x-radius);
                    float y = d.y / Mathf.Max(0.01f, r.y-radius);
                    if (x*x+y*y > 1.001f) return false;
                }
                return true;
            }
        }

        private void Start()
        {
            if (HabitatCount == 0) { enabled = false; return; }
            int count = 0;
            for (int h = 0; h < HabitatCount; h++)
                count += h < pondHabitatCount ? fishPerPondHabitat : fishPerRiverHabitat;
            visualRoot = new GameObject("ForestFish_Visuals");
            visualRoot.transform.SetParent(transform, false);
            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = Texture2D.whiteTexture;
            fish = new Transform[count]; tails = new Transform[count];
            wakes = new SpriteRenderer[count]; habitats = new int[count]; lengths = new float[count];
            Color[] colors = {new Color(1f,0.55f,0.22f,0.83f),new Color(0.94f,0.86f,0.6f,0.87f),
                new Color(0.7f,0.88f,0.91f,0.85f),new Color(0.97f,0.73f,0.3f,0.84f)};
            for (int h = 0, i = 0; h < HabitatCount; h++)
            for (int j = 0; j < (h < pondHabitatCount ? fishPerPondHabitat : fishPerRiverHabitat); j++, i++)
            {
                habitats[i] = h;
                lengths[i] = h < pondHabitatCount ? 0.36f + j*0.025f : 0.22f;
                var root = new GameObject((h < pondHabitatCount ? "PondFish_" : "RiverFish_") + i);
                root.transform.SetParent(visualRoot.transform, false);
                fish[i] = root.transform;
                Color color = colors[i % colors.Length];
                float length = lengths[i];
                var body = Part(fish[i],"Body",RuntimeSpriteFactory.CircleSprite,color,-24);
                body.transform.localScale = new Vector3(length*0.72f,length*0.27f,1);
                body.transform.localPosition = new Vector3(length*0.06f,0,0);
                tails[i] = Tail(fish[i],length,color);
                var back = Part(fish[i],"Back",RuntimeSpriteFactory.CircleSprite,new Color(1f,0.92f,0.7f,0.35f),-23);
                back.transform.localScale = new Vector3(length*0.46f,length*0.055f,1);
                var eye = Part(fish[i],"Eye",RuntimeSpriteFactory.CircleSprite,new Color(0.05f,0.11f,0.14f,0.8f),-22);
                eye.transform.localPosition = new Vector3(length*0.27f,length*0.055f,0);
                eye.transform.localScale = Vector3.one*length*0.035f;
                var fin = Part(fish[i],"Fin",RuntimeSpriteFactory.CircleSprite,new Color(color.r,color.g,color.b,0.3f),-25);
                fin.transform.localScale = new Vector3(length*0.22f,length*0.41f,1);
                FishDetailArt.Add(fish[i],length,color,-23,i);
                wakes[i] = Part(visualRoot.transform,"FishWake_"+i,RuntimeSpriteFactory.RingSprite,Color.clear,-21);
            }
            Animate();
        }

        private static SpriteRenderer Part(Transform parent,string name,Sprite sprite,Color color,int order)
        {
            var obj = new GameObject(name,typeof(SpriteRenderer));
            obj.transform.SetParent(parent,false);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite; renderer.color=color; renderer.sortingOrder=order;
            return renderer;
        }

        private Transform Tail(Transform parent,float length,Color color)
        {
            var obj = new GameObject("Tail",typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition = new Vector3(-length*0.21f,0,0);
            var mesh = new Mesh {name="ForestFishTail_Runtime"};
            mesh.vertices = new[] {Vector3.zero,new Vector3(-length*0.26f,length*0.14f,0),
                new Vector3(-length*0.2f,0,0),new Vector3(-length*0.26f,-length*0.14f,0)};
            mesh.triangles = new[] {0,1,2,0,2,3};
            mesh.colors = new[] {color,color,color,color};
            mesh.uv = new[] {Vector2.one*0.5f,Vector2.one*0.5f,Vector2.one*0.5f,Vector2.one*0.5f};
            mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=material; renderer.sortingOrder=-24;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return obj.transform;
        }

        private void Update() { if(fish==null || habitats==null || habitatRadii==null || habitatCenters==null)return; elapsed+=Time.deltaTime; Animate(); }
        private void Animate()
        {
            for(int i=0;i<fish.Length;i++)
            {
                if(fish[i]==null || i>=habitats.Length) continue;
                int h=habitats[i];
                if(habitatRadii==null || h>=habitatRadii.Length || habitatCenters==null || h>=habitatCenters.Length) continue;
                float phase=elapsed*(0.32f+(i%4)*0.035f)+i*2.39996f;
                float len = lengths!=null && i<lengths.Length ? lengths[i] : 0.3f;
                Vector2 safe=habitatRadii[h]-Vector2.one*(len*0.5f);
                float scale=0.55f+(i%3)*0.11f;
                Vector2 offset=new Vector2(Mathf.Cos(phase)*safe.x,Mathf.Sin(phase)*safe.y)*scale;
                fish[i].localPosition=habitatCenters[h]+offset;
                Vector2 tangent=new Vector2(-Mathf.Sin(phase)*safe.x,Mathf.Cos(phase)*safe.y);
                fish[i].localRotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg);
                if(tails!=null && i<tails.Length && tails[i]!=null)
                {
                    tails[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(elapsed*8+i)*24);
                }
                if(wakes!=null && i<wakes.Length && wakes[i]!=null)
                {
                    float wake=Mathf.Repeat(elapsed*0.45f+i*0.317f,1);
                    wakes[i].transform.localPosition=fish[i].localPosition;
                    wakes[i].transform.localScale=new Vector3(0.10f+wake*0.20f,0.045f+wake*0.075f,1);
                    wakes[i].color=new Color(0.75f,0.96f,1f,Mathf.Sin(wake*Mathf.PI)*0.10f);
                }
            }
        }
        private void OnEnable() { if(visualRoot!=null)visualRoot.SetActive(true); }
        private void OnDisable() { if(visualRoot!=null)visualRoot.SetActive(false); }
        private void OnDestroy()
        {
            if(visualRoot!=null)Destroy(visualRoot);
            foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);
            if(material!=null)Destroy(material);
        }
    }
}
