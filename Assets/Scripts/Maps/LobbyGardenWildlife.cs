using System.Collections.Generic;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Decorative lower-garden wildlife stays in authored flower beds and never touches gameplay or saved progress.</summary>
    public sealed class LobbyGardenWildlife : MonoBehaviour
    {
        [Header("Authored flower beds and perches")]
        [SerializeField] private Vector2[] flowerBeds;
        [SerializeField] private Vector2[] birdPerches;
        private Transform visualRoot;
        private Transform[] butterflies, leftWings, rightWings, birds, birdLeftWings, birdRightWings;
        private SpriteRenderer[] birdShadows;
        private Transform[] birdFeet;
        private Transform[] foldedWings;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Material sharedMaterial;
        private float elapsed;
        public float AnimationTime => elapsed;
        public int ButterflyCount => butterflies == null ? 0 : butterflies.Length;
        public int BirdCount => birds == null ? 0 : birds.Length;
        public bool IsFlying(int index) => birds != null && index >= 0 && index < birds.Length && Mathf.Repeat(elapsed + index * 3.4f, 15f) >= 10f;

        private void Start()
        {
            visualRoot = Child(transform, "LowerGardenWildlife");
            int count = flowerBeds == null ? 0 : flowerBeds.Length * 2;
            butterflies = new Transform[count]; leftWings = new Transform[count]; rightWings = new Transform[count];
            Color[] palette = { new Color(0.27f,0.64f,0.98f), new Color(0.69f,0.39f,0.92f), new Color(0.96f,0.40f,0.64f) };
            for (int i = 0; i < count; i++)
            {
                butterflies[i] = Child(visualRoot, "LowerButterfly_" + i);
                Color color = palette[i % palette.Length];
                leftWings[i] = ButterflyWing(butterflies[i], color, "LeftWing");
                rightWings[i] = ButterflyWing(butterflies[i], color, "RightWing");
                Disc(butterflies[i], "Body", new Vector2(0,0), new Vector2(0.028f,0.17f), new Color(0.12f,0.10f,0.15f), 18);
                Disc(butterflies[i], "Head", new Vector2(0,0.085f), new Vector2(0.034f,0.034f), new Color(0.16f,0.12f,0.18f), 18);
                butterflies[i].localScale = Vector3.one * (0.83f + i % 2 * 0.12f);
            }
            int birdsCount = birdPerches == null ? 0 : birdPerches.Length;
            birds = new Transform[birdsCount]; birdLeftWings = new Transform[birdsCount]; birdRightWings = new Transform[birdsCount];
            birdShadows = new SpriteRenderer[birdsCount];
            birdFeet = new Transform[birdsCount];
            foldedWings = new Transform[birdsCount];
            for (int i = 0; i < birdsCount; i++)
            {
                birds[i] = Child(visualRoot, "GardenBird_" + i);
                Color back = i % 2 == 0 ? new Color(0.20f,0.40f,0.49f) : new Color(0.47f,0.35f,0.23f);
                Color breast = i % 2 == 0 ? new Color(0.86f,0.67f,0.38f) : new Color(0.84f,0.47f,0.30f);
                Disc(birds[i], "BodyOutline", Vector2.zero, new Vector2(0.29f,0.20f), new Color(0.12f,0.18f,0.17f), 16);
                Disc(birds[i], "Body", new Vector2(0,0.008f), new Vector2(0.265f,0.18f), back, 17);
                Disc(birds[i], "Breast", new Vector2(0.065f,-0.025f), new Vector2(0.13f,0.105f), breast, 18);
                Disc(birds[i], "Head", new Vector2(0.105f,0.095f), new Vector2(0.12f,0.115f), back, 19);
                Disc(birds[i], "Cheek", new Vector2(0.128f,0.068f), new Vector2(0.055f,0.045f), new Color(0.95f,0.88f,0.68f), 20);
                Disc(birds[i], "Eye", new Vector2(0.139f,0.108f), new Vector2(0.019f,0.019f), new Color(0.07f,0.09f,0.08f), 21);
                Triangle(birds[i], "Beak", new[] {new Vector2(0.153f,0.093f),new Vector2(0.217f,0.078f),new Vector2(0.157f,0.066f)}, new Color(0.86f,0.65f,0.28f), 18);
                Triangle(birds[i], "Tail", new[] {new Vector2(-0.10f,0.015f),new Vector2(-0.28f,0.08f),new Vector2(-0.24f,-0.065f)}, back * 0.8f, 15);
                Disc(birds[i],"Brow",new Vector2(0.124f,0.125f),new Vector2(0.072f,0.016f),new Color(0.88f,0.84f,0.67f),20);
                Disc(birds[i],"EyeGlint",new Vector2(0.142f,0.112f),Vector2.one*0.007f,Color.white,22);
                for(int feather=0;feather<5;feather++)
                {
                    var plume=Disc(birds[i],"BackFeather",new Vector2(-0.095f+feather*0.032f,0.017f),new Vector2(0.066f,0.032f),Color.Lerp(back,breast,0.13f),18);
                    plume.transform.localEulerAngles=new Vector3(0,0,-18);
                }
                birdFeet[i]=Child(birds[i],"Feet");
                for(int leg=0;leg<2;leg++)
                {
                    float x=-0.04f+leg*0.09f;Color legColor=new Color(0.34f,0.28f,0.20f);
                    var shin=Disc(birdFeet[i],"Leg",new Vector2(x,-0.105f),new Vector2(0.010f,0.064f),legColor,15);
                    shin.transform.localEulerAngles=new Vector3(0,0,15);
                    for(int toe=0;toe<3;toe++)
                    {
                        var foot=Disc(birdFeet[i],"Toe",new Vector2(x+0.012f,-0.133f+(toe-1)*0.008f),new Vector2(0.045f,0.009f),legColor,15);
                        foot.transform.localEulerAngles=new Vector3(0,0,(toe-1)*18);
                    }
                }
                birdLeftWings[i] = BirdWing(birds[i], back, "LeftWing");
                birdRightWings[i] = BirdWing(birds[i], back, "RightWing");
                foldedWings[i]=Child(birds[i],"FoldedWing");
                var cover=Disc(foldedWings[i],"Cover",new Vector2(-0.04f,0.025f),new Vector2(0.20f,0.095f),Color.Lerp(back,Color.black,0.22f),19);
                cover.transform.localEulerAngles=new Vector3(0,0,-18);
                for(int stripe=0;stripe<4;stripe++)
                {
                    var feather=Disc(foldedWings[i],"FeatherLine",new Vector2(-0.10f+stripe*0.028f,0.011f),new Vector2(0.085f,0.012f),Color.Lerp(back,new Color(0.83f,0.77f,0.57f),0.28f),20);
                    feather.transform.localEulerAngles=new Vector3(0,0,-24);
                }
                birdShadows[i] = Disc(visualRoot,"BirdShadow_"+i,Vector2.zero,new Vector2(0.34f,0.10f),new Color(0,0,0,0.18f),-16);
                birdShadows[i].sprite = RuntimeSpriteFactory.SoftGlowSprite;
            }
            Animate();
        }

        private static Transform Child(Transform parent, string name)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform;
        }

        private SpriteRenderer Disc(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var target = Child(parent,name); target.localPosition = position; target.localScale = new Vector3(size.x,size.y,1);
            var renderer = target.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = RuntimeSpriteFactory.CircleSprite;
            renderer.color = color; renderer.sortingOrder = order;
            if (sharedMaterial == null) sharedMaterial = renderer.sharedMaterial;
            return renderer;
        }

        private Transform ButterflyWing(Transform parent, Color color, string name)
        {
            var wing = Child(parent,name);
            var dark = new Color(0.12f,0.13f,0.23f);
            Disc(wing,"UpperOutline",new Vector2(-0.095f,0.063f),new Vector2(0.19f,0.18f),dark,16);
            Disc(wing,"LowerOutline",new Vector2(-0.077f,-0.055f),new Vector2(0.145f,0.14f),dark,16);
            Disc(wing,"UpperColor",new Vector2(-0.093f,0.066f),new Vector2(0.155f,0.147f),color,17);
            Disc(wing,"LowerColor",new Vector2(-0.077f,-0.052f),new Vector2(0.115f,0.107f),Color.Lerp(color,Color.black,0.15f),17);
            Disc(wing,"EyeSpot",new Vector2(-0.121f,0.08f),new Vector2(0.05f,0.045f),dark,18);
            Disc(wing,"SpotLight",new Vector2(-0.125f,0.088f),new Vector2(0.022f,0.020f),new Color(0.92f,0.95f,1),19);
            for (int j=0;j<3;j++) Disc(wing,"EdgeDot_"+j,new Vector2(-0.145f+j*0.025f,-0.057f-j*0.018f),new Vector2(0.014f,0.014f),new Color(0.96f,0.9f,0.79f),18);
            return wing;
        }

        private Transform BirdWing(Transform parent, Color color, string name)
        {
            var wing=Child(parent,name);
            for(int i=0;i<6;i++)
            {
                var feather=Disc(wing,"Feather_"+i,new Vector2(-0.025f-i*0.024f,0.035f+i*0.019f),new Vector2(0.052f,0.29f-i*0.022f),Color.Lerp(color,new Color(0.10f,0.20f,0.24f),i*0.10f),18);
                feather.transform.localEulerAngles=new Vector3(0,0,-25-i*8);
            }
            return wing;
        }

        private void Triangle(Transform parent,string name,Vector2[] points,Color color,int order)
        {
            var mesh=new Mesh {name=name+"_GardenRuntime"}; meshes.Add(mesh);
            mesh.vertices=new[]{(Vector3)points[0],(Vector3)points[1],(Vector3)points[2]};
            mesh.colors=new[]{color,color,color};mesh.uv=new[]{Vector2.one*0.5f,Vector2.one*0.5f,Vector2.one*0.5f};mesh.triangles=new[]{0,1,2};mesh.RecalculateBounds();
            var part=Child(parent,name);part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=sharedMaterial;renderer.sortingOrder=order;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        private void Update() {elapsed+=Time.deltaTime;Animate();}
        private void Animate()
        {
            if (butterflies != null && leftWings != null && rightWings != null && flowerBeds != null && flowerBeds.Length > 0)
            {
                for(int i=0;i<butterflies.Length;i++)
                {
                    if (butterflies[i] == null) continue;
                    int bedIdx = (i / 2) % flowerBeds.Length;
                    float phase=elapsed*(0.65f+i*0.035f)+i*2.1f;
                    butterflies[i].localPosition=flowerBeds[bedIdx]+new Vector2(Mathf.Sin(phase)*0.48f,0.16f+Mathf.Sin(phase*1.73f)*0.23f);
                    butterflies[i].localEulerAngles=new Vector3(0,0,Mathf.Sin(phase)*28);
                    float fold=0.18f+Mathf.Abs(Mathf.Sin(elapsed*12+i))*0.82f;
                    if (leftWings[i] != null) leftWings[i].localScale=new Vector3(fold,1,1);
                    if (rightWings[i] != null) rightWings[i].localScale=new Vector3(-fold,1,1);
                }
            }
            if (birds != null && birdPerches != null && birdPerches.Length > 0 &&
                birdFeet != null && foldedWings != null && birdLeftWings != null && birdRightWings != null && birdShadows != null)
            {
                for(int i=0;i<birds.Length;i++)
                {
                    if (birds[i] == null) continue;
                    float clock=elapsed+i*3.4f,cycle=Mathf.Floor(clock/15f),t=Mathf.Repeat(clock,15f);
                    int from=(i+(int)cycle)%birdPerches.Length,to=(from+1)%birdPerches.Length;
                    bool flying=t>=10f;float progress=flying?Mathf.SmoothStep(0,1,(t-10)/5f):0;
                    Vector2 ground=Vector2.Lerp(birdPerches[from],birdPerches[to],progress);
                    float height=flying?Mathf.Sin(progress*Mathf.PI)*0.68f:Mathf.Max(0,Mathf.Sin(t*3f))*0.025f;
                    birds[i].localPosition=ground+Vector2.up*height;
                    float facing=birdPerches[to].x>=birdPerches[from].x?1:-1;
                    birds[i].localScale=new Vector3(facing,1,1);
                    float flap=flying?0.4f+0.6f*Mathf.Abs(Mathf.Sin(elapsed*17+i)):0.26f;
                    if (birdFeet[i] != null) birdFeet[i].gameObject.SetActive(!flying);
                    if (foldedWings[i] != null) foldedWings[i].gameObject.SetActive(!flying);
                    if (birdLeftWings[i] != null)
                    {
                        birdLeftWings[i].gameObject.SetActive(flying);
                        birdLeftWings[i].localScale=flying?new Vector3(1,flap,1):new Vector3(0.85f,0.25f,1);
                        birdLeftWings[i].localEulerAngles=new Vector3(0,0,flying?-16:-78);
                    }
                    if (birdRightWings[i] != null)
                    {
                        birdRightWings[i].gameObject.SetActive(flying);
                        birdRightWings[i].localScale=flying?new Vector3(1,-flap,1):new Vector3(0.85f,0.22f,1);
                        birdRightWings[i].localEulerAngles=new Vector3(0,0,flying?16:-78);
                    }
                    if (birdShadows[i] != null)
                    {
                        birdShadows[i].transform.localPosition=ground+new Vector2(0.05f,-0.10f);
                        birdShadows[i].color=new Color(0,0,0,0.18f-height*0.12f);
                    }
                }
            }
        }
        private void OnEnable() {if(visualRoot!=null)visualRoot.gameObject.SetActive(true);}
        private void OnDisable() {if(visualRoot!=null)visualRoot.gameObject.SetActive(false);}
        private void OnDestroy()
        {
            if(visualRoot!=null)Destroy(visualRoot.gameObject);
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        }
    }
}
