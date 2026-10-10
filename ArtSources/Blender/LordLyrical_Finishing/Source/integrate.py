from pathlib import Path
import shutil
root=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\als\outputs\LordLyrical')
out=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs')
backup=out/'LordLyrical_Finishing/Integration_Backup'
def edit(path,pairs):
 p=root/path;b=backup/path;b.parent.mkdir(parents=True,exist_ok=True)
 if not b.exists():shutil.copy2(p,b)
 s=p.read_text(encoding='utf-8-sig')
 for old,new in pairs:
  if old not in s:raise ValueError('Missing edit anchor '+path+' '+old[:80])
  s=s.replace(old,new,1)
 p.write_text(s,encoding='utf-8')
edit('Assets/LordLyrical/Scripts/Core/ExperienceConfiguration.cs',[(
'public GameObject chamberModel;',
'public GameObject chamberModel;\n        public GameObject finishingModel,orcWeapon;\n        public VoiceCueLibrary voiceCues;')])
edit('Assets/LordLyrical/Scripts/Environment/ResonantWorld.cs',[
('ChamberPresentation chamberArt;','ChamberPresentation chamberArt;ShowArtPresentation finishing;'),
('new OrcEncounterController(chamber,config.orcModel)','new OrcEncounterController(chamber,config.orcModel,config.orcWeapon)'),
('effects=new VFXDirector(transform,JsonUtility.FromJson<CueTable>(config.cueJson.text).cues);','if(config.finishingModel){finishing=Instantiate(config.finishingModel,transform).GetComponent<ShowArtPresentation>();finishing.Initialize(roles,orcs,lord,config.vfxQuality);}\n            else effects=new VFXDirector(transform,JsonUtility.FromJson<CueTable>(config.cueJson.text).cues);'),
('public void ResetEffects(){if(built)effects.Reset();}','public void ResetEffects(){if(built){effects?.Reset();if(finishing)finishing.Reset();}}\n        public Vector3 VoicePosition(int slot)=>slot<0?lord.root.position+Vector3.up*1.9f:roles[Mathf.Clamp(slot,0,9)].root.position+Vector3.up*1.6f;'),
('vortex.gameObject.SetActive(frame.vortex);','vortex.gameObject.SetActive(frame.vortex&&finishing==null);'),
('effects.Render(frame,chamberArt!=null?chamberArt.Mouth:dragon.Mouth,chamberArt!=null?chamberArt.Heart:dragon.Heart,lord.root.position);','if(finishing){finishing.quality=config.vfxQuality;finishing.Render(frame,chamberArt!=null?chamberArt.Mouth:dragon.Mouth,chamberArt!=null?chamberArt.Heart:dragon.Heart);}\n            else effects.Render(frame,chamberArt!=null?chamberArt.Mouth:dragon.Mouth,chamberArt!=null?chamberArt.Heart:dragon.Heart,lord.root.position);'),
('title.gameObject.SetActive(frame.title);','title.gameObject.SetActive(frame.title&&finishing==null);'),
('glyph.gameObject.SetActive(t>=34&&t<39);','glyph.gameObject.SetActive(t>=34&&t<39&&finishing==null);')])
edit('Assets/LordLyrical/Scripts/Characters/RoleVisual.cs',[(
'        public void Render(ShowFrame frame,bool occupied,bool local)',
'''        public Vector3 Hand(bool left=false)
        {
            if(NetworkRoleAvatar.TryGetHand(slot,left,out var tracked))return tracked;
            if(authored&&authored.animator){var hand=authored.animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);if(hand)return hand.position;}
            return root.position+new Vector3(left?-.4f:.4f,1.4f,.25f);
        }
        public void Render(ShowFrame frame,bool occupied,bool local)''')])
edit('Assets/LordLyrical/Scripts/Participants/NetworkRoleAvatar.cs',[(
'        void OnEnable()',
'''        public static bool TryGetHand(int slot,bool left,out Vector3 position)
        {
            foreach(var item in Instances)if(item&&item.lastSlot==slot&&item.avatar&&(item.avatar.IsLocalAvatar||item.UsesTrackedBody)){
                var hand=left?item.avatar._leftHand:item.avatar._rightHand;if(hand){position=hand.position;return true;}
            }
            position=default;return false;
        }
        void OnEnable()''')])
edit('Assets/LordLyrical/Scripts/Characters/OrcEncounterController.cs',[
('public OrcEncounterController(Transform parent,GameObject model)','public Vector3 Target(int index)=>orcs[Mathf.Clamp(index,0,11)].position+Vector3.up*1.65f;\n        public OrcEncounterController(Transform parent,GameObject model,GameObject weapon=null)'),
('                ResonantGeometry.MeshObject("Fork",','                if(weapon){var prop=Object.Instantiate(weapon,clubs[i]);prop.transform.localPosition=Vector3.zero;prop.transform.localRotation=Quaternion.identity;}\n                else {\n                ResonantGeometry.MeshObject("Fork",'),
('                if(performances[i]&&performances[i].leftWeaponSocket)','                }\n                if(performances[i]&&performances[i].leftWeaponSocket)'),
('clubs[i].localScale=Vector3.one*.55f;','clubs[i].localScale=Vector3.one*(weapon?1f:.55f);')])
edit('Assets/LordLyricalJeru_Unity/Scripts/JeruPerformance.cs',[
('if(time>=59 && time<61)','if(time>=34 && time<39){index=3;local=(time-34)*.8f;}\n            else if(time>=87 && time<92){index=4;local=(time-87)*.6f;}\n            else if(time>=59 && time<61)'),
('else if(time>=92 && time<99){index=2;local=(time-92)*4/7;}','else if(time>=92 && time<101){index=2;local=(time-92)*1.0f;}'),
('float t=Mathf.Repeat(Mathf.Max(0,seconds),Mathf.Max(.01f,length));','float t=index==0||index==1?Mathf.Repeat(Mathf.Max(0,seconds),Mathf.Max(.01f,length)):Mathf.Clamp(seconds,0,Mathf.Max(0,length-.001f));')])
# The dragon's original .blend, prefab, meshes, clips, and materials remain untouched.
edit('Assets/LordLyricalChamber_Unity/Scripts/ChamberPresentation.cs',[
('for(int i=0;i<dragonRenderers.Length;i++)dragonRenderers[i].sharedMaterials=dragonInstances[i];','''var finishShader=Shader.Find("LordLyrical/Resonant Actor Surface");
                if(finishShader)foreach(var ms in dragonInstances)foreach(var m in ms){m.shader=finishShader;m.SetColor("_EdgeColor",new Color(.05f,.55f,2));}
                for(int i=0;i<dragonRenderers.Length;i++)dragonRenderers[i].sharedMaterials=dragonInstances[i];'''),
('dragon.localScale=Vector3.one*dragonScale*Mathf.Max(.001f,1-dissolve);','dragon.localScale=Vector3.one*dragonScale;'),
('            Pose(clip,time);','''            Pose(clip,time);
            float hit=Smooth(90,90.25f,t)*(1-Smooth(91.3f,92,t));
            float recoil=hit*Mathf.Sin((t-90)*28);
            if(head!=null){float track=t>=69&&t<81?Mathf.Sin((t-69)*.55f)*8:0;head.localRotation*=Quaternion.Euler(recoil*5,track+recoil*3,0);}
            if(chest!=null)chest.localRotation*=Quaternion.Euler(recoil*2,0,recoil*3);
            foreach(var bone in poseNodes)if(bone.name.StartsWith("tail"))bone.localRotation*=Quaternion.Euler(0,hit*Mathf.Sin(t*16+bone.GetSiblingIndex())*3,0);'''),
('                if(m.HasProperty("_BaseColor"))','                if(m.HasProperty("_Dissolve"))m.SetFloat("_Dissolve",dissolve);\n                if(m.HasProperty("_Fracture"))m.SetFloat("_Fracture",Smooth(90,91,t)*(1-dissolve));\n                if(m.HasProperty("_BaseColor"))')])
for f in (out/'LordLyricalFinishing_Unity').rglob('*'):
 if f.is_file():
  dest=root/'Assets/LordLyricalFinishing_Unity'/f.relative_to(out/'LordLyricalFinishing_Unity');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest)
print('FINISHING_INTEGRATION_SOURCE_READY')
