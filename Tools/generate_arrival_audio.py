from pathlib import Path
import numpy as np, wave, json
root=Path(__file__).resolve().parents[1]
out=root/'Assets/Resources/LordLyricalAccents'
out.mkdir(parents=True,exist_ok=True)
sr=48000
rng=np.random.default_rng(183941)
report={}
def noise(n,low,high):
    x=rng.standard_normal(n)
    f=np.fft.rfftfreq(n,1/sr)
    w=np.clip((f-low)/max(10,low*.3),0,1)*np.clip((high-f)/max(30,high*.35),0,1)
    y=np.fft.irfft(np.fft.rfft(x)*w,n)
    return y/max(np.std(y),1e-9)
def save(name,x):
    x=x-np.mean(x)
    edge=min(240,len(x)//10)
    x[:edge]*=np.linspace(0,1,edge);x[-edge:]*=np.linspace(1,0,edge)
    x=x/max(np.max(np.abs(x)),1e-9)*.78
    with wave.open(str(out/(name+'.wav')),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((x*32767).astype('<i2').tobytes())
    report[name]={'seconds':len(x)/sr,'peak':float(np.max(np.abs(x))),'rms':float(np.sqrt(np.mean(x*x))),'clippedSamples':int(np.sum(np.abs(x)>=1))}
t=np.arange(int(sr*1.2))/sr
freq=130+1500*(t/1.2)**2
phase=np.cumsum(freq)/sr*2*np.pi
x=(noise(len(t),150,4500)*.16+np.sin(phase)*.16+np.sin(phase*1.97)*.07)*(t/1.2)**1.7
x*=np.minimum(1,(1.2-t)/.06)
save('LightningDescent',x)
t=np.arange(int(sr*1.4))/sr
snap=noise(len(t),500,8500)*np.exp(-t*32)
body=noise(len(t),45,950)*np.exp(-t*4.5)
phase=2*np.pi*(80*t+65*(1-np.exp(-t*7))/7)
x=snap*.30+body*.29+np.sin(phase)*np.exp(-t*5)*.25
x+=noise(len(t),1500,6500)*np.exp(-np.maximum(0,t-.075)*30)*(t>=.075)*.09
save('LightningLanding',x)
t=np.arange(int(sr*3.2))/sr
roll=noise(len(t),30,600)
env=(1-np.exp(-t*18))*np.exp(-t*1.25)*(1+.17*np.sin(t*11))
x=roll*env+noise(len(t),300,1600)*env*.15
save('ArrivalThunder',x)
(out/'ArrivalAudio_Provenance.json').write_text(json.dumps({'source':'Original procedural synthesis for Lord Lyrical; no external samples','generator':'Tools/generate_arrival_audio.py','seed':183941,'clips':report},indent=2))
print(json.dumps(report))
