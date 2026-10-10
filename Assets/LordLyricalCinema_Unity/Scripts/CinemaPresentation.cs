using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // One bounded pool for aperture dust, room motes and the final release.
    // Everything is sampled from song time, including backwards seeks and operator restarts.
    public sealed class CinemaPresentation : IDisposable
    {
        public const int ParticleBudget=960;
        readonly Transform root,title,floorSeal,floorResonance;
        readonly Material titleMaterial,sealMaterial,roomMaterial,sparkMaterial;
        readonly TextMesh subtitle,credits;readonly CinemaSoundscape sound;
        readonly Mesh quad;
        readonly ParticleSystem particles;
        readonly ParticleSystemRenderer particleRenderer;
        readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[ParticleBudget];
        readonly List<ReleaseSurface> surfaces=new();
        readonly MaterialPropertyBlock block=new();
        readonly List<Material> owned=new();
        readonly PersonalFarewell40 personal;readonly ResonantWorld world;readonly Camera view;readonly SpectralDrift35 spectral;
        TrackedRig rig;NetworkSessionController session;
        bool swapped,disposed;
        int count;
        sealed class ReleaseSurface { public Renderer renderer;public Material[] original,peel;public bool founder; public float seed; }
        public SpatialRoomSurvey Survey {get;}
        public float RealmRelease {get;private set;}
        public float TitleOpacity {get;private set;}
        public int ActiveParticles=>count;
        public Transform Root=>root;
        public CinemaPresentation(ResonantWorld world,Camera camera,CinematicParticleDirector source)
        {
            this.world=world;view=camera;root=new GameObject("Cinema 26 | room, portal and return").transform;
            personal=new PersonalFarewell40(root);sound=root.gameObject.AddComponent<CinemaSoundscape>();spectral=new SpectralDrift35(root);
            quad=new Mesh{name="Cinema artwork plane",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};quad.RecalculateBounds();
            titleMaterial=new Material(Resources.Load<Shader>("Cinema26/Title"));titleMaterial.mainTexture=Resources.Load<Texture2D>("Cinema26/LordLyricalTitle");
            sealMaterial=new Material(titleMaterial);sealMaterial.mainTexture=Resources.Load<Texture2D>("Cinema26/ResonanceSigil");sealMaterial.SetColor("_Color",new Color(.23f,.68f,1));
            title=Plane("LORD LYRICAL | engraved title",titleMaterial,new Vector3(0,2,5),new Vector3(3.1f,2.067f,1));
            floorSeal=Plane("Landing seal",sealMaterial,new Vector3(0,.018f,5),Vector3.one*2.7f);floorSeal.rotation=Quaternion.Euler(90,0,0);
            subtitle=Text("Title subtitle","THE RESONANT REALM",new Vector3(0,.88f,4.95f),.014f);
            credits=Text("Creator credits","JERU THE DAMAJA\n\n5TH YANG LLC  ·  MAXIMUM WARP ENTERTAINMENT",new Vector3(0,.54f,4.95f),.009f);
            roomMaterial=new Material(Resources.Load<Shader>("Cinema26/RoomResonance"));roomMaterial.mainTexture=Resources.Load<Texture2D>("Cinema26/ResonanceSigil");
            floorResonance=Plane("MR landing resonance | transparent floor",roomMaterial,new Vector3(0,.022f,5),new Vector3(9,9,1));floorResonance.rotation=Quaternion.Euler(90,0,0);
            rig=UnityEngine.Object.FindFirstObjectByType<TrackedRig>();session=UnityEngine.Object.FindFirstObjectByType<NetworkSessionController>();
            Survey=new GameObject("Measured room | tracking-space overlay").AddComponent<SpatialRoomSurvey>();
            Survey.Initialize(camera.transform.parent?camera.transform.parent:rig?rig.transform:null,roomMaterial);
            var pg=new GameObject("Cinema | pooled release sparks");pg.transform.SetParent(root,false);
            particles=pg.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.playOnAwake=false;main.maxParticles=ParticleBudget;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
            sparkMaterial=new Material(source?source.sparksMaterial:Resources.Load<Material>("Concert/BeatAccents"));
            if(sparkMaterial.HasProperty("_Monochrome"))sparkMaterial.SetFloat("_Monochrome",1);
            if(sparkMaterial.HasProperty("_NearFade"))sparkMaterial.SetFloat("_NearFade",.12f);
            if(sparkMaterial.HasProperty("_SoftDistance"))sparkMaterial.SetFloat("_SoftDistance",0);
            particleRenderer=pg.GetComponent<ParticleSystemRenderer>();particleRenderer.sharedMaterial=sparkMaterial;particleRenderer.renderMode=ParticleSystemRenderMode.Stretch;particleRenderer.velocityScale=.025f;particleRenderer.lengthScale=2;particleRenderer.maxParticleSize=.025f;particleRenderer.shadowCastingMode=ShadowCastingMode.Off;particleRenderer.receiveShadows=false;
            particleRenderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
            particles.useAutoRandomSeed=false;particles.randomSeed=2601;particles.Play();particles.Pause();
            var peelShader=Resources.Load<Shader>("Cinema26/RealmPeel");
            var materialCache=new Dictionary<Material,Material>();
            var founder=world.FounderRoot;
            foreach(var r in world.GetComponentsInChildren<Renderer>(true)){
                if(r is ParticleSystemRenderer||r is LineRenderer||r is TrailRenderer||r.GetComponent<TextMesh>())continue;
                var materials=r.sharedMaterials;bool any=false;var replacements=new Material[materials.Length];
                for(int i=0;i<materials.Length;i++){
                    var m=materials[i];replacements[i]=m;if(!m||m.renderQueue>=3000)continue;
                    if(m.shader.name=="LordLyrical/Chamber Surface"){any=true;continue;}
                    if(materialCache.TryGetValue(m,out var cached)){replacements[i]=cached;any=true;continue;}
                    var replacement=new Material(peelShader);replacement.name=m.name+" | final release";
                    foreach(string prop in new[]{"_BaseMap","_EmissionMap"})if(m.HasProperty(prop))replacement.SetTexture(prop,m.GetTexture(prop));
                    if(!m.HasProperty("_BaseMap")&&m.HasProperty("_MainTex"))replacement.SetTexture("_BaseMap",m.GetTexture("_MainTex"));
                    foreach(string prop in new[]{"_BaseColor","_EmissionColor"})if(m.HasProperty(prop))replacement.SetColor(prop,m.GetColor(prop));
                    if(!m.HasProperty("_BaseColor")&&m.HasProperty("_Color"))replacement.SetColor("_BaseColor",m.GetColor("_Color"));
                    foreach(string prop in new[]{"_Metallic","_Smoothness","_Cull"})if(m.HasProperty(prop))replacement.SetFloat(prop,m.GetFloat(prop));
                    if(m.HasProperty("_BaseMap")){replacement.SetTextureScale("_BaseMap",m.GetTextureScale("_BaseMap"));replacement.SetTextureOffset("_BaseMap",m.GetTextureOffset("_BaseMap"));}
                    replacements[i]=replacement;materialCache.Add(m,replacement);owned.Add(replacement);any=true;
                }
                if(any)surfaces.Add(new ReleaseSurface{renderer=r,original=materials,peel=replacements,founder=founder&&r.transform.IsChildOf(founder),seed=Hash(surfaces.Count*179)});
            }
        }
        Transform Plane(string name,Material m,Vector3 p,Vector3 scale){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=scale;go.AddComponent<MeshFilter>().sharedMesh=quad;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go.transform;}
        TextMesh Text(string name,string words,Vector3 p,float size){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=p;var text=go.AddComponent<TextMesh>();text.text=words;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;text.fontSize=96;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;return text;}
        public static float Hash(int seed){unchecked{uint n=(uint)seed*747796405u+2891336453u;n=((n>>((int)(n>>28)+4))^n)*277803737u;return ((n>>22)^n)/4294967295f;}}
        void Add(Vector3 p,Vector3 velocity,float size,float alpha,int seed,bool gold=false){
            if(count>=ParticleBudget||alpha<.003f)return;
            Color c=gold?new Color(1,.68f,.26f,alpha):new Color(.32f,.72f,1,alpha);
            buffer[count++]=new ParticleSystem.Particle{position=p,velocity=velocity,startSize=size,startColor=c,startLifetime=10,remainingLifetime=5,randomSeed=(uint)(seed+1)};
        }
        public void Render(float t,bool waiting)
        {
            if(disposed)return;Magic33Score.SetClock(t,waiting);sound.Render(t,waiting);count=0;bool ending=CinemaTiming.Returning(t);
            if(ending&&world.FounderRoot){var p=world.FounderRoot.localPosition;p.y=ConductorRoute.Position(121).y+CinemaTiming.Hover(t);world.FounderRoot.localPosition=p;}
            spectral.Render(t,!waiting,view,1);
            Personal40(t,!waiting);
            Shader.SetGlobalFloat("_RealmEntrance41",Transition41.Entrance(t,waiting));
            RealmRelease=CinemaTiming.Realm(t);Shader.SetGlobalFloat("_RealmDissolve",RealmRelease);
            bool wasEnding=swapped;
            if(swapped!=ending){
                swapped=ending;foreach(var s in surfaces)if(s.renderer)s.renderer.sharedMaterials=ending?s.peel:s.original;
            }
            if(ending||wasEnding)foreach(var s in surfaces)if(s.renderer){
                s.renderer.GetPropertyBlock(block);
                block.SetFloat("_CinemaDissolve",s.founder?CinemaTiming.Founder(t):RealmRelease);
                block.SetFloat("_CinemaActor",s.founder?1:0);
                if(s.founder){var b=s.renderer.bounds;block.SetVector("_CinemaBounds",new Vector4(b.min.y,b.max.y,0,0));}
                s.renderer.SetPropertyBlock(block);
            }
            float energy=CinemaTiming.RoomEnergy(t);
            // Hide scan overlays while virtual architecture is crossing into/out of the room.
            if(t>=0&&t<CinemaTiming.RealmGone)energy*=t<9?1-CinemaTiming.S(0,1.4f,t):0;
            else if(t>=CinemaTiming.RealmGone)energy*=CinemaTiming.S(CinemaTiming.RealmGone,CinemaTiming.RealmGone+.8f,t);
            roomMaterial.SetFloat("_Energy",energy);roomMaterial.SetFloat("_Clock",t);
            roomMaterial.SetFloat("_PortalRadius",t>=0&&t<9?OpeningTiming.PortalRadius(t):0);
            // Measured surfaces may carry light, but never punch holes in the authored transition.
            Survey.SetOcclusion(false,0);
            Survey.Present(energy>.001f,waiting);floorResonance.gameObject.SetActive(!waiting&&energy>.001f&&Survey.SurfaceCount==0);
            if(!waiting)Room38(t);
            TitleOpacity=CinemaTiming.Title(t);
            title.gameObject.SetActive(TitleOpacity>.001f);titleMaterial.SetFloat("_Opacity",TitleOpacity);titleMaterial.SetFloat("_Dissolve",CinemaTiming.TitleDissolve(t));
            title.localScale=new Vector3(3.1f,2.067f,1)*(t<0?1:Mathf.Lerp(.94f,1,CinemaTiming.S(CinemaTiming.EndTitleStart,CinemaTiming.EndTitleFull,t)));
            subtitle.gameObject.SetActive(TitleOpacity>.001f);subtitle.color=new Color(.72f,.83f,.90f,TitleOpacity);
            credits.gameObject.SetActive(t>=CinemaTiming.EndTitleStart&&TitleOpacity>.001f);credits.color=new Color(.61f,.72f,.8f,TitleOpacity*CinemaTiming.S(CinemaTiming.EndTitleFull,CinemaTiming.EndTitleFull+.4f,t));
            float seal=CinemaTiming.S(-4.5f,-2.2f,t)*.16f*(1-CinemaTiming.S(-1,-.8f,t))+CinemaTiming.S(-.85f,-.55f,t)*(1-CinemaTiming.S(4,8,t));
            floorSeal.gameObject.SetActive(seal>.001f);sealMaterial.SetFloat("_Opacity",seal*.7f);
            floorSeal.localScale=Vector3.one*Mathf.Lerp(1.4f,3.6f,CinemaTiming.S(-.8f,3,t));
            if(!waiting&&t>=0&&t<9){
                float radius=OpeningTiming.PortalRadius(t),fade=1-CinemaTiming.S(7.4f,9,t);
                for(int i=0;i<260;i++){
                    float u=Hash(i*31),age=Mathf.Repeat(t*(.2f+u*.3f)+Hash(i*97),1);
                    float angle=i*2.399963f+t*(.65f+u*.6f),r=radius*(.96f+Mathf.Pow(age,3)*.20f);
                    Vector3 radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    Vector3 p=OpeningTiming.PortalCenter+radial*r+Vector3.forward*(age-.5f)*.3f;
                    Add(p,new Vector3(-radial.y,radial.x,0)*1.5f,.015f+u*.032f,fade*Mathf.Sin(age*Mathf.PI),i,u>.85f);
                }
            }
            if(energy>.001f){
                for(int i=0;i<120;i++){
                    if(!Survey.TrySurfacePoint(i*17,out var p)){
                        float angle=i*2.399963f,radius=.5f+Hash(i*19)*2.4f;
                        p=new Vector3(Mathf.Cos(angle)*radius,.025f,5+Mathf.Sin(angle)*radius);
                    }
                    float life=Mathf.Repeat(t*.27f+Hash(i*109),1);
                    p+=Vector3.up*life*.55f;
                    Add(p,Vector3.up*.4f,.012f+Hash(i*83)*.02f,energy*Mathf.Sin(life*Mathf.PI)*.65f,400+i,i%5==0);
                }
            }
            float offering=Magic33Score.PortalOffering(t);
            if(!waiting&&offering>0){
                if(rig&&rig.Hands){
                    if(rig.Hands.Left.tracked&&rig.Hands.Left.open)Offering(t,rig.Hands.Left.position,0,offering);
                    if(rig.Hands.Right.tracked&&rig.Hands.Right.open)Offering(t,rig.Hands.Right.position,1,offering);
                }
                for(int slot=0;slot<10;slot++)if(!session||slot!=session.Slot)for(int side=0;side<2;side++)
                    if(NetworkRoleAvatar.TryGetOpenPalm(slot,side==0,out var p))Offering(t,p,2+slot*2+side,offering);
            }
            if(ending&&t<CinemaTiming.FounderGone+1.6f&&surfaces.Count>0){
                for(int i=0;i<260;i++){
                    var s=surfaces[(i*37)%surfaces.Count];if(!s.renderer||!s.renderer.gameObject.activeInHierarchy)continue;
                    float start=s.founder?CinemaTiming.FounderFadeStart+Hash(i*43)*1.7f:CinemaTiming.ReturnStart+Hash(i*43)*3.8f;
                    float age=t-start;if(age<0||age>2.4f)continue;
                    var bounds=s.renderer.bounds;
                    Vector3 unit=new Vector3(Hash(i*79)*2-1,Hash(i*83)*2-1,Hash(i*97)*2-1);
                    int axis=i%3;if(axis==0)unit.x=unit.x<0?-1:1;else if(axis==1)unit.y=unit.y<0?-1:1;else unit.z=unit.z<0?-1:1;
                    Vector3 p=bounds.center+Vector3.Scale(bounds.extents,unit);
                    float a=i*2.399963f+age*1.3f;var velocity=new Vector3(Mathf.Cos(a)*.3f,.7f,Mathf.Sin(a)*.3f);
                    p+=velocity*age+Vector3.up*age*age*.13f;
                    Add(p,velocity,.014f+Hash(i*61)*.029f,CinemaTiming.S(0,.14f,age)*(1-CinemaTiming.S(1.2f,2.4f,age)),800+i,i%4==0);
                }
            }
            // Landing pulse is world anchored and quiet against the master track.
            float landingAge=t-(-.8f);
            if(landingAge>=0&&landingAge<1.05f){
                for(int i=0;i<72;i++){
                    float angle=i*2.399963f,r=.22f+landingAge*(.9f+Hash(i*71)*1.3f);
                    Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var p=OpeningTiming.JeruPosition((-.8f))+radial*r+Vector3.up*(.035f+landingAge*(.2f+Hash(i)*.45f));
                    Add(p,radial*.7f+Vector3.up*.3f,.014f+Hash(i)*.015f,(1-CinemaTiming.S(.12f,1.05f,landingAge))*.65f,2400+i,i%4==0);
                }
            }
            float goodbyeAge=t-CinemaTiming.FounderGone;
            if(goodbyeAge>=0&&goodbyeAge<.8f&&world.FounderRoot){
                for(int i=0;i<64;i++){
                    float angle=i*2.399963f;var radial=new Vector3(Mathf.Cos(angle),Hash(i)*.8f,Mathf.Sin(angle));
                    var p=world.FounderRoot.position+Vector3.up*1.2f+radial*(.05f+goodbyeAge*.65f);
                    Add(p,radial*.65f,.008f+Hash(i)*.01f,(1-CinemaTiming.S(.04f,.8f,goodbyeAge))*.6f,2600+i,i%6==0);
                }
            }
            // A levitation wake rises around JERU, then contracts into the final release.
            if(ending&&t<CinemaTiming.FounderGone&&world.FounderRoot){
                float power=CinemaTiming.S(116,119,t)*(1-CinemaTiming.Founder(t));
                var origin=world.FounderRoot.position;
                for(int i=0;i<140;i++){
                    float u=Mathf.Repeat(i/140f+(t-116)*.19f,1),a=i*2.399963f+t*.55f;
                    float r=(.48f+.18f*Mathf.Sin(u*Mathf.PI))*(1-CinemaTiming.Founder(t)*.6f);
                    Vector3 p=origin+new Vector3(Mathf.Cos(a)*r,u*2.1f-.15f,Mathf.Sin(a)*r);
                    Add(p,new Vector3(-Mathf.Sin(a)*.15f,.25f,Mathf.Cos(a)*.15f),.009f+Hash(i*73)*.012f,power*Mathf.Sin(u*Mathf.PI)*.7f,1800+i,i%7==0);
                }
            }
            // Compact outward dust bloom reveals the title only after the performer is gone.
            float titleAge=t-CinemaTiming.EndTitleStart;
            if(titleAge>=0&&titleAge<1.1f){
                for(int i=0;i<96;i++){
                    float a=i*2.399963f,r=.12f+titleAge*(.65f+Hash(i*73)*1.1f);
                    Vector3 radial=new Vector3(Mathf.Cos(a)*1.4f,Mathf.Sin(a)*.7f,0);
                    Add(title.position+radial*r,radial*.7f,.012f,(1-CinemaTiming.S(.08f,1.1f,titleAge))*.65f,2200+i,i%3==0);
                }
            }
            particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);particleRenderer.enabled=count>0;
        }
        void Personal40(float t,bool active)
        {
            if(!rig)rig=UnityEngine.Object.FindFirstObjectByType<TrackedRig>();
            var l=rig&&rig.Hands?rig.Hands.Left:default;var r=rig&&rig.Hands?rig.Hands.Right:default;
            var chosen=r.tracked&&r.open?r:l.tracked&&l.open?l:r.tracked?r:l;
            Color color=session&&session.Slot>=0?RootKeys.Slots[session.Slot].color:new Color(.83f,.88f,.94f);
            personal.RenderInteractive(t,active,view?view.transform.position:new Vector3(0,1.7f,2),view?view.transform.rotation:Quaternion.identity,l,r,world.FounderRoot?world.FounderRoot.position:new Vector3(0,0,5),color,Time.unscaledDeltaTime);
            float play=active?BattleScore.W(-4.5f,-2.5f,6.5f,8,t):0;
            for(int side=0;side<2;side++){
                var h=side==0?l:r;if(!h.tracked||play<=0)continue;
                for(int i=0;i<24;i++){float u=Mathf.Repeat(t*.35f+Hash(i*73+side*41),1),a=i*2.399963f+t*.25f;float radius=h.open?Mathf.Lerp(.16f,.045f,u):Mathf.Lerp(.05f,.22f,u);Vector3 radial=h.rotation*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));int before=count;Add(h.position+radial*radius+h.direction*.06f,radial*.1f,.005f+Hash(i*19)*.003f,play*Mathf.Sin(u*Mathf.PI)*.30f,3000+side*41+i);if(count>before){var c=color;c.a=play*Mathf.Sin(u*Mathf.PI)*.30f;buffer[count-1].startColor=c;}}
            }
            if(active&&personal.Visible)for(int i=0;i<24;i++){float a=i*2.399963f+t*.6f,u=Hash(i*17);int before=count;Add(personal.Position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*(.025f+u*.055f),Vector3.up*.04f,.0035f,StoryScore40.Farewell(t)*.28f,3100+i);if(count>before){var c=color;c.a=StoryScore40.Farewell(t)*.28f;buffer[count-1].startColor=c;}}
        }
        void Room38(float t)
        {
            float opening=CinemaTiming.S(0,.3f,t)*(1-CinemaTiming.S(6.3f,8.2f,t));
            float returning=CinemaTiming.S(CinemaTiming.RealmGone,CinemaTiming.RealmGone+.8f,t)*(1-CinemaTiming.S(CinemaTiming.FounderFadeStart,CinemaTiming.FounderGone,t));
            float arrival=CinemaTiming.S(-.85f,-.65f,t)*(1-CinemaTiming.S(.5f,1.3f,t));
            float power=Mathf.Max(opening,Mathf.Max(returning,arrival));if(power<.001f)return;
            int budget=Time.unscaledDeltaTime>.017f&&Application.isPlaying?80:160;
            for(int i=0;i<budget;i++){
                float seed=Hash(i*137),u=Mathf.Repeat(t*(.21f+seed*.25f)+seed,1);
                bool measured=Survey.TrySurfacePoint(i*53,out var surface);
                if(!measured){float angle=i*2.399963f;surface=new Vector3(Mathf.Cos(angle)*(1+seed*2),.03f,5+Mathf.Sin(angle)*(1+seed*2));}
                Vector3 p,velocity;
                if(opening>0){
                    float angle=i*2.399963f;Vector3 edge=OpeningTiming.PortalCenter+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*OpeningTiming.PortalRadius(t);
                    p=Vector3.Lerp(surface,edge,u*u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.18f;velocity=(edge-surface).normalized*.55f;
                }else{p=surface+Vector3.up*u*(returning>0?.35f:.85f);velocity=Vector3.up*.45f;}
                int before=count;Add(p,velocity,.007f+seed*.017f,power*Mathf.Sin(u*Mathf.PI)*.48f,2600+i,i%5==0);
                if(count>before){Color colour=i%5==0?new Color(1,.72f,.33f):new Color(.83f,.87f,.91f);if(i%7==0&&session&&session.Slot>=0)colour=RootKeys.Slots[session.Slot].color;colour.a=power*Mathf.Sin(u*Mathf.PI)*.48f;buffer[count-1].startColor=colour;}
            }
        }
        void Offering(float t,Vector3 palm,int id,float power)
        {
            float angle=id*2.399963f+t*.12f;
            Vector3 edge=OpeningTiming.PortalCenter+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*OpeningTiming.PortalRadius(t);
            for(int j=0;j<12;j++){
                float u=Mathf.Repeat(j/12f+t*.7f,1);Vector3 p=Vector3.Lerp(palm,edge,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.15f;
                Add(p,(edge-palm).normalized*.6f,.008f+.01f*u,power*Mathf.Sin(u*Mathf.PI)*.45f,1400+id*17+j);
            }
        }
        static void Release(UnityEngine.Object o){if(!o)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
        public void Dispose(){
            Shader.SetGlobalFloat("_RealmEntrance41",1);
            if(disposed)return;disposed=true;Shader.SetGlobalFloat("_RealmDissolve",0);
            foreach(var s in surfaces)if(s.renderer){s.renderer.sharedMaterials=s.original;s.renderer.GetPropertyBlock(block);block.SetFloat("_CinemaDissolve",0);s.renderer.SetPropertyBlock(block);}
            personal.Dispose();spectral.Dispose();if(Survey)Release(Survey.gameObject);Release(root.gameObject);Release(quad);Release(titleMaterial);Release(sealMaterial);Release(roomMaterial);Release(sparkMaterial);
            foreach(var m in owned)Release(m);
        }
    }
}