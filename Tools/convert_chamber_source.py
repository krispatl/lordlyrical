"""Bake a bounded, deterministic surfel derivative; original SPZ is preserved."""
import gzip,struct,numpy as np
from pathlib import Path
root=Path(__file__).resolve().parents[1]; b=gzip.open(root/'ArtSources/chamber.spz','rb').read(); _,v,n,degree,bits,flags,_=struct.unpack('<IIIBBBB',b[:16]);assert v==2 and degree==0
a=np.frombuffer(b,np.uint8,n*9,16).reshape(n,3,3).astype(np.int32); p=a[:,:,0]+(a[:,:,1]<<8)+(a[:,:,2]<<16); p=np.where(p>=8388608,p-16777216,p)/(2**bits);alpha=np.frombuffer(b,np.uint8,n,16+n*9)
c=np.frombuffer(b,np.uint8,n*3,16+n*10).reshape(n,3)
c=np.clip(((c/255-.5)/.15)*.28209479+.5,0,1)
s=np.exp(np.frombuffer(b,np.uint8,n*3,16+n*13).reshape(n,3)/16-10)
q=np.frombuffer(b,np.uint8,n*3,16+n*16).reshape(n,3)/127.5-1
q=np.c_[q,np.sqrt(np.maximum(0,1-(q*q).sum(1)))];q/=np.linalg.norm(q,axis=1)[:,None]
mask=(alpha>140)&(p[:,1]>-2.3)&(p[:,1]<17)&(np.abs(p[:,0])<15)&(p[:,2]>-30)&(p[:,2]<7)
ids=np.flatnonzero(mask);rng=np.random.default_rng(20261003)
weight=np.clip(np.prod(np.sort(s[ids],axis=1)[:,1:],axis=1),.00002,.015)**.35
ids=rng.choice(ids,min(110000,len(ids)),replace=False,p=weight/weight.sum())
# Orient surfel plane across the two largest covariance axes, including full quaternion rotation.
xyz=q[ids,:3];w=q[ids,3:];basis=np.eye(3)[np.argsort(s[ids],axis=1)[:,1:]]
def rotate(a):return a+2*np.cross(xyz,np.cross(xyz,a)+w*a)
a=rotate(basis[:,0]);d=rotate(basis[:,1]);size=np.sort(s[ids],axis=1)[:,1:]
size=np.clip(size*2.8,.035,.35);a*=size[:,0,None];d*=size[:,1,None]
vertices=np.stack([p[ids]-a-d,p[ids]+a-d,p[ids]-a+d,p[ids]+a+d],axis=1)
vertices[:,:,2]*=-1;vertices[:,:,1]+=1.65
colors=np.repeat(np.c_[c[ids],np.ones(len(ids))][:,None,:],4,axis=1)
out=root/'ArtSources/chamber-surfaces.bytes'
with out.open('wb') as f:
 f.write(struct.pack('<I',len(ids))); f.write(vertices.astype('<f4').tobytes());f.write((colors*255).astype('u1').tobytes())
print(dict(source_points=n,baked_surfels=len(ids),triangles=len(ids)*2,bytes=out.stat().st_size))

