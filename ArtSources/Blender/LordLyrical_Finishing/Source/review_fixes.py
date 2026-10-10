from pathlib import Path
import shutil
base=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');root=base.parent/'als/outputs/LordLyrical';pkg=base/'outputs/LordLyricalFinishing_Unity'
def edit(p,old,new):
 s=p.read_text(encoding='utf-8-sig');assert old in s,(p,old);p.write_text(s.replace(old,new),encoding='utf-8')
p=root/'Assets/LordLyrical/Scripts/Characters/OrcEncounterController.cs'
edit(p,'Mathf.Clamp(t-26,0,21)*.27f','Mathf.Clamp(t-26,0,21)*.14f')
edit(p,'float y=dropped?-Mathf.Clamp01((t-45)*2)*1.2f:','float y=dropped?Mathf.SmoothStep(0,.75f,Mathf.Clamp01((t-45)/1.2f)):')
edit(p,'Quaternion.Euler(dropped?85:','Quaternion.Euler(dropped?85*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-45)/1.2f)):')
edit(p,'label.characterSize=.3f','label.characterSize=.045f')
p=root/'Assets/LordLyrical/Scripts/Environment/ResonantWorld.cs';edit(p,'call.characterSize=.17f','call.characterSize=.035f')
p=root/'Assets/LordLyrical/Scripts/Editor/SonicOrcShowValidation.cs';edit(p,'r.sharedMaterial.shader.name=="LordLyrical/Sonic Orc Surface"','r.sharedMaterial.shader.name=="LordLyrical/Resonant Actor Surface"')
p=root/'Assets/LordLyricalOrc_Unity/Scripts/SonicOrcPerformance.cs'
edit(p,'Renderer[] renderers;','Renderer[] renderers;Transform[] gripFingers;')
edit(p,'            graph.Evaluate(0);','''            graph.Evaluate(0);
            if(gripFingers==null){var nodes=GetComponentsInChildren<Transform>(true);var fingers=new System.Collections.Generic.List<Transform>();foreach(var n in nodes)if(n.name.EndsWith("_l")&&(n.name.StartsWith("index_")||n.name.StartsWith("middle_")||n.name.StartsWith("ring_")||n.name.StartsWith("pinky_")))fingers.Add(n);gripFingers=fingers.ToArray();}
            foreach(var finger in gripFingers)finger.rotation=Quaternion.AngleAxis(28,transform.right)*finger.rotation;''')
p=pkg/'Scripts/ShowArtPresentation.cs';edit(p,'new Vector3(0,.07f,11.3f)','new Vector3(0,.07f,14f)')
p=pkg/'Editor/FinishingContactReview.cs';edit(p,'16-(time-26)*.27f','16-(time-26)*.14f');edit(p,'world.Render(ShowFrame.At(94),true);camera.transform.position','world.Render(ShowFrame.At(94),true);foreach(var text in world.GetComponentsInChildren<TextMesh>())text.gameObject.SetActive(false);camera.transform.position')
# A Unity ScriptableObject must have a matching script filename for editable assets.
p=pkg/'Scripts/VoiceCueLibrary.cs';s=p.read_text();start=s.index('    [CreateAssetMenu(menuName="Lord Lyrical/Facial cue track")]');facial=s[start:s.rfind('\n}')];p.write_text(s[:start]+'}\n');(pkg/'Scripts/FacialCueTrack.cs').write_text('using System;\nusing UnityEngine;\nnamespace LordLyrical\n{\n'+facial+'\n}\n')
for folder in ['Scripts','Editor','Shaders']:
 for f in (pkg/folder).glob('*'):
  if f.is_file():shutil.copy2(f,root/'Assets/LordLyricalFinishing_Unity'/folder/f.name)
print('CONTACT_AND_PRESENTATION_FIXES_READY')
