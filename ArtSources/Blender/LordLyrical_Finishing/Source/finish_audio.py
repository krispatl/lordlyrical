from pathlib import Path
import numpy as np,wave,json
from PIL import Image
root=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');out=root/'outputs/LordLyricalFinishing_Unity';audio=out/'Resources/LordLyricalAccents';audio.mkdir(parents=True,exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
rate=44100;rng=np.random.default_rng(1700);manifest=[]
for name,duration,base,decay,noise in [('RitualFootfall',.7,67,8,.15),('TuningForkImpact',1.5,146.83,3.8,.14),('GlassResonance',1.7,329.63,3,.1),('WingPressure',1.4,54,2,.7),('DragonHarmonicGrowl',2.8,41.2,1.1,.3),('WordChorus',2.4,164.81,1.7,.025)]:
 t=np.arange(round(duration*rate))/rate;n=rng.uniform(-1,1,len(t));n=np.convolve(n,np.ones(23)/23,'same');tone=np.zeros(len(t))
 for k,amp in [(1,.65),(2,.2),(3.017,.11),(5.12,.06)]:tone+=np.sin(2*np.pi*base*k*t+(.8 if name=='DragonHarmonicGrowl' else .1)*np.sin(t*19))*amp
 env=np.minimum(t*80,1)*np.exp(-decay*t)*np.minimum((duration-t)*12,1)
 if name=='WingPressure':env=np.sin(np.pi*t/duration)**2;tone*=.25
 if name=='WordChorus':tone=sum(np.sin(2*np.pi*base*r*t) for r in [1,1.25,1.5,2])/4
 signal=(tone+noise*n*4)*env;signal*=.7/max(.01,abs(signal).max())
 with wave.open(str(audio/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((signal*32767).astype('<i2').tobytes())
 manifest.append(dict(name=name,duration=duration,source='Original deterministic synthesis; no sampled voice or recording',peak=.7))
# Tileable detail normals for cloth, forged metal, skin pores. These are new procedural textures.
n=512;y,x=np.mgrid[0:n,0:n]/n
for name,height,strength in [('Woven_Brocade',np.sin(x*np.pi*128)*np.sin(y*np.pi*128)+.28*np.sin((x+y)*np.pi*32),.6),('Forged_Microdetail',np.sin(x*np.pi*52+np.sin(y*np.pi*36))*.35+np.sin(y*np.pi*166)*.09,.45),('Skin_Pores',np.sin(x*np.pi*190)*np.sin(y*np.pi*186)*.17,.22)]:
 dx=(np.roll(height,-1,1)-np.roll(height,1,1))*strength;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*strength;v=np.stack([-dx,-dy,np.ones_like(dx)],-1);v/=np.linalg.norm(v,axis=-1,keepdims=True);Image.fromarray(np.uint8(np.clip(v*.5+.5,0,1)*255),'RGB').save(tex/(name+'_Normal.png'))
(root/'outputs/LordLyrical_Finishing/Audio_Manifest.json').write_text(json.dumps(manifest,indent=2));print('Audio and microdetail textures ready')
