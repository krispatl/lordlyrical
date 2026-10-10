"""Original procedural VFX maps: ray-integrated evolving 3D density, not stock images."""
from pathlib import Path
import numpy as np
from scipy.ndimage import map_coordinates,gaussian_filter
from PIL import Image
import json
ROOT=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\als\outputs\LordLyrical\Assets\LordLyricalVFX_Unity\Textures')
ROOT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(44107)
noise=rng.random((64,64,64),dtype=np.float32)
noise=gaussian_filter(noise,.7,mode='wrap');noise=(noise-noise.mean())/noise.std()
n=128;steps=40
y,x,z=np.meshgrid(np.linspace(-1,1,n,dtype=np.float32),np.linspace(-1,1,n,dtype=np.float32),np.linspace(-1,1,steps,dtype=np.float32),indexing='ij')
sheet=np.zeros((1024,1024,4),dtype=np.uint8)
for f in range(64):
 t=f/63;curl=np.sin(y*3+z*2+t*2)*.12
 xx=x+curl;yy=y+.10*np.sin(z*4+t*2);zz=z
 density=np.zeros_like(x)
 for frequency,weight in [(7,.60),(15,.27),(31,.13)]:
  coords=np.array([(xx+1)*frequency+t*5,(yy+1)*frequency-t*3,(zz+1)*frequency+t*4])
  density+=map_coordinates(noise,coords,order=1,mode='wrap')*weight
 radius=np.sqrt((xx*.96)**2+(yy*1.08)**2+zz**2)
 envelope=np.clip((.90-radius)*4,0,1)
 d=np.maximum(0,density*.42+.30)*envelope
 optical=np.cumsum(d,axis=2)*.13
 trans=np.exp(-optical)
 light=np.exp(-np.cumsum(d[:,::-1,:],axis=1)[:,::-1,:]*.09)
 scatter=(d*trans*(.18+.82*light)).sum(axis=2)*.13
 alpha=1-np.exp(-d.sum(axis=2)*.16)
 luminance=np.clip(scatter/np.maximum(alpha,.001),0,1)
 rgba=np.dstack([luminance*.83,luminance*.91,luminance,alpha])
 # Unity's first tile is the upper left; adjacent frames interpolate in the shader.
 row,col=divmod(f,8);sheet[row*n:(row+1)*n,col*n:(col+1)*n]=np.uint8(np.clip(rgba,0,1)*255)
Image.fromarray(sheet).save(ROOT/'Volumetric_Plume_8x8.png')
N=512;flow=np.zeros((N,N,4),np.float32)
from scipy.ndimage import zoom
for c in range(4):
 for size,w in [(8,.50),(16,.27),(32,.14),(64,.065),(128,.025)]:
  grid=rng.random((size,size),dtype=np.float32);grid=np.pad(grid,((0,1),(0,1)),mode='wrap')
  a,b=np.meshgrid(np.arange(N)*size/N,np.arange(N)*size/N,indexing='ij')
  flow[:,:,c]+=map_coordinates(grid,[a,b],order=3,mode='wrap')*w
 flow[:,:,c]=np.clip((flow[:,:,c]-.15)*1.4,0,1)
Image.fromarray(np.uint8(flow*255)).save(ROOT/'Energy_Flow_RGBA.png')
y,x=np.mgrid[-1:1:128j,-1:1:128j];r=np.sqrt(x*x+y*y)
for name,alpha in [('Spark_Core',np.exp(-r*r*18)*(1-np.clip(r,0,1))**2),('Optical_Flare',np.exp(-r*6)*.55+np.exp(-x*x*4-y*y*380)*.18+np.exp(-y*y*4-x*x*380)*.12)]:
 alpha=np.clip(alpha,0,1);rgba=np.dstack([np.ones_like(r)]*3+[alpha]);Image.fromarray(np.uint8(rgba*255)).save(ROOT/(name+'.png'))
manifest={'source':'Original procedural volume integration and seeded multiscale noise','plumeFrames':64,'atlasResolution':1024,'frameResolution':128,'raySteps':40,'seed':44107,'maps':[p.name for p in ROOT.glob('*.png')]}
(ROOT/'Texture_Manifest.json').write_text(json.dumps(manifest,indent=2));print('VFX_TEXTURES_READY',flush=True)
