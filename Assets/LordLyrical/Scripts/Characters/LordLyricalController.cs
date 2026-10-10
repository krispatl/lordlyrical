using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    public sealed class LordLyricalController
    {
        public Transform root,leftHand,rightHand;readonly Material material;readonly Transform compass;
        readonly JeruPerformance performance;JeruConcertFace face;
        public void SampleFace(float time,bool live){if(performance){if(!face)face=performance.GetComponent<JeruConcertFace>();if(face)face.Sample(time,live);}}
        public LordLyricalController(Transform parent,Texture2D portrait,GameObject characterPrefab=null)
        {
            root=ResonantGeometry.Node("Lord Lyrical - eleventh presence",parent,new Vector3(0,0,5));material=ResonantGeometry.Material("Lord Lyrical full spectrum",Color.cyan,1);
            if(characterPrefab!=null)
            {
                var character=Object.Instantiate(characterPrefab,root);
                character.name="Jeru - rigged Founder";character.transform.localPosition=Vector3.zero;character.transform.localScale=Vector3.one*1.22f;
                performance=character.GetComponent<JeruPerformance>();
                if(performance!=null)
                {
                    performance.previewAutomatically=false;
                    if(performance.animator!=null && performance.animator.isHuman)
                    {
                        leftHand=performance.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        rightHand=performance.animator.GetBoneTransform(HumanBodyBones.RightHand);
                    }
                }
            }
            if(performance==null)
            {
            ResonantGeometry.MeshObject("Founder mantle",root,ResonantGeometry.Lathe(new[]{0f,.2f,1f,1.6f,1.85f},new[]{.4f,.45f,.26f,.6f,.14f},12),material,Vector3.zero,Vector3.one);
            var portraitMesh=new Mesh{vertices=new[]{new Vector3(-.45f,0,0),new Vector3(.45f,0,0),new Vector3(-.45f,.95f,0),new Vector3(.45f,.95f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};portraitMesh.RecalculateNormals();
            if(portrait!=null){var pm=new Material(Shader.Find("LordLyrical/Wave"));pm.SetTexture("_MainTex",portrait);pm.SetColor("_Color",Color.white);ResonantGeometry.MeshObject("Jeru source portrait hologram",root,portraitMesh,pm,new Vector3(0,1.3f,-.12f),Vector3.one);}
            for(int side=-1;side<=1;side+=2){var hand=ResonantGeometry.MeshObject("Spectrum hand",root,ResonantGeometry.Lathe(new[]{0f,.1f,.25f},new[]{.06f,.14f,.02f},8),material,new Vector3(side*.75f,1.4f,0),Vector3.one).transform;if(side<0)leftHand=hand;else rightHand=hand;}
            }
            compass=ResonantGeometry.Node("Twelve frequency compass",root,Vector3.up*1.15f);
            // The body aura is rendered by CinematicParticleDirector; no solid orbit rings.

        }
        public Vector3 BattleTarget {get;private set;}
        public Vector3 PalmNormal=>performance?performance.BattlePalmNormal:(BattleTarget-rightHand.position).normalized;
        public void ApplyBattle(float t,Vector3 heart,ConcertState state,Vector3 audience){
            BattleTarget=heart;performance?.ApplyBattle(t,heart,BattleScore.ShieldPoint(root.position),state.rescueCharge,audience);
        }
        public void RenderArrival(float songTime)
        {
            root.localPosition=OpeningTiming.JeruPosition(songTime);
            root.localRotation=Quaternion.identity;
            float settle=OpeningTiming.Smooth(-.8f,-.62f,songTime)*(1-OpeningTiming.Smooth(-.62f,-.1f,songTime));
            root.localPosition+=Vector3.up*settle*.06f;
            if(performance){performance.SampleArrival(songTime);performance.SetSpectrum(new Color(.3f,.65f,1),1.6f);}
            compass.gameObject.SetActive(false);
        }
        public void Render(ShowFrame frame)
        {
            float t=(float)frame.time;bool circle=t>=69&&t<90;
            root.localPosition=ConductorRoute.Position(t);
            root.localRotation=ConductorRoute.Rotation(t);
            float cycle=t>=81?Mathf.Floor(t*2)/12:t>=59&&t<61?t*.7f:t*.04f;
            material.SetColor("_Color",RootKeys.Spectrum(cycle));material.SetFloat("_Glow",t<21?.3f:t>=92?3:1);
            float raised=t>=92?Mathf.Clamp01((t-92)/4):t>=59&&t<61?1:t>=81&&t<83?1:0;
            if(performance!=null)
            {
                performance.SampleShow(t);
                performance.SetSpectrum(RootKeys.Spectrum(cycle),t<21?.3f:t>=92?3:1);
            }
            else
            {
                leftHand.localPosition=new Vector3(-.75f,1.4f+raised*.9f,-raised*.3f);rightHand.localPosition=new Vector3(.75f,1.4f+raised*.9f,-raised*.3f);
            }
            compass.localRotation=Quaternion.Euler(0,t*15,0);compass.gameObject.SetActive(t>=21&&t<111);

        }
    }
}
