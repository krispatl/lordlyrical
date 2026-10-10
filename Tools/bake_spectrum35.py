from pathlib import Path
import numpy as np, subprocess, imageio_ffmpeg, hashlib, json, struct
P=Path(__file__).resolve().parents[1]
source=P/'Assets/LordLyrical/Audio/LordLyrical.mp3'
raw=subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(source),'-f','f32le','-ac','1','-ar','22050','pipe:1'])
x=np.frombuffer(raw,dtype='<f4'); rate=30; bands=24; n=2048
edges=np.geomspace(40,10000,bands+1); freq=np.fft.rfftfreq(n,1/22050)
frames=int(np.ceil(len(x)/22050*rate))+1; out=np.zeros((frames,bands),dtype=np.float32)
window=np.hanning(n); pad=np.pad(x,(n//2,n//2))
for i in range(frames):
 start=min(round(i*22050/rate),len(x)); segment=pad[start:start+n]
 if len(segment)<n:segment=np.pad(segment,(0,n-len(segment)))
 spec=np.abs(np.fft.rfft(segment*window))/window.sum()
 for b in range(bands):
  values=spec[(freq>=edges[b])&(freq<edges[b+1])]
  out[i,b]=np.sqrt(np.mean(values**2)) if len(values) else 0
# Per-band 40 dB range, anchored to the track's 98th percentile. Silence stays zero.
peak=np.maximum(np.percentile(out,98,axis=0),1e-6)
out=np.clip((20*np.log10(np.maximum(out,1e-9)/peak)+40)/40,0,1)
for i in range(1,frames):
 decay=np.where(out[i]>out[i-1],.78,.24);out[i]=out[i-1]+decay*(out[i]-out[i-1])
out[0]*=0;out[-1]*=0
dest=P/'Assets/LordLyricalCinema_Unity/Resources/Cinema26/Spectrum35.bytes'
dest.write_bytes(struct.pack('<4i',3501,rate,bands,frames)+np.round(out*65535).astype('<u2').tobytes())
report={'trackSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'duration':len(x)/22050,'frames':frames,'sampleRate':rate,'bands':bands,'frequencyEdgesHz':edges.tolist(),'bytes':dest.stat().st_size,'method':'2048-point Hann FFT, RMS logarithmic bands, 40 dB range, offline attack/release envelope'}
(P/'Tools/Spectrum35_Bake.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
