using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LordLyrical
{
    // One dynamic mesh for all notation; all motion derives from the shared song clock.
    public sealed class ConcertPresentation : MonoBehaviour
    {
        Mesh mesh;Material material;MeshRenderer meshRenderer;
        readonly List<Vector3> vertices=new(20000);readonly List<Color> colors=new(20000);readonly List<int> triangles=new(30000);
        readonly Vector3[] handPositions=new Vector3[20];int handCount;
        public float harmony=.3f;public ConcertState Battle;
        Transform stage;Light key,rim;Vector3 focal;int quality;
        ParticleSystem accents;readonly ParticleSystem.Particle[] particles=new ParticleSystem.Particle[512];
        public void Initialize(int q)
        {
            quality=q;mesh=new Mesh{name="Living score batched mesh"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;meshRenderer=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Resources.Load<Shader>("Concert/ScoreLight"));meshRenderer.sharedMaterial=material;
            meshRenderer.shadowCastingMode=ShadowCastingMode.Off;meshRenderer.receiveShadows=false;
            var prefab=Resources.Load<GameObject>("Concert/StageRig");if(prefab)stage=Instantiate(prefab,transform).transform;
            key=CreateLight("JERU warm key",new Vector3(-3,7,1),new Color(1,.72f,.42f),46);
            rim=CreateLight("JERU ice rim",new Vector3(3,6,10),new Color(.24f,.62f,1),55);
            var particleMaterial=Resources.Load<Material>("Concert/BeatAccents");
            if(particleMaterial){var g=new GameObject("Music transients · chandelier sparks");g.transform.SetParent(transform,false);accents=g.AddComponent<ParticleSystem>();var main=accents.main;main.playOnAwake=false;main.loop=false;main.maxParticles=512;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;var emission=accents.emission;emission.enabled=false;var shape=accents.shape;shape.enabled=false;var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.035f;renderer.localBounds=new Bounds(new Vector3(0,8,8),new Vector3(30,25,35));accents.Play();accents.Pause();}
        }
        static Color CooperativeColour(int mask,int index)
        {
            int count=0;for(int i=0;i<10;i++)if((mask&(1<<i))!=0)count++;
            if(count==0)return Color.white;int chosen=index%count;
            for(int i=0;i<10;i++)if((mask&(1<<i))!=0&&chosen--==0)return RootKeys.Slots[i].color;
            return Color.white;
        }
        Light CreateLight(string name,Vector3 position,Color c,float angle){var g=new GameObject(name);g.transform.SetParent(transform,false);g.transform.localPosition=position;var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=c;l.range=18;l.spotAngle=angle;l.shadows=LightShadows.None;return l;}
        public void Render(float t,bool showing,Vector3 jeru,int q)
        {
            quality=q;focal=jeru+Vector3.up*1.5f;var m=MusicReactiveSystem.Current;
            bool chamber=t>=21&&t<117;
            if(stage)stage.gameObject.SetActive(chamber);
            key.enabled=rim.enabled=chamber;key.transform.LookAt(focal);rim.transform.LookAt(focal);
            key.intensity=showing?2.4f+m.amplitude*.9f+harmony*.6f:0;rim.intensity=showing?2+m.bass*.8f:0;
            Shader.SetGlobalVector("_ConcertSpectrum",new Vector4(m.bass,m.mid,m.air,m.pulse));
            vertices.Clear();colors.Clear();triangles.Clear();handCount=0;
            if(showing){
                for(int i=0;i<10;i++)foreach(bool left in new[]{true,false})if(NetworkRoleAvatar.TryGetHand(i,left,out var p))handPositions[handCount++]=p;
                int count=quality==0?36:64;float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-18)/6));float end=1-Mathf.Clamp01((t-111)/6);
                for(int i=0;i<count;i++){
                    float a=i*2.399963f+t*(.023f+(i%4)*.008f);float radius=4.5f+(i%11)*.77f;
                    var p=new Vector3(Mathf.Sin(a)*radius,.5f+Mathf.Repeat(i*.37f+t*.13f,8.4f),3+Mathf.Cos(a)*radius);
                    float scatter=0;for(int h=0;h<handCount;h++){var d=p-handPositions[h];float force=Mathf.Clamp01(1-d.magnitude/1.3f);p+=d.normalized*force*.6f;scatter=Mathf.Max(scatter,force);}
                    float shimmer=.2f+.15f*Mathf.Sin(i+t*.7f)+m.pulse*.25f+scatter;
                    Color c=Color.Lerp(new Color(.18f,.52f,.9f),new Color(1,.67f,.22f),(i%5==0)?.85f:0)*(shimmer*reveal*end);
                    if(Camera.main)c*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,3,Vector3.Distance(Camera.main.transform.position,p)));
                    Note(p,.065f+(i%3)*.025f,Quaternion.Euler(0,-a*Mathf.Rad2Deg,Mathf.Sin(t*.5f+i)*9),c*1.4f,i%3);
                }
                if(Battle!=null)foreach(var orc in Battle.orcs){
                    float age=t-orc.cooperativeAt;if(orc.cooperativeMask==0||age<0||age>.45f)continue;
                    Vector3 centre=orc.lastHitPoint+orc.lastHitNormal*.08f;
                    float r=.12f+age*.8f,alpha=1-age/.45f;
                    for(int j=0;j<12;j++){
                        float a=j*Mathf.PI/6,an=(j+1)*Mathf.PI/6;
                        Stroke(centre+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*r,centre+new Vector3(Mathf.Cos(an),Mathf.Sin(an),0)*r,.018f*alpha,CooperativeColour(orc.cooperativeMask,j)*alpha);
                        float twist=a*2+t*8;
                        Vector3 braid=new Vector3(Mathf.Cos(twist),Mathf.Sin(twist),0)*.06f;
                        Stroke(centre+braid+Vector3.forward*(j/12f-.5f)*.5f,centre-braid+Vector3.forward*((j+1)/12f-.5f)*.5f,.012f*alpha,CooperativeColour(orc.cooperativeMask,j+1)*alpha);
                    }
                }
                // Architectural light traces rise into a canopy on the three authored musical releases.
                float earned=Battle!=null?Mathf.Exp(-Mathf.Max(0,t-Battle.lastSuccess)*2)*.4f+Battle.finaleCharge*.6f:0;
                float swell=Envelope(t,59,63)+Envelope(t,81,85)+Envelope(t,92,103)+earned;
                if(chamber){
                    for(int side=-1;side<=1;side+=2)for(int b=0;b<6;b++){
                        Vector3 start=new Vector3(side*9,.12f,1+b*3);Vector3 target=new Vector3(side*(2+Mathf.Sin(t*.2f+b)),10+swell*3,6+b*2);
                        Color c=Color.Lerp(new Color(.07f,.38f,.65f),new Color(.8f,.5f,.13f),swell*.65f)*(.14f+m.bass*.16f+swell*.35f);
                        Stroke(start,target,.012f+swell*.014f,c);
                    }
                    for(int ring=0;ring<3;ring++){
                        float r=5.5f+ring*1.6f;Color c=new Color(.07f,.38f,.63f)*(.18f+harmony*.23f+m.bass*.35f+swell*.45f);
                        for(int i=0;i<80;i++){
                            float a=i*Mathf.PI*2/80,an=(i+1)*Mathf.PI*2/80;
                            float frequency=TrackSpectrum35.Sample(t,(i/80f*23+ring*4)%23);
                            float frequencyNext=TrackSpectrum35.Sample(t,((i+1)/80f*23+ring*4)%23);
                            float y=9+ring*.65f+Mathf.Sin(a*5+t*.23f)*(.12f+swell*.5f)+frequency*.48f;
                            float yn=9+ring*.65f+Mathf.Sin(an*5+t*.23f)*(.12f+swell*.5f)+frequencyNext*.48f;
                            float breathing=r+m.bass*.15f;
                            Stroke(new Vector3(Mathf.Sin(a)*breathing,y,7+Mathf.Cos(a)*breathing),new Vector3(Mathf.Sin(an)*breathing,yn,7+Mathf.Cos(an)*breathing),.009f+swell*.01f,c*(.65f+frequency*.7f));
                        }
                    }
                }
            }
            float stageFocus=1-.60f*BattleScore.W(62.4f,63,100,102,t);
            for(int i=0;i<colors.Count;i++)colors[i]*=stageFocus*Magic33Score.Quiet(t);
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.bounds=new Bounds(new Vector3(0,7,8),new Vector3(50,32,55));
            if(accents){int n=0;var score=MusicReactiveSystem.Score;int lastBeat=m.beat;
                if(showing&&chamber&&score!=null)for(int b=lastBeat;b>=0&&b>lastBeat-8;b--){float age=t-score.beats[b];if(age<0||age>1.35f||score.strengths[b]<.55f)continue;int count=quality==0?20:42;
                    for(int j=0;j<count&&n<particles.Length;j++){
                        float a=j*2.399963f+b*.73f;float side=(b%2==0?-1:1);var origin=new Vector3(side*8,7.2f,4+b%4*3.5f);var velocity=new Vector3(Mathf.Sin(a)*1.4f,1.1f+(j%5)*.23f,Mathf.Cos(a)*1.4f);
                        var p=origin+velocity*age+Vector3.down*(age*age*.9f);float alpha=1-age/1.35f;
                        particles[n++]=new ParticleSystem.Particle{position=p,startSize=.025f+(j%4)*.012f,startColor=Color.Lerp(new Color(.13f,.5f,1),new Color(1,.57f,.12f),b%3==0?1:0)*alpha,startLifetime=2,remainingLifetime=1,randomSeed=(uint)(b*64+j+1)};
                    }
                }
                if(showing&&Battle!=null&&t>=92&&t<101){
                    float gather=HeroDuelScore.FinalGather(t),release=HeroDuelScore.FinalRelease(t);
                    float earned=Mathf.Clamp01(Battle.finaleCharge);
                    int count=quality==0?72:128;
                    for(int j=0;j<count&&n<particles.Length;j++){
                        float seed=j*2.399963f,phase=Mathf.Repeat(t*.42f+j*.618034f,1);
                        float radius=Mathf.Lerp(1.7f,.24f,phase)*(1-release)+release*(.3f+(t-98)*2.1f);
                        radius=Mathf.Max(.12f,radius);
                        Vector3 axis=new Vector3(Mathf.Sin(seed)*Mathf.Sin(seed*.71f),Mathf.Cos(seed*.71f),Mathf.Cos(seed)*Mathf.Sin(seed*.71f));
                        Vector3 pos=Battle.dragonHeart+axis*radius;
                        float life=Mathf.Sin(phase*Mathf.PI);
                        float alpha=Mathf.Clamp01(gather*.55f+release*.85f)*life;
                        Color tint=Color.Lerp(new Color(.18f,.65f,1),new Color(1,.78f,.34f),earned*.7f+release*.3f);
                        particles[n++]=new ParticleSystem.Particle{position=pos,startSize=(.025f+(j%4)*.008f)*(1+release),startColor=tint*alpha,startLifetime=2,remainingLifetime=1,randomSeed=(uint)(j+19001)};
                    }
                }
                accents.SetParticles(particles,n);accents.Simulate(0,false,false,false);
            }
        }
        static float Envelope(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((t-a)/.7f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-b+1)/1)));
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);for(int j=0;j<4;j++)colors.Add(color*StoryScore40.Quiet(MusicReactiveSystem.Current.time));triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);triangles.Add(i);triangles.Add(i+2);triangles.Add(i+3);}
        void Stroke(Vector3 a,Vector3 b,float w,Color c){var n=Vector3.Cross(b-a,Vector3.forward).normalized*w;if(n.sqrMagnitude<.000001f)n=Vector3.right*w;Quad(a-n,a+n,b+n,b-n,c);}
        void Note(Vector3 p,float s,Quaternion rotation,Color c,int kind){
            Vector3 P(float x,float y)=>p+rotation*new Vector3(x*s,y*s,0);
            for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10,b=(i+1)*Mathf.PI*2/10;Quad(P(0,0),P(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.48f),P(Mathf.Cos(b)*.8f,Mathf.Sin(b)*.48f),P(0,0),c);}
            Quad(P(.6f,0),P(.78f,0),P(.78f,2.5f),P(.6f,2.5f),c);
            if(kind>0)Quad(P(.7f,2.5f),P(1.8f,1.9f),P(1.7f,1.6f),P(.7f,2.15f),c);
            if(kind==2)Quad(P(.7f,1.9f),P(1.6f,1.35f),P(1.5f,1.08f),P(.7f,1.55f),c);
        }
        void OnDestroy(){if(Application.isPlaying){if(mesh)Destroy(mesh);if(material)Destroy(material);}else{if(mesh)DestroyImmediate(mesh);if(material)DestroyImmediate(material);}}
    }
}
