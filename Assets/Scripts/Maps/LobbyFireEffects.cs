using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Bounded fire layers, rising embers and camp smoke animate existing painted fires without spawning objects every frame.</summary>
    public sealed class LobbyFireEffects : MonoBehaviour
    {
        [Header("Builder Anchors")]
        [SerializeField] private Vector2[] firePositions;
        [Header("Intensity")]
        [SerializeField, Range(0.2f,1f)] private float intensity = 0.75f;
        private GameObject visualRoot;
        private SpriteRenderer[] flames, cores, embers, glows, smoke;
        private Sprite flameSprite;
        private Texture2D flameTexture;
        private float elapsed;
        public float AnimationTime => elapsed;
        public int VisualCount => visualRoot != null ? visualRoot.transform.childCount : 0;

        private void Start()
        {
            if (firePositions == null || firePositions.Length == 0) { enabled=false; return; }
            visualRoot=new GameObject("LivingFires");
            visualRoot.transform.SetParent(transform,false);
            CreateFlameSprite();
            int count=firePositions.Length;
            flames=new SpriteRenderer[count*3];
            cores=new SpriteRenderer[count*3];
            embers=new SpriteRenderer[count*14];
            glows=new SpriteRenderer[count];
            smoke=new SpriteRenderer[5];
            for(int i=0;i<flames.Length;i++)
            {
                flames[i]=Create("Flame_" + i,flameSprite,-16);
                cores[i]=Create("FlameCore_" + i,flameSprite,-15);
            }
            for(int i=0;i<embers.Length;i++) embers[i]=Create("Ember_" + i,RuntimeSpriteFactory.CircleSprite,14);
            for(int i=0;i<count;i++) glows[i]=Create("FireGroundGlow_" + i,RuntimeSpriteFactory.SoftGlowSprite,-19);
            for(int i=0;i<smoke.Length;i++) smoke[i]=Create("CampSmoke_" + i,RuntimeSpriteFactory.SoftGlowSprite,-13);
        }

        private SpriteRenderer Create(string name,Sprite sprite,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));
            obj.transform.SetParent(visualRoot.transform,false);
            var renderer=obj.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.sortingOrder=order;
            return renderer;
        }

        private void CreateFlameSprite()
        {
            // A simple antialiased teardrop, not a replacement image asset.
            const int Size=64;
            flameTexture=new Texture2D(Size,Size,TextureFormat.RGBA32,false);
            flameTexture.name="LobbyFlame_Runtime";
            flameTexture.wrapMode=TextureWrapMode.Clamp;
            flameTexture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++)
            {
                float height=y/(float)(Size-1);
                float width=Mathf.Pow(1-height,0.7f)*0.42f*Mathf.Sin(Mathf.Min(1,height*5+0.15f)*Mathf.PI*0.5f);
                float lean=Mathf.Sin(height*3.5f)*height*0.15f;
                for(int x=0;x<Size;x++)
                {
                    float distance=Mathf.Abs(x/(float)(Size-1)-0.5f-lean);
                    float alpha=Mathf.Clamp01((width-distance)*Size*0.14f)*Mathf.SmoothStep(0,1,height*12);
                    alpha*=Mathf.Lerp(1f,0.4f,height);
                    pixels[y*Size+x]=new Color(1,1,1,alpha);
                }
            }
            flameTexture.SetPixels(pixels);
            flameTexture.Apply(false,true);
            flameSprite=Sprite.Create(flameTexture,new Rect(0,0,Size,Size),new Vector2(0.5f,0),Size);
            flameSprite.name="LobbyFlameSprite_Runtime";
        }

        private void Update()
        {
            if (flames == null || cores == null || firePositions == null || firePositions.Length == 0) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null || cores[i] == null) continue;
                int station = Mathf.Min(i / 3, firePositions.Length - 1);
                int tongue = i % 3;
                float flicker = 0.8f + Mathf.Sin(elapsed * 8.5f + i * 2.5f) * 0.13f + Mathf.Sin(elapsed * 14 + i) * 0.07f;
                float height = (station == 0 ? 0.36f : 0.42f) * flicker * (tongue == 1 ? 1 : 0.62f);
                Vector2 location = firePositions[station] + new Vector2((tongue - 1) * 0.095f, -0.07f);
                flames[i].transform.localPosition = location;
                flames[i].transform.localScale = new Vector3(0.18f, height, 1);
                flames[i].transform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(elapsed * 5 + i) * 9);
                flames[i].color = new Color(1, 0.41f, 0.08f, intensity * 0.5f);
                cores[i].transform.localPosition = location;
                cores[i].transform.localScale = new Vector3(0.095f, height * 0.65f, 1);
                cores[i].transform.localEulerAngles = flames[i].transform.localEulerAngles;
                cores[i].color = new Color(1, 0.78f, 0.29f, intensity * 0.48f);
            }
            if (embers != null)
            {
                for (int i = 0; i < embers.Length; i++)
                {
                    if (embers[i] == null) continue;
                    int station = Mathf.Min(i / 14, firePositions.Length - 1);
                    float t = Mathf.Repeat(elapsed * (0.35f + (i % 4) * 0.06f) + i * 0.618034f, 1);
                    float side = Mathf.Sin(i * 5.4f) * 0.23f;
                    Vector2 position = firePositions[station] + new Vector2(side + Mathf.Sin(t * 4 + i) * t * 0.16f, t * 1.25f);
                    embers[i].transform.localPosition = position;
                    embers[i].transform.localScale = new Vector3(0.017f + (i % 3) * 0.006f, 0.03f + (i % 3) * 0.008f, 1);
                    embers[i].color = Color.Lerp(new Color(1, 0.83f, 0.3f), new Color(1, 0.24f, 0.03f), t);
                    Color color = embers[i].color;
                    color.a = Mathf.Sin(t * Mathf.PI) * intensity;
                    embers[i].color = color;
                }
            }
            if (glows != null)
            {
                for (int i = 0; i < glows.Length; i++)
                {
                    if (glows[i] == null || i >= firePositions.Length) continue;
                    glows[i].transform.localPosition = firePositions[i];
                    glows[i].transform.localScale = new Vector3(1.45f, 0.9f, 1);
                    glows[i].color = new Color(1, 0.46f, 0.1f, (0.13f + Mathf.Sin(elapsed * 5 + i) * 0.025f) * intensity);
                }
            }
            if (smoke != null)
            {
                Vector2 camp = firePositions[firePositions.Length - 1];
                for (int i = 0; i < smoke.Length; i++)
                {
                    if (smoke[i] == null) continue;
                    float t = Mathf.Repeat(elapsed * 0.2f + i / (float)smoke.Length, 1);
                    smoke[i].transform.localPosition = camp + new Vector2(t * 0.35f, 0.25f + t * 1.5f);
                    smoke[i].transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.65f, t);
                    Color color = smoke[i].color;
                    color.a = Mathf.Sin(t * Mathf.PI) * 0.25f * intensity;
                    smoke[i].color = color;
                }
            }
        }

        private void OnEnable() { if(visualRoot!=null) visualRoot.SetActive(true); }
        private void OnDisable() { if(visualRoot!=null) visualRoot.SetActive(false); }
        private void OnDestroy()
        {
            if(visualRoot!=null) Destroy(visualRoot);
            if(flameSprite!=null) Destroy(flameSprite);
            if(flameTexture!=null) Destroy(flameTexture);
        }
    }
}
