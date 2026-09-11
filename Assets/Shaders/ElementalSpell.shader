Shader "TopDownGame/ElementalSpell"
{
 Properties {
  _MainTex("Sprite",2D)="white"{} _Tint("Element",Color)=(1,.3,0,1) _Core("Core",Color)=(1,1,1,1)
  _Element("Element type",Float)=0 _Stage("Phase",Float)=0 _Progress("Progress",Float)=0 _Clock("Time",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Tint,_Core; float _Element,_Stage,_Progress,_Clock;
   struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
   float stroke(float distance,float width){return 1-smoothstep(width,width+.013,abs(distance));}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   float hex(float2 p){p=abs(p);return max(p.x*.8660254+p.y*.5,p.y);}
   half4 frag(v i):SV_Target {
    float2 p=(i.uv-.5)*2; float r=length(p), angle=atan2(p.y,p.x),t=_Progress,c=_Clock;
    float edge=1-smoothstep(.87,1,r);float mask=0,hot=0;
    float fade=saturate(t*18)*saturate((1-t)*7);
    if(_Stage<.5 || (_Stage>3.5 && _Stage<4.5)) {
     float spin=angle+c*1.4;
     mask=stroke(r-.77,.009)+stroke(r-.64,.007)*.6;
     float ticks=pow(saturate(cos(spin*12)),24);
     mask+=ticks*step(.65,r)*step(r,.76);
     mask+=stroke(hex(float2(cos(c)*p.x-sin(c)*p.y,sin(c)*p.x+cos(c)*p.y))-.45,.012)*.65;
     mask*=.75;hot=pow(saturate(1-r*3),3)*t;
     fade=_Stage<.5 ? saturate(t*6) : .65;
    } else if(_Element<.5) {
     // Fire: curled tongues and a hot core, not a soft circular icon.
     float2 q=p;
     if(_Stage<1.5){q.x+=.18; q.y*=1.5;}
     float n=noise(q*6+float2(-c*8,c*2))*.6+noise(q*13-c*3)*.25;
     float flame=length(q)+.16*sin(angle*5-r*11+c*9)+n*.22;
     float reach=_Stage>1.5 && _Stage<2.5 ? .25+sqrt(t)*.55 : .62;
     mask=(1-smoothstep(reach-.18,reach,flame))*edge;
     hot=pow(saturate(1-flame/max(.15,reach-.13)),.6);
     mask+=stroke(r-(.15+t*.7),.018)*(1-t)*step(1.5,_Stage)*step(_Stage,2.5);
    } else if(_Element<1.5) {
     // Six long ice blades with crisp facets and smaller orbiting splinters.
     float a=abs(frac((angle+c*.1)/6.2831853*6+.5)-.5)*1.0472;
     float shard=1-smoothstep(.028,.065,abs(sin(a)*r));
     mask=shard*smoothstep(.08,.2,r)*(1-smoothstep(.62,.82,r));
     mask+=stroke(r-.31,.008)*.6;hot=shard*.65;
    } else if(_Element<2.5) {
     // Three tapered blade crescents rotate, leaving the center readable.
     float spin=angle+c*10;
     float taper=frac(spin/6.2831853*3);
     float taperShape=pow(saturate(sin(taper*3.14159265)),.8);
     float ring=.56+.18*taper+.015*sin(angle*17-c*9);
     float grain=.8+.2*noise(p*25+c*3);
     mask=stroke(r-ring,.045*taperShape)*taperShape*grain;
     hot=stroke(r-ring+.012,.009)*taperShape;
     mask+=stroke(r-ring-.09,.012)*pow(taperShape,3)*.32;
     mask+=stroke(r-.82,.003)*pow(saturate(sin(angle*19+c*4)),12)*.3;
    } else if(_Element<3.5) {
     // Forked electric paths with narrow white cores.
     float bolt=p.x+.12*sin(p.y*19+c*35)+.055*sin(p.y*43-c*21);
     mask=stroke(bolt,.026)*(1-smoothstep(.65,.95,abs(p.y)));
     hot=stroke(bolt,.007);
     mask+=stroke(p.x-abs(p.y)*.65+.05*sin(p.y*39+c*28),.012)*step(0,p.y)*.6;
     mask+=stroke(r-(.25+t*.45),.008)*.4;
    } else if(_Element<4.5) {
     float a=angle+c*1.2;
     float petals=.5+.5*cos(a*5);
     mask=stroke(r-(.3+.2*petals),.018)+stroke(r-.75,.009)*.6;
     float cross=min(max(abs(p.x)-.055,abs(p.y)-.21),max(abs(p.x)-.21,abs(p.y)-.055));
     hot=1-smoothstep(0,.016,cross);mask+=hot*.8;
    } else if(_Element<5.5) {
     float h=hex(p);
     mask=stroke(h-.71,.018)+stroke(h-.64,.006)*.5;
     float grid=abs(frac(p.x*6+p.y*3+c*.12)-.5);
     mask+=stroke(grid-.25,.008)*step(h,.67)*.28;
     mask+=stroke(h-.58,.006)*pow(saturate(cos(angle*6)),10);
     hot=stroke(h-.71,.008);mask+=pow(saturate(1-r),3)*.22;
    } else {
     float spiral=sin(angle*3+r*16+c*9);
     float rim=.22+.5*(1-t);
     mask=stroke(r-rim,.04+.03*spiral)+stroke(r-.78,.008)*.4;
     mask+=pow(saturate(spiral),12)*smoothstep(.18,.4,r)*(1-smoothstep(.7,.9,r))*.55;
     hot=stroke(r-rim,.008); 
    }
    if(_Stage>2.5 && _Stage<3.5) fade=saturate(t*12)*saturate((1-t)*12);
    if(_Stage>4.5) fade=saturate(t*8);
    float alpha=saturate(mask+hot*.4)*fade*edge;
    half3 col=lerp(_Tint.rgb,_Core.rgb,saturate(hot));
    return half4(col,alpha);
   }
   ENDHLSL
  }
 }
}
