using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Small fish, butterflies and falling leaves use bounded authored habitats and reusable geometry; never affect navigation or saves.</summary>
    public sealed class LobbyWildlife : MonoBehaviour
    {
        [Header("Builder Habitats")]
        [SerializeField] private Vector2 pondCenter;
        [SerializeField] private Vector2 pondRadii = new Vector2(0.52f,0.58f);
        [SerializeField] private Vector2[] flowerPatches;
        [SerializeField, Range(4, 16)] private int fishCount = 10;
        private Vector2[] swimPositions;
        private SpriteRenderer[] ripples;
        private Renderer[][] fishRenderers;
        private UnityEngine.Rendering.SortingGroup[] fishGroups;
        private SpriteRenderer[][] detailRenderers;
        private Color[][] detailColors;
        public int JumpCount { get; private set; }
        private int[] jumpCycles;
        private GameObject visualRoot;
        private Transform[] fish, tails, butterflies, leftWings, rightWings, leaves;
        private SpriteRenderer[] fishBodies, leafGlows;
        private Mesh tailMesh, wingMesh, leafMesh;
        private Material sharedSpriteMaterial;
        private float elapsed;
        public float AnimationTime => elapsed;
        public int FishCount => fish != null ? fish.Length : 0;
        public int ButterflyCount => butterflies != null ? butterflies.Length : 0;
        public int VisualCount => visualRoot != null ? visualRoot.GetComponentsInChildren<Renderer>(true).Length : 0;
        public bool FishStayInPond
        {
            get
            {
                if(fish==null) return false;
                foreach(Vector2 swimmer in swimPositions)
                {
                    Vector2 offset=swimmer-pondCenter;
                    float x=offset.x/Mathf.Max(0.01f,pondRadii.x);
                    float y=offset.y/Mathf.Max(0.01f,pondRadii.y);
                    if(x*x+y*y>1.01f) return false;
                }
                return true;
            }
        }

        private void Start()
        {
            visualRoot=new GameObject("LivingWildlife");
            visualRoot.transform.SetParent(transform,false);
            tailMesh=Shape("FishTail",new[]{new Vector2(0,0),new Vector2(-0.1f,0.065f),new Vector2(-0.075f,0),new Vector2(-0.1f,-0.065f)},
                new Color(0.88f,0.53f,0.25f,0.55f));
            wingMesh=Shape("ButterflyWing",new[]{Vector2.zero,new Vector2(-0.11f,0.12f),new Vector2(-0.17f,0.075f),
                new Vector2(-0.14f,-0.01f),new Vector2(-0.10f,-0.09f),new Vector2(-0.04f,-0.08f)},
                new Color(0.98f,0.74f,0.34f,0.94f));
            leafMesh=Shape("DriftingLeaf",new[]{new Vector2(0,0.09f),new Vector2(-0.035f,0.02f),new Vector2(-0.025f,-0.045f),
                new Vector2(0,-0.085f),new Vector2(0.03f,-0.015f),new Vector2(0.035f,0.04f)},
                new Color(0.69f,0.73f,0.3f,0.7f));
            fish=new Transform[fishCount]; tails=new Transform[fishCount]; fishBodies=new SpriteRenderer[fishCount];
            fishGroups=new UnityEngine.Rendering.SortingGroup[fishCount];
            detailRenderers=new SpriteRenderer[fishCount][];detailColors=new Color[fishCount][];
            swimPositions=new Vector2[fishCount]; ripples=new SpriteRenderer[fishCount];
            fishRenderers=new Renderer[fishCount][]; jumpCycles=new int[fishCount];
            for(int i=0;i<fish.Length;i++)
            {
                fish[i]=Root("PondFish_"+i);
                Color[] palette={new Color(1f,0.38f,0.13f),new Color(1f,0.84f,0.22f),
                    new Color(0.91f,0.94f,1f),new Color(0.35f,0.65f,0.95f),new Color(0.95f,0.48f,0.66f)};
                SpriteRenderer body=Sprite(fish[i],"Body",RuntimeSpriteFactory.CircleSprite,palette[i%palette.Length],-18);
                body.transform.localScale=new Vector3(0.23f,0.085f,1);
                fishBodies[i]=body;
                if(sharedSpriteMaterial==null) sharedSpriteMaterial=body.sharedMaterial;
                tails[i]=Geometry(fish[i],"Tail",tailMesh,-18);
                tails[i].localPosition=new Vector3(-0.09f,0,0);
                SpriteRenderer highlight=Sprite(fish[i],"Back",RuntimeSpriteFactory.SoftGlowSprite,new Color(1,0.86f,0.55f,0.36f),-18);
                highlight.transform.localScale=new Vector3(0.16f,0.025f,1);
                fish[i].localScale=Vector3.one*(1.2f+(i%3)*0.16f);
                SpriteRenderer eye=Sprite(fish[i],"Eye",RuntimeSpriteFactory.CircleSprite,new Color(0.07f,0.09f,0.12f),-18);
                eye.transform.localScale=new Vector3(0.017f,0.017f,1);
                eye.transform.localPosition=new Vector3(0.066f,0.017f,0);
                // Detailed markings are part of the swimming rig, not stationary decals.
                FishDetailArt.Add(fish[i],0.34f,palette[i%palette.Length],-18,i);
                var fishGroup=fish[i].gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                fishGroup.sortingOrder=-18;fishGroups[i]=fishGroup;
                detailRenderers[i]=fish[i].Find("FishDetails").GetComponentsInChildren<SpriteRenderer>();
                detailColors[i]=new Color[detailRenderers[i].Length];
                for(int k=0;k<detailColors[i].Length;k++)detailColors[i][k]=detailRenderers[i][k].color;
                fishRenderers[i]=fish[i].GetComponentsInChildren<Renderer>();
                ripples[i]=Sprite(visualRoot.transform,"LandingRipple_"+i,
                    RuntimeSpriteFactory.RingSprite,new Color(0.7f,0.95f,1f,0),-16);
                swimPositions[i]=pondCenter;
                jumpCycles[i]=-1;
            }
            int butterflyCount=flowerPatches!=null ? flowerPatches.Length*2 : 0;
            butterflies=new Transform[butterflyCount]; leftWings=new Transform[butterflyCount]; rightWings=new Transform[butterflyCount];
            for(int i=0;i<butterflyCount;i++)
            {
                butterflies[i]=Root("GardenButterfly_"+i);
                leftWings[i]=Geometry(butterflies[i],"LeftWing",wingMesh,16);
                rightWings[i]=Geometry(butterflies[i],"RightWing",wingMesh,16);
                SpriteRenderer body=Sprite(butterflies[i],"Body",RuntimeSpriteFactory.CircleSprite,new Color(0.16f,0.12f,0.09f),17);
                body.transform.localScale=new Vector3(0.018f,0.15f,1);
                butterflies[i].localScale=Vector3.one*(0.65f+(i%2)*0.16f);
            }
            leaves=new Transform[7]; leafGlows=new SpriteRenderer[7];
            for(int i=0;i<leaves.Length;i++)
            {
                leaves[i]=Root("DriftingLeaf_"+i);
                Geometry(leaves[i],"Leaf",leafMesh,15);
                leafGlows[i]=Sprite(leaves[i],"Shadow",RuntimeSpriteFactory.SoftGlowSprite,new Color(0,0,0,0.1f),-16);
                leafGlows[i].transform.localPosition=new Vector3(0.1f,-0.3f,0);
                leafGlows[i].transform.localScale=new Vector3(0.1f,0.06f,1);
            }
        }

        private Transform Root(string name)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(visualRoot.transform,false);
            return obj.transform;
        }

        private static SpriteRenderer Sprite(Transform parent,string name,Sprite sprite,Color color,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));
            obj.transform.SetParent(parent,false);
            var renderer=obj.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite; renderer.color=color; renderer.sortingOrder=order;
            return renderer;
        }

        private Transform Geometry(Transform parent,string name,Mesh mesh,int order)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(parent,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=sharedSpriteMaterial;
            renderer.sortingOrder=order;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return obj.transform;
        }

        private static Mesh Shape(string name,Vector2[] outline,Color color)
        {
            var mesh=new Mesh {name=name+"_Runtime"};
            var vertices=new Vector3[outline.Length+1];
            var colors=new Color[vertices.Length];
            var uv=new Vector2[vertices.Length];
            var triangles=new int[outline.Length*3];
            for(int i=0;i<vertices.Length;i++) {colors[i]=color; uv[i]=new Vector2(0.5f,0.5f);}
            for(int i=0;i<outline.Length;i++)
            {
                vertices[i+1]=outline[i];
                triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=(i+1)%outline.Length+1;
            }
            mesh.vertices=vertices; mesh.colors=colors; mesh.uv=uv; mesh.triangles=triangles; mesh.RecalculateBounds();
            return mesh;
        }

        private void Update()
        {
            if(fish==null) return;
            elapsed+=Time.deltaTime;
            for(int i=0;i<fish.Length;i++)
            {
                float phase=elapsed*(0.33f+i*0.045f)+i*1.8f;
                float radius=0.3f+(i%5)*0.12f;
                swimPositions[i]=pondCenter+new Vector2(Mathf.Cos(phase)*pondRadii.x,Mathf.Sin(phase)*pondRadii.y)*radius;
                float clock=elapsed+i*1.91f;
                float jumpTime=Mathf.Repeat(clock,19f);
                bool jumping=jumpTime<0.85f;
                float height=jumping ? Mathf.Sin(jumpTime/0.85f*Mathf.PI)*0.42f : 0;
                int cycle=Mathf.FloorToInt(clock/19f);
                if(jumping && jumpCycles[i]!=cycle) {jumpCycles[i]=cycle; JumpCount++;}
                fish[i].localPosition=swimPositions[i]+Vector2.up*height;
                fishGroups[i].sortingOrder=jumping ? -16 : -18;
                float splash=Mathf.Clamp01((jumpTime-0.85f)/0.8f);
                bool splashVisible=jumpTime>=0.85f && jumpTime<1.65f;
                ripples[i].transform.localPosition=swimPositions[i];
                ripples[i].transform.localScale=new Vector3(0.12f+splash*0.45f,0.06f+splash*0.2f,1);
                ripples[i].color=new Color(0.72f,0.95f,1f,splashVisible ? (1-splash)*0.55f : 0);
                Vector2 tangent=new Vector2(-Mathf.Sin(phase)*pondRadii.x,Mathf.Cos(phase)*pondRadii.y);
                fish[i].localEulerAngles=new Vector3(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg);
                tails[i].localEulerAngles=new Vector3(0,0,Mathf.Sin(elapsed*10+i)*22);
                Color color=fishBodies[i].color;
                color.a=jumping ? 1f : 0.76f+Mathf.Sin(phase*0.65f)*0.08f;
                fishBodies[i].color=color;
                for(int k=0;k<detailRenderers[i].Length;k++)
                {
                    Color marking=detailColors[i][k];marking.a*=color.a;detailRenderers[i][k].color=marking;
                }
            }
            for(int i=0;i<butterflies.Length;i++)
            {
                float phase=elapsed*(0.55f+i*0.04f)+i*2.4f;
                Vector2 patch=flowerPatches[i/2];
                butterflies[i].localPosition=patch+new Vector2(Mathf.Sin(phase)*0.48f,
                    Mathf.Sin(phase*1.7f)*0.25f+0.15f);
                butterflies[i].localEulerAngles=new Vector3(0,0,-Mathf.Sin(phase)*35);
                float wing=0.14f+Mathf.Abs(Mathf.Sin(elapsed*17+i))*0.86f;
                leftWings[i].localScale=new Vector3(wing,1,1);
                rightWings[i].localScale=new Vector3(-wing,1,1);
            }
            for(int i=0;i<leaves.Length;i++)
            {
                float t=Mathf.Repeat(elapsed*0.055f+i*0.618034f,1);
                leaves[i].localPosition=new Vector3(-8.2f+(i*3.3f%16)+t*0.65f+Mathf.Sin(t*7+i)*0.24f,
                    -2.45f-t*1.3f);
                leaves[i].localEulerAngles=new Vector3(0,0,i*37+Mathf.Sin(t*8)*50);
                float size=Mathf.Sin(t*Mathf.PI)*0.8f;
                leaves[i].localScale=Vector3.one*size;
            }
        }

        private void OnEnable() {if(visualRoot!=null) visualRoot.SetActive(true);}
        private void OnDisable() {if(visualRoot!=null) visualRoot.SetActive(false);}
        private void OnDestroy()
        {
            if(visualRoot!=null) Destroy(visualRoot);
            if(tailMesh!=null) Destroy(tailMesh);
            if(wingMesh!=null) Destroy(wingMesh);
            if(leafMesh!=null) Destroy(leafMesh);
        }
    }
}
