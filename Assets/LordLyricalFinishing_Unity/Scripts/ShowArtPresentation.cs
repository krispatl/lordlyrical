using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LordLyrical
{
    // All motion is a function of absolute narrative time. No random state or simulation history.
    public sealed class ShowArtPresentation : MonoBehaviour
    {
        public GameObject vortexPrefab;
        public Material filamentMaterial;
        public int quality=1;
        public ConcertState Battle;
        Transform corridor;
        RoleVisual[] roles;
        OrcEncounterController orcs;
        LordLyricalController lord;
        CinematicParticleDirector cinema;
        DuelContactVFX duelContacts;
        readonly List<Material> owned=new();
        readonly LineRenderer[] lines=new LineRenderer[72];
        readonly Vector3[] points=new Vector3[64];
        readonly Vector3[] shardVertices=new Vector3[96*4];
        readonly Color[] shardColors=new Color[96*4];
        readonly TextMesh[] letters=new TextMesh[11];
        readonly Vector3[] letterHomes=new Vector3[11];
        TextMesh subtitle,credits,originalKey;
        Mesh shards;
        MeshRenderer shardRenderer;
        MaterialPropertyBlock block;
        float time;bool cinemaTitle;
        int cursor;
        public int ActiveRibbonCount {get;private set;}
        public bool TitleActive=>subtitle&&subtitle.gameObject.activeSelf;
        public void Initialize(RoleVisual[] roleActors,OrcEncounterController enemies,LordLyricalController conductor,int detail)
        {
            cinemaTitle=Resources.Load<Texture2D>("Cinema26/LordLyricalTitle");
            roles=roleActors;orcs=enemies;lord=conductor;quality=detail;block=new MaterialPropertyBlock();
            duelContacts=new GameObject("Duel contact particles").AddComponent<DuelContactVFX>();duelContacts.transform.SetParent(transform,false);duelContacts.Initialize();
            cinema=GetComponent<CinematicParticleDirector>();if(cinema)cinema.Initialize(roles,orcs,lord);
            if(vortexPrefab){corridor=Instantiate(vortexPrefab,transform).transform;corridor.name="Authored Frequency Vortex";}
            for(int i=0;i<lines.Length;i++){
                var go=new GameObject("Pooled resonance ribbon "+i);go.transform.SetParent(transform,false);
                var l=go.AddComponent<LineRenderer>();l.sharedMaterial=filamentMaterial;l.useWorldSpace=true;l.positionCount=64;l.numCornerVertices=0;l.numCapVertices=2;l.textureMode=LineTextureMode.Stretch;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.enabled=false;lines[i]=l;
            }
            shards=new Mesh{name="Seekable resonance fragments"};shards.MarkDynamic();int[] triangles=new int[96*12];Vector2[] uv=new Vector2[shardVertices.Length];
            int[] tetra={0,2,1,0,1,3,0,3,2,1,2,3};for(int i=0;i<96;i++){for(int j=0;j<12;j++)triangles[i*12+j]=i*4+tetra[j];for(int k=0;k<4;k++)uv[i*4+k]=new Vector2(k%2,.5f);}
            shards.vertices=shardVertices;shards.triangles=triangles;shards.uv=uv;shards.bounds=new Bounds(new Vector3(0,8,12),Vector3.one*80);
            var sg=new GameObject("Corruption peel and impact fragments");sg.transform.SetParent(transform,false);sg.AddComponent<MeshFilter>().sharedMesh=shards;shardRenderer=sg.AddComponent<MeshRenderer>();shardRenderer.sharedMaterial=filamentMaterial;shardRenderer.shadowCastingMode=ShadowCastingMode.Off;
            string word="LORDLYRICAL";for(int i=0;i<11;i++){letters[i]=Text("Assembling letter "+i,word[i].ToString(),.11f);letterHomes[i]=new Vector3((i-5f)*.67f+(i>3?.25f:0),5.25f,9);}
            subtitle=Text("Realm subtitle","THE RESONANT REALM",.055f);subtitle.transform.localPosition=new Vector3(0,4.38f,9);
            credits=Text("Show credits","A VR Experience\n\nCreated by Jeru The Damaja\n5th Yang LLC\nMaximum Warp Entertainment",.045f);credits.transform.localPosition=new Vector3(0,3.0f,9);
            originalKey=Text("Exposed original root E","E",.12f);Reset();
        }
        TextMesh Text(string name,string words,float size){var g=new GameObject(name);g.transform.SetParent(transform,false);var t=g.AddComponent<TextMesh>();t.text=words;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;t.fontSize=96;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.6f,.9f,1);return t;}
        public void Reset(){foreach(var l in lines)if(l)l.enabled=false;foreach(var t in letters)if(t)t.gameObject.SetActive(false);if(subtitle)subtitle.gameObject.SetActive(false);if(credits)credits.gameObject.SetActive(false);if(originalKey)originalKey.gameObject.SetActive(false);if(shardRenderer)shardRenderer.enabled=false;if(cinema)cinema.Clear();if(duelContacts)duelContacts.Clear();}
        static bool In(float t,float a,float b)=>t>=a&&t<b;
        static float S(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        bool Present(int slot)=>roles[slot].HasParticipant;
        Vector3 Hand(int slot,bool left=false)=>roles[slot].Hand(left);
        LineRenderer Ribbon(Color color,float width,float gain=1)
        {
            if(cursor>=lines.Length)return null;var l=lines[cursor++];l.enabled=true;l.widthMultiplier=width;l.startColor=l.endColor=Color.white;
            block.Clear();block.SetColor("_Color",color);block.SetFloat("_Gain",gain);l.SetPropertyBlock(block);return l;
        }
        void Arc(Vector3 center,float radius,Quaternion rotation,Color color,float width=.035f,float gain=1,float turns=1,float wobble=0)
        {
            if(radius<=.01f)return;var l=Ribbon(color,width,gain);if(!l)return;for(int j=0;j<64;j++){float a=j/63f*Mathf.PI*2*turns;float r=radius*(1+wobble*Mathf.Sin(a*7+time*3));points[j]=center+rotation*new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);}l.SetPositions(points);
        }
        void Beam(Vector3 a,Vector3 b,Color color,float width,float twist=0,float phase=0,float strength=1)
        {
            if(strength<=.0001f)return;var l=Ribbon(color,width,strength);if(!l)return;var axis=(b-a).normalized;var u=Vector3.Cross(axis,Vector3.up).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;var v=Vector3.Cross(axis,u);
            for(int j=0;j<64;j++){float f=j/63f,w=f*Mathf.PI*2*5-time*12+phase;points[j]=Vector3.Lerp(a,b,f)+(u*Mathf.Cos(w)+v*Mathf.Sin(w))*twist*Mathf.Sin(f*Mathf.PI);}l.SetPositions(points);
        }
        void Wave(Vector3 origin,float age,Color color,float speed=5,int layers=3)
        {for(int i=0;i<layers;i++){float r=age*speed-i*.65f;if(r>.02f)Arc(origin,r,Quaternion.Euler(90,0,0),color,.045f,Mathf.Clamp01(1-r/24));}}
        void Sonar(Vector3 origin,float age,bool deep)
        {float radius=age*(deep?6:7);for(int i=0;i<(deep?2:quality==0?3:6);i++)Arc(origin,radius,Quaternion.Euler(i*31,i*23,0),RootKeys.Slots[6].color,deep?.012f:.022f,Mathf.Clamp01(1-radius/42)*(deep?.24f:1));}
        public void Render(ShowFrame frame,Vector3 mouth,Vector3 heart)
        {
            time=(float)frame.time;float t=time;cursor=0;
            if(corridor){corridor.gameObject.SetActive(t<21);corridor.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*.14f)*2);corridor.localScale=Vector3.one*Mathf.Lerp(1,.46f,S(20,21,t));}
            if(t<21){for(int i=0;i<12;i++){float depth=Mathf.Repeat(i*6-t*8,72)-5;Arc(new Vector3(0,2,depth),5.5f+.2f*Mathf.Sin(i),Quaternion.Euler(0,0,i*15+t*4),RootKeys.Spectrum(i/12f),.06f,.55f);} }
            if(In(t,20,23))Wave(new Vector3(0,.08f,3),t-20,new Color(.25f,.7f,1),3.7f,4);
            if(In(t,21,54)||In(t,92,101))for(int i=0;i<10;i++){
                if(!Present(i))continue;var p=roles[i].Position;p.y=.045f;float step=Mathf.Repeat(t-21+i*.13f,1.25f);Arc(p,.18f+step*.58f,Quaternion.Euler(90,0,0),RootKeys.Slots[i].color,.017f,(1-step/1.25f)*.55f);
                if(i<4&&In(t,34,47))Beam(p+Vector3.up*.005f,roles[i].Position+Vector3.up*.05f,RootKeys.Slots[i].color,.025f);
            }
            if(In(t,26,30))Wave(new Vector3(0,.06f,16),t-26,RootKeys.Slots[0].color,5,4);
            if(In(t,31,34)&&Present(6))Sonar(Hand(6),t-31,false);
            if(In(t,55,59)&&Present(6))Sonar(Hand(6),(t-55)*.65f,false);
            if(In(t,59,61))for(int i=0;i<12;i++)Arc(lord.root.position+Vector3.up*2,(t-59)*12,Quaternion.Euler(i*15,i*30,0),RootKeys.Spectrum(i/12f),.025f,.75f);
            bool ward=In(t,47,50)||In(t,70,81);
            if(ward){float level=In(t,70,81)?1:.7f;Vector3 center=new Vector3(0,.1f,0);Color c=RootKeys.Slots[4].color;int n=cinema?4:quality==0?4:8;
                for(int i=0;i<n;i++)Arc(center,6.2f,Quaternion.Euler(0,i*180f/n,0),c,cinema?.018f:.035f,(cinema?.24f:.5f)*level,.5f);
                for(int i=0;i<3;i++){float h=1+i*1.5f;Arc(center+Vector3.up*h,Mathf.Sqrt(6.2f*6.2f-h*h),Quaternion.Euler(90,0,0),c,cinema?.018f:.03f,cinema?.24f:.5f);}

            }
            if(In(t,47,49))Wave(new Vector3(0,.07f,14f),t-47,RootKeys.Slots[0].color,7,3);
            if(In(t,50,54)&&Present(8))for(int i=0;i<5;i++)Arc(Hand(8),1+(t-50)*3-i*.24f,Quaternion.Euler(10,i*37,t*24),RootKeys.Slots[8].color,.05f,.7f,1,.10f);
            if(In(t,52,55))for(int i=0;i<8;i++){float z=3+i*2;float amount=S(52+i*.14f,53.6f+i*.14f,t);Beam(new Vector3(-8*(1-amount),.04f,z),new Vector3(8*(1-amount),.04f,z),RootKeys.Slots[7].color,.025f);}
            if(In(t,63,68.8f))for(int i=0;i<3;i++)Wave(new Vector3((i-1)*7,.1f,12),t-63-i*.22f,new Color(.45f,.6f,.8f),4,2);
            if(In(t,66.3f,68.8f))for(int i=0;i<3;i++)Arc(mouth,(t-66.3f)*2-i*.3f,Quaternion.identity,new Color(.3f,.65f,1),.06f);
            if(In(t,69,81)){
                Vector3 target=DragonBreath.Target(t,lord.root.position);float strength=DragonBreath.Strength(t);
                Beam(mouth,target,new Color(.35f,.65f,1),Mathf.Lerp(.16f,.35f,DragonBreath.SweepBlend(t)),.08f,0,strength);
                Beam(mouth,target,new Color(.1f,.32f,1,.35f),Mathf.Lerp(.5f,1,DragonBreath.SweepBlend(t)),.17f,Mathf.PI,strength);
                Arc(target,.6f,Quaternion.identity,RootKeys.Slots[4].color,.09f,strength,1,.12f);

            }
            duelContacts.Render(t,lord.rightHand.position,heart,Battle,quality);
            float counter=BattleScore.Counter(t);
            if(counter>0){Beam(lord.rightHand.position,heart,new Color(.15f,.7f,1),.075f,.035f,0,counter);Beam(lord.rightHand.position,heart,new Color(.8f,.95f,1),.024f,0,0,counter);}
            if(In(t,83,90)&&Present(6))Sonar(Hand(6),Mathf.Repeat(t-83,3.5f),true);
            originalKey.gameObject.SetActive(In(t,83,98));originalKey.transform.position=heart+new Vector3(0,.3f,-.7f);originalKey.color=RootKeys.Slots[2].color;
            if(In(t,92,98)&&Battle!=null){
                for(int i=0;i<10;i++)if((Battle.supportMask&(1<<i))!=0){
                    float angle=i*Mathf.PI/5;var orbit=heart+new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),-.3f)*1.5f;
                    Arc(orbit,.10f,Quaternion.identity,RootKeys.Slots[i].color,.02f,.4f+Battle.finaleCharge*.6f);
                }
            }
            if(In(t,98,101)){for(int i=0;i<5;i++)Arc(Vector3.Lerp(new Vector3(0,3.7f,6),heart,S(98,99,t)),.2f+(t-98)*2+i*.2f,Quaternion.Euler(i*36,0,0),RootKeys.Spectrum(i*.2f),.07f);}
            if(In(t,101,106))Wave(new Vector3(0,.06f,10),t-101,Color.cyan,5,4);
            if(cinema){cinema.Battle=Battle;cinema.Evaluate(t,mouth,heart,quality);shardRenderer.enabled=false;}else UpdateShards(t,heart);
            bool title=t>=111&&!cinemaTitle;for(int i=0;i<11;i++){
                letters[i].gameObject.SetActive(title);if(!title)continue;float f=S(111+i*.09f,116+i*.09f,t);Vector3 start=new Vector3(i%2==0?-14:14,3+(i%5)*1.9f,8+(i%3)*4);
                letters[i].transform.position=Vector3.Lerp(start,letterHomes[i],f)+Vector3.up*Mathf.Sin(f*Mathf.PI)*2;letters[i].transform.rotation=Quaternion.Euler(0,(1-f)*(i%2==0?60:-60),(1-f)*(i%2==0?-45:45));letters[i].color=Color.Lerp(RootKeys.Slots[i%10].color,new Color(.72f,.93f,1),f);
                if(f<1)Beam(start,letters[i].transform.position,RootKeys.Slots[i%10].color,.024f,.08f);
            }
            subtitle.gameObject.SetActive(title);credits.gameObject.SetActive(title);subtitle.color=new Color(.57f,.8f,.91f,S(115,118,t));credits.color=new Color(.46f,.64f,.75f,S(117,120,t));
            ActiveRibbonCount=cursor;for(int i=cursor;i<lines.Length;i++)lines[i].enabled=false;
        }
        void UpdateShards(float t,Vector3 heart)
        {
            bool hit=In(t,39,41),columns=In(t,63,68),fracture=In(t,90,92),peel=In(t,98,101);shardRenderer.enabled=hit||columns||fracture||peel;if(!shardRenderer.enabled)return;
            float age=t-(hit?39:columns?63:fracture?90:98);Vector3 center=hit?orcs.Target(0):heart;
            for(int i=0;i<96;i++){
                float a=i*2.399963f,seed=(i*37%97)/97f;Vector3 dir=new Vector3(Mathf.Cos(a),seed*1.6f-.5f,Mathf.Sin(a));Vector3 p;
                if(peel){float u=Mathf.Clamp01((age-seed*.7f)/2.2f);p=Vector3.Lerp(heart+dir*2.8f,new Vector3(0,.1f,10),u)+new Vector3(Mathf.Sin(a+u*12),0,Mathf.Cos(a+u*12))*Mathf.Sin(u*Mathf.PI)*2;}
                else if(columns){p=new Vector3((i%3-1)*7,6+seed*5,12)+dir*age*.7f+Vector3.down*age*age*.8f;p.y=Mathf.Max(.08f,p.y);}
                else p=center+dir*age*(hit?2:1.3f)+Vector3.down*age*age*.5f;
                float size=(peel?.06f:.10f)*(quality==0&&i%2==1?0:1)*(1-Mathf.Clamp01(age/(peel?3:columns?5:2)));
                var q=Quaternion.Euler(i*31+age*115,i*17+age*70,age*170);var color=peel?RootKeys.Spectrum(seed):columns?new Color(.25f,.29f,.34f):hit?RootKeys.Slots[2].color:RootKeys.Slots[8].color;
                for(int j=0;j<4;j++){Vector3 v=j==0?new Vector3(0,2,0):j==1?new Vector3(-1,-1,1):j==2?new Vector3(1,-1,1):new Vector3(0,-1,-1);shardVertices[i*4+j]=p+q*v*size;shardColors[i*4+j]=color;}
            }
            shards.vertices=shardVertices;shards.colors=shardColors;
        }
        void OnDestroy(){if(shards){if(Application.isPlaying)Destroy(shards);else DestroyImmediate(shards);}foreach(var m in owned)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}

