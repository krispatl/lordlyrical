import bpy,bmesh,math,json,sys,os,shutil
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
from bl_ext.user_default.mpfb.services.humanservice import HumanService
from bl_ext.user_default.mpfb.services.targetservice import TargetService
from bl_ext.user_default.mpfb.services.assetservice import AssetService
ROOT=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');OUT=ROOT/'outputs'/'Participant_Avatars';PKG=ROOT/'outputs'/'LordLyricalAvatars_Unity'
DATA=Path(r'C:\Users\kris\AppData\Roaming\Blender Foundation\Blender\5.2\extensions\.user\user_default\mpfb\data')
ROSTER=[
 dict(id='D_Warrior',name='The Warrior',key='D',family='Stone Choir',hex='E88A20',height=1.48,width=1.46,gender=.85,age=.62,muscle=.88,weight=.70,skin='middleage_african_male',design='Granite lamellar armor, broad amber gauntlets, split beard and low crested helm',ability='Amplitude Strike'),
 dict(id='C_Foundation',name='The Foundation',key='C',family='Stone Choir',hex='AF7F24',height=1.42,width=1.61,gender=.45,age=.67,muscle=.90,weight=.85,skin='middleage_asian_female',design='Heavy hexagonal mantle, shield shoulders, dark gold plated apron',ability='Amplitude Strike'),
 dict(id='E_Blade',name='The Blade',key='E',family='High Elves',hex='168DFF',height=1.96,width=.87,gender=.30,age=.32,muscle=.48,weight=.40,skin='young_african_female',design='Swept ears and crest, narrow blue tabard, swept blade forearms',ability='Resonance Shatter / Chord Barrage'),
 dict(id='G_Balanced',name='The Balanced',key='G',family='High Elves',hex='A0D5BC',height=1.90,width=.95,gender=.65,age=.40,muscle=.44,weight=.43,skin='young_asian_male',design='Symmetrical crescent shoulders, jade-silver surcoat and twin circular seals',ability='Chord Barrage'),
 dict(id='F_Healer',name='The Healer',key='F',family='High Elves',hex='FFDC82',height=1.84,width=.96,gender=.25,age=.50,muscle=.35,weight=.48,skin='middleage_african_female',design='Ivory-gold layered robe, open sun halo, winged collar and bracers',ability='Resonant Field'),
 dict(id='B_Shadow',name='The Shadow',key='B',family='Pitch / avian',hex='6754DE',height=1.82,width=.90,gender=.55,age=.45,muscle=.50,weight=.39,skin='young_african_male',design='Raven mask, folded feather mantle and long indigo flight feathers',ability='Infrasound Dread'),
 dict(id='Cs_Seeker',name='The Seeker',key='C#',family='Prowl / feline',hex='A3F5FF',height=1.69,width=.91,gender=.3,age=.38,muscle=.54,weight=.41,skin='young_asian_female',design='Feline face guard and ears, articulated tail, short asymmetric scout sash',ability='Sonar'),
 dict(id='Fs_Architect',name='The Architect',key='F#',family='Founders',hex='DBEDFF',height=2.03,width=1.02,gender=.55,age=.52,muscle=.45,weight=.47,skin='middleage_african_male',design='Crystalline shoulder spires, geometric back frame, layered white-blue mantle',ability='Floor Rebuild'),
 dict(id='Bb_Discordant',name='The Discordant',key='Bb',family='Howlers',hex='F02F62',height=1.88,width=1.18,gender=.75,age=.47,muscle=.80,weight=.60,skin='middleage_african_male',design='Curved horns, howler muzzle, asymmetric serrated crimson mantle',ability='Cacophony / Anti-Frequency'),
 dict(id='A_Ancient',name='The Ancient',key='A',family='Founders',hex='FFBA48',height=1.97,width=1.10,gender=.70,age=.92,muscle=.38,weight=.58,skin='old_african_male',design='Weathered gold vestments, nested resonance halo and long segmented beard',ability='Harmonic Support')]
def srgb(h):
 return tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
def material(name,color,metal=0,rough=.5,emission=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1);bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if emission:bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=emission
 return m
def select(obj):
 for o in bpy.context.selected_objects:o.select_set(False)
 obj.hide_set(False);obj.select_set(True);bpy.context.view_layer.objects.active=obj
def make_mesh(name,verts,faces,mat,bone=None):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 bm=bmesh.new();bm.from_mesh(me)
 if all(len(e.link_faces)==2 for e in bm.edges):bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 else:
  bm.normal_update()
  for f in bm.faces:
   c=f.calc_center_median()
   if f.normal.dot(Vector((c.x,c.y,0)))<0:f.normal_flip()
 bm.to_mesh(me);bm.free();me.update()
 o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);me.materials.append(mat)
 if bone:
  g=o.vertex_groups.new(name=bone);g.add(list(range(len(verts))),1,'REPLACE')
 parts.append(o);return o
def skin(o):
 o.parent=rig;mod=o.modifiers.new('Skeleton','ARMATURE');mod.object=rig
def transfer(o):
 for vg in body.vertex_groups:o.vertex_groups.new(name=vg.name)
 for v in o.data.vertices:
  co,n,idx,dist=bvh.find_nearest(v.co);vs=body.data.polygons[idx].vertices;nearest=sorted(vs,key=lambda i:(body.data.vertices[i].co-v.co).length)[:3];ws=[1/max(.001,(body.data.vertices[i].co-v.co).length)**2 for i in nearest];total=sum(ws);values={}
  for i,w in zip(nearest,ws):
   for g in body.data.vertices[i].groups:values[g.group]=values.get(g.group,0)+g.weight*w/total
  top=sorted(values.items(),key=lambda p:-p[1])[:4];total=sum(w for _,w in top)
  for gi,w in top:
   if w>1e-6:o.vertex_groups[gi].add([v.index],w/total,'REPLACE')
def ribbon(name,points,width,mat,bone=None):
 vs=[]
 for i,p in enumerate(points):
  tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);across=tangent.cross(Vector((0,-1,0))).normalized()*width/2
  vs.extend([Vector(p)-across,Vector(p)+across])
 ob=make_mesh(name,vs,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)],mat,bone)
 if not bone:transfer(ob)
 return ob
def tube(name,points,radii,mat,bone,segments=6):
 vs=[];fs=[]
 for j,p in enumerate(points):
  tangent=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized();q=tangent.to_track_quat('Z','Y')
  for k in range(segments):vs.append(Vector(p)+q@Vector((radii[j]*math.cos(k*math.tau/segments),radii[j]*math.sin(k*math.tau/segments),0)))
 for j in range(len(points)-1):
  for k in range(segments):fs.append((j*segments+k,j*segments+(k+1)%segments,(j+1)*segments+(k+1)%segments,(j+1)*segments+k))
 fs.append(tuple(range(segments-1,-1,-1)));fs.append(tuple((len(points)-1)*segments+k for k in range(segments)))
 return make_mesh(name,vs,fs,mat,bone)
def plate(name,outline,depth,mat,bone):
 pts=[Vector(p) for p in outline];center=sum(pts,Vector())/len(pts);center.y-=depth
 vs=pts+[center]+[p+Vector((0,depth*.25,0)) for p in pts];n=len(pts);fs=[(i,(i+1)%n,n) for i in range(n)]+[(i,n+1+i,n+1+(i+1)%n,(i+1)%n) for i in range(n)]+[tuple(range(n+1,2*n+1))]
 return make_mesh(name,vs,fs,mat,bone)
def ring(name,center,rx,rz,mat,bone,start=0,end=math.tau,r=.004):
 n=max(12,round((end-start)/math.tau*48));points=[(center[0]+rx*math.cos(start+(end-start)*i/n),center[1],center[2]+rz*math.sin(start+(end-start)*i/n)) for i in range(n+1)]
 return tube(name,points,[r]*(n+1),mat,bone,4)
def patch(name,predicate,offset,mat):
 cp=body.copy();cp.data=body.data.copy();cp.name=name;sc.collection.objects.link(cp)
 for m in list(cp.modifiers):cp.modifiers.remove(m)
 bm=bmesh.new();bm.from_mesh(cp.data);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[f for f in bm.faces if not predicate(f.calc_center_median())],context='FACES')
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.normal_update()
 for v in bm.verts:v.co+=v.normal*offset
 bm.to_mesh(cp.data);bm.free();cp.data.materials.clear();cp.data.materials.append(mat);cp.material_slots[0].link='DATA';cp.material_slots[0].material=mat
 for p in cp.data.polygons:p.material_index=0
 parts.append(cp);return cp
def cloth_panels(name,bottom,spread,mat,count=8):
 waist=h*.54;top=h*.61
 for k in range(count):
  a0=math.tau*k/count+.09;a1=math.tau*(k+1)/count-.09;vs=[]
  for j in range(9):
   t=j/8;z=top+(bottom-top)*t;radx=w*.19+(spread-w*.19)*t; rady=.13*h+(.19*h-.13*h)*t
   for i in range(6):
    a=a0+(a1-a0)*i/5;fold=1+.04*math.sin(i/5*math.pi*4)*t
    vs.append((radx*math.cos(a)*fold,rady*math.sin(a)*fold,z+(.018*math.cos(i/5*math.pi*2) if j==8 else 0)))
  fs=[(j*6+i,j*6+i+1,(j+1)*6+i+1,(j+1)*6+i) for j in range(8) for i in range(5)]
  ob=make_mesh(name+' panel',vs,fs,mat);transfer(ob)
  for edge in [0,5]:ribbon(name+' border',[vs[j*6+edge] for j in range(9)],.012,trim)
def surface_trace(points,bone,width=.004):
 hits=[]
 for x,z in points:
  q,n,idx,dist=bvh.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
  if q is not None:hits.append(q+Vector((0,-.030,0)))
 if len(hits)>1:tube('Cymatic channel',hits,[width]*len(hits),glow,bone,4)
def bake_body(raw,name):
 cp=raw.copy();deps=bpy.context.evaluated_depsgraph_get();cp.data=bpy.data.meshes.new_from_object(raw.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps);cp.name=name;sc.collection.objects.link(cp)
 for mod in list(cp.modifiers):cp.modifiers.remove(mod)
 for vg in list(cp.vertex_groups):
  if vg.name not in rig.data.bones:cp.vertex_groups.remove(vg)
 return cp
def animate():
 def reset():
  for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0)
 def turn(name,axis,angle):
  pb=rig.pose.bones.get(name)
  if pb:pb.rotation_quaternion=pb.rotation_quaternion@Quaternion(pb.bone.matrix_local.to_3x3().inverted()@Vector(axis),math.radians(angle))
 def pose(clip,t):
  reset();turn('spine_02',(1,0,0),math.sin(t*math.tau/3)*1.0);turn('head',(0,0,1),math.sin(t*math.tau/3)*2)
  for s,side in [(1,'l'),(-1,'r')]:
   turn('upperarm_'+side,(0,1,0),s*27);turn('lowerarm_'+side,(1,0,0),18)
  if clip=='Walk':
   u=math.sin(t*math.tau/1.2)
   for s,side in [(1,'l'),(-1,'r')]:turn('thigh_'+side,(1,0,0),s*u*15);turn('calf_'+side,(1,0,0),max(0,-s*u)*24);turn('foot_'+side,(1,0,0),-s*u*11-max(0,-s*u)*16);turn('upperarm_'+side,(1,0,0),-s*u*10)
  if clip=='Ability':
   q=math.sin(math.pi*min(t/3,1))**.7
   for s,side in [(1,'l'),(-1,'r')]:
    if slot<2:turn('upperarm_'+side,(1,0,0),-68*q*(1 if side=='r' else .65));turn('lowerarm_'+side,(1,0,0),-35*q);turn('spine_03',(0,0,1),s*3*q)
    elif slot in [2,3]:turn('upperarm_'+side,(1,0,0),-75*q*(1 if side=='r' else .8));turn('lowerarm_'+side,(1,0,0),-20*q);turn('hand_'+side,(0,1,0),s*20*q)
    elif slot==4:turn('upperarm_'+side,(0,1,0),-s*67*q);turn('lowerarm_'+side,(1,0,0),-25*q)
    elif slot==5:turn('upperarm_'+side,(0,1,0),-s*82*q);turn('spine_02',(1,0,0),6*q)
    elif slot==6:turn('upperarm_'+side,(1,0,0),-72*q);turn('lowerarm_'+side,(1,0,0),-30*q);turn('head',(0,0,1),18*q*math.sin(t*2))
    elif slot==7:turn('upperarm_'+side,(1,0,0),(-102 if side=='r' else -45)*q);turn('lowerarm_'+side,(1,0,0),-12*q)
    elif slot==8:turn('upperarm_'+side,(1,0,0),-80*q);turn('upperarm_'+side,(0,1,0),-s*24*q);turn('spine_01',(1,0,0),8*q)
    else:turn('upperarm_'+side,(0,1,0),-s*60*q);turn('lowerarm_'+side,(1,0,0),-50*q)
  if clip=='Stagger':
   q=math.sin(min(math.pi,t*8))*math.exp(-t*2);turn('spine_01',(1,0,0),-22*q);turn('head',(1,0,0),-16*q)
  if clip=='Rise':
   q=min(1,t/3);q=q*q*(3-2*q)
   for s,side in [(1,'l'),(-1,'r')]:turn('upperarm_'+side,(0,1,0),-s*112*q);turn('upperarm_'+side,(1,0,0),-14*q);turn('lowerarm_'+side,(1,0,0),-22*q)
  if clip=='Hands':
   q=.5-.5*math.cos(t*math.tau/2)
   for s,side in [(1,'l'),(-1,'r')]:
    for f in ['index','middle','ring','pinky']:
     for j in [1,2,3]:turn(f+'_%02d_'%j+side,(1,0,0),45*q)
  for bone in ['tail_01','tail_02','tail_03']:turn(bone,(0,0,1),math.sin(t*2+len(bone))*(5 if clip!='Ability' else 12))
 rig.animation_data_create();records=[]
 for clip,duration,loop in [('Idle',3,True),('Walk',1.2,True),('Ability',3,False),('Stagger',1,False),('Rise',4,False),('Hands',2,True)]:
  name=role['id']+'_'+clip;act=bpy.data.actions.new(name);act.use_fake_user=True;rig.animation_data.action=act
  for f in range(round(duration*30)+1):
   pose(clip,0 if loop and f==round(duration*30) else f/30)
   for pb in rig.pose.bones:
    if pb.bone.use_deform:pb.keyframe_insert('rotation_quaternion',frame=f+1,group=pb.name)
  records.append(dict(name=name,seconds=duration,loop=loop))
 rig.animation_data.action=None;reset();return records

slot=int(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else 0;role=ROSTER[slot]
sc=bpy.context.scene;sc.name=role['id']+' | Rigged character'
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
macro=TargetService.get_default_macro_info_dict();macro.update({k:role[k] for k in ['gender','age','muscle','weight']});macro['race']={'african':.5,'asian':.3,'caucasian':.2}
raw=HumanService.create_human(macro_detail_dict=macro);raw.name='Anatomical source '+role['id'];rig=HumanService.add_builtin_rig(raw,'game_engine');rig.name='Avatar_Rig'
HumanService.set_character_skin(str(DATA/'skins'/role['skin']/(role['skin']+'.mhmat')),raw,skin_type='MAKESKIN')
eye=HumanService.add_mhclo_asset(AssetService.find_asset_absolute_path('low-poly.mhclo',asset_subdir='eyes'),raw,asset_type='Eyes',subdiv_levels=0)
bpy.context.view_layer.update();body=bake_body(raw,'Anatomical body');eyes=bake_body(eye,'Eyes');bpy.data.objects.remove(eye,do_unlink=True)
raw.hide_set(True);raw.hide_render=True
h=role['height'];w=role['width'];baseheight=max(v.co.z for v in body.data.vertices);scalez=h/baseheight
M=Matrix.Diagonal((scalez*w,scalez*(1.18 if slot<2 else 1),scalez,1))
for o in [body,eyes]:o.data.transform(M);o.parent=rig;o.data.update()
select(rig);bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:b.head=M@b.head;b.tail=M@b.tail
if slot==6:
 prev='pelvis';points=[(0,.13,h*.51),(0,.40,h*.47),(0,.67,h*.53),(0,.84,h*.67)]
 for i in range(3):b=rig.data.edit_bones.new('tail_%02d'%(i+1));b.head=points[i];b.tail=points[i+1];b.parent=rig.data.edit_bones[prev];prev=b.name
bpy.ops.object.mode_set(mode='OBJECT')
rig.data.bones['Root'].use_deform=True
key=srgb(role['hex']);clothcolor=tuple(.012+c*.075 for c in key);metalcolor=tuple(.13+c*.21 for c in key)
if slot in [4,7]:clothcolor=(.38,.42,.40) if slot==4 else (.25,.32,.40);metalcolor=(.58,.56,.42) if slot==4 else (.54,.64,.7)
cloth=material(role['id']+'_Vestment',clothcolor,.12,.72);armor=material(role['id']+'_Armor',metalcolor,.65,.34);trim=material(role['id']+'_Trim',tuple(.28+c*.32 for c in key),.75,.30);glow=material(role['id']+'_Cymatic',key,.25,.32,2);hair=material(role['id']+'_Hair',(.025,.022,.02),.05,.87)
skinmat=body.data.materials[0];skinmat.name=role['id']+'_Skin'
body.data.materials.clear();body.data.materials.append(skinmat)
body.data.materials.append(cloth);body.data.materials.append(armor);body.data.materials.append(hair)
for i,m in enumerate([skinmat,cloth,armor,hair]):body.material_slots[i].link='DATA';body.material_slots[i].material=m
for p in body.data.polygons:
 z=p.center.z
 p.material_index=0 if z>h*.84 else (2 if z<h*.14 else 1);p.use_smooth=True
 if z>h*.964:p.material_index=3
parts=[body,eyes]
bvh=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
# Garment layers are extracted from the fitted anatomy, retaining its original skin weights.
patch('Fitted cuirass',lambda p:h*.55<p.z<h*.79 and abs(p.x)<h*(.17 if slot<2 else .12),.022,armor)
patch('Boot shells',lambda p:p.z<h*.155,.014,armor)
patch('Forearm shells',lambda p:abs(p.x)>h*.19 and h*.59<p.z<h*.72,.015,armor)
# Remove anatomy under the enclosed toe boxes, avoiding skin protrusions through footwear.
for ob in [body]+[p for p in parts if p.name.startswith('Boot shells')]:
 bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().z<h*.065],context='FACES');bm.to_mesh(ob.data);bm.free()
bvh=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
bottom={0:.31,1:.25,2:.30,3:.23,4:.07,5:.34,6:.36,7:.12,8:.30,9:.055}[slot]*h
cloth_panels('Split vestment',bottom,h*(.22 if slot in [4,7,9] else .17),cloth,8 if slot not in [2,6] else 4)
neck=rig.data.bones['neck_01'].head_local.z;head=rig.data.bones['head'];hz=head.head_local.z
# Plated shoulder silhouettes, sized independently of skin geometry.
for s,side in [(1,'l'),(-1,'r')]:
 shoulder=rig.data.bones['upperarm_'+side].head_local;ex=.14*h if slot<2 else .09*h
 if slot==8 and side=='r':ex*=1.6
 outline=[(shoulder.x-s*.065*h,-.05*h,shoulder.z+.025*h),(shoulder.x+s*ex*.45,-.03*h,shoulder.z+.065*h),(shoulder.x+s*ex,-.05*h,shoulder.z-.005*h),(shoulder.x+s*ex*.65,-.09*h,shoulder.z-.09*h),(shoulder.x-s*.035*h,-.11*h,shoulder.z-.06*h)]
 plate('Shoulder plate',outline,.025*h,armor,'clavicle_'+side)
 ribbon('Shoulder seam',[outline[0],outline[1],outline[2]],.008,glow,'clavicle_'+side)
 # Layered knee guards, fully enclosed boot toe boxes and articulated waist plates.
 knee=rig.data.bones['calf_'+side].head_local
 plate('Knee shield',[(knee.x-s*.05*h,knee.y-.065*h,knee.z+.045*h),(knee.x+s*.05*h,knee.y-.065*h,knee.z+.045*h),(knee.x+s*.055*h,knee.y-.075*h,knee.z-.02*h),(knee.x,knee.y-.075*h,knee.z-.075*h),(knee.x-s*.055*h,knee.y-.075*h,knee.z-.02*h)],.012,armor,'calf_'+side)
 foot=rig.data.bones['foot_'+side].head_local;x=foot.x;back=foot.y+.055*h;front=foot.y-.16*h;fw=.058*h
 verts=[(x-fw,front,.003),(x+fw,front,.003),(x+fw,back,.003),(x-fw,back,.003),(x-fw*.75,front+.025*h,.075*h),(x+fw*.75,front+.025*h,.075*h),(x+fw*.7,back,.12*h),(x-fw*.7,back,.12*h)]
 make_mesh('Armored boot',verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],armor,'foot_'+side)
 tube('Boot welt',[(x-fw,front,.025),(x+fw,front,.025),(x+fw,back,.025),(x-fw,back,.025),(x-fw,front,.025)],[.006]*5,trim,'foot_'+side,4)
 for j in range(3):
  z=h*(.605-j*.035);xx=s*h*(.115+j*.006)
  plate('Overlapping waist scale',[(xx-s*.042*h,-.123*h,z+.02*h),(xx+s*.043*h,-.113*h,z+.02*h),(xx+s*.035*h,-.135*h,z-.027*h),(xx,-.145*h,z-.04*h),(xx-s*.036*h,-.143*h,z-.02*h)],.008,armor,'pelvis')
 # A slim knuckle and wrist channel stays visible in first-person view.
 hand=rig.data.bones['hand_'+side];hp=hand.head_local
 tube('Hand sigil '+side,[hp+Vector((0,.011,0)),hand.tail_local+Vector((0,.011,0))],[.008,.004],glow,'hand_'+side,5)
 # Crown horns are omitted on elves; ears use pointed sculpted fins.
 if slot in [2,3,4]:
  half=max(abs(v.co.x) for v in body.data.vertices if v.co.z>h*.90)
  plate('Swept elven ear',[(s*half*.86,-.01,h*.925),(s*(half+.060*h),.005,h*.96),(s*half*.90,-.008,h*.899)],.012,skinmat,'head')
 if slot<2:
  for j in range(3):
   x=s*(.015+j*.014)*h
   tube('Stone beard braid',[(x,-.083*h,h*.892),(x*1.2,-.107*h,h*(.82-j*.009)),(x*.80,-.095*h,h*(.79+j*.008))],[.012*h,.013*h,.004*h],hair,'head',6)
 if slot==0:plate('Low warrior crest',[(s*.02*h,0,h*.98),(s*.04*h,.02,h*1.07),(s*.055*h,.06,h*.96)],.014,armor,'head')
 if slot==1:plate('Foundation crest',[(s*.02*h,-.07*h,h*.955),(s*.115*h,-.02,h*1.035),(s*.12*h,.08,h*.93)],.025,trim,'head')
 if slot==2:plate('Blade crown',[(s*.023*h,-.062*h,h*.95),(s*.062*h,0,h*1.10),(s*.067*h,.06,h*.96)],.014,trim,'head')
 if slot==3:ring('Balanced crescent',(s*.18*h,.05*h,neck),.105*h,.11*h,trim,'clavicle_'+side,start=.05,end=math.pi*1.65,r=.01)
 if slot==5:
  for j in range(6):
   x0=s*(.10+.022*j)*h;z0=h*(.81-.022*j);x1=s*(.27+.018*j)*h;z1=h*(.38+.021*j)
   plate('Folded raven flight feather',[(x0,.10*h,z0),(s*(.28+.025*j)*h,.115*h,z0+.025*h),(x1,.20*h,z1),(s*(.15+.019*j)*h,.10*h,z0-.025*h)],.018,armor if j%2 else cloth,'spine_03')
 if slot==6:
  plate('Feline ear',[(s*.027*h,0,h*.965),(s*.057*h,.025,h*1.085),(s*.075*h,.045,h*.952)],.015,armor,'head')
  plate('Feline ear inner',[(s*.042*h,-.007,h*.98),(s*.056*h,.002,h*1.06),(s*.062*h,.006,h*.979)],.008,glow,'head')
 if slot==7:
  for j in range(3):
   x=s*(.14+j*.045)*h;z=neck+(.08-j*.016)*h
   plate('Crystal shoulder vane',[(x,-.04*h,z-.08*h),(x+s*.035*h,-.03*h,z+.13*h),(x+s*.055*h,.03*h,z-.10*h)],.02,trim,'clavicle_'+side)
 if slot==8:
  tube('Howler curved horn',[(s*.068*h,.012,h*.955),(s*.16*h,.04,h*1.05),(s*.19*h,.08,h*1.18),(s*.13*h,.10,h*1.24)],[.045*h,.037*h,.022*h,.002],armor,'head',7)
 if slot==9:
  for j in range(3):tube('Ancient beard braid',[(s*(.015+j*.018)*h,-.063*h,h*.87),(s*(.02+j*.026)*h,-.07*h,h*.77),(s*.026*h,-.075*h,h*.68)],[.014*h,.018*h,.003],trim,'head',6)
if slot in [4,9]:
 for j in range(1 if slot==4 else 3):ring('Resonance halo',(0,.11*h,h*.93),h*(.18+j*.028),h*(.18+j*.028),trim if j else glow,'head',start=.12,end=math.tau-.12,r=.008 if j else .005)
if slot==7:
 for rx in [.20,.28]:
  pts=[(0,.12*h,neck+.20*h),(rx*h,.12*h,neck),(0,.12*h,neck-.23*h),(-rx*h,.12*h,neck),(0,.12*h,neck+.20*h)]
  tube('Founder geometric frame',pts,[.012]*5,trim,'spine_03',4)
if slot in [5,6,8]:
 # A fitted animal face guard is authored over the human anatomy.
 front=-.16*h if slot==5 else -.105*h
 plate('Feral face guard',[(-.061*h,-.050*h,h*.945),(.061*h,-.050*h,h*.945),(.070*h,-.072*h,h*.875),(0,front,h*(.851 if slot!=5 else .83)),(-.070*h,-.072*h,h*.875)],.03*h,armor,'head')
 for s in [-1,1]:tube('Feral eye slit',[(s*.022*h,-.084*h,h*.926),(s*.058*h,-.072*h,h*.934)],[.005,.006],glow,'head',4)
if slot==6:
 points=[(0,.13,h*.51),(0,.40,h*.47),(0,.67,h*.53),(0,.84,h*.67)]
 for j in range(3):tube('Feline tail',points[j:j+2],[.036*(1-j*.22),.036*(1-(j+1)*.22)],cloth,'tail_%02d'%(j+1),8)
# Role seal, drawn on the fitted torso rather than floating in front of it.
zc=h*.755
for radius in [.033*h,.057*h]:surface_trace([(radius*math.cos(i*math.tau/32),zc+radius*math.sin(i*math.tau/32)) for i in range(33)],'spine_03',.003)
for j in range(3+(slot%4)):
 ang=j*math.tau/(3+slot%4);surface_trace([(r*math.cos(ang),zc+r*math.sin(ang)) for r in [.02*h,.062*h]],'spine_03',.003)
# Separate articulated first-person hands without changing their skeleton.
hands=[]
for side in ['l','r']:
 names={n for n in body.vertex_groups.keys() if n=='hand_'+side or(n.endswith('_'+side) and any(n.startswith(f+'_') for f in ['index','middle','ring','pinky','thumb']))};indices={body.vertex_groups[n].index for n in names}
 verts={v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in indices)>.6}
 cp=body.copy();cp.data=body.data.copy();cp.name='Avatar_'+('Left' if side=='l' else 'Right')+'Hand';sc.collection.objects.link(cp)
 bm=bmesh.new();bm.from_mesh(cp.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in verts],context='VERTS');bm.to_mesh(cp.data);bm.free()
 # Independent tracked hands cannot retain influences from the hidden forearm.
 for group in list(cp.vertex_groups):
  if group.name not in names:cp.vertex_groups.remove(group)
 bm=bmesh.new();bm.from_mesh(cp.data);boundary=[e for e in bm.edges if e.is_boundary]
 hp=rig.data.bones['hand_'+side].head_local;direction=(rig.data.bones['hand_'+side].tail_local-hp).normalized()
 for v in {v for e in boundary for v in e.verts}:v.co-=direction*(v.co-hp).dot(direction)
 if boundary:bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(cp.data);bm.free()
 cp.data.materials.clear();cp.data.materials.append(cloth);cp.material_slots[0].link='DATA';cp.material_slots[0].material=cloth
 for p in cp.data.polygons:p.material_index=0
 hands.append(cp)
 bm=bmesh.new();bm.from_mesh(body.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[f for f in bm.faces if all(v.index in verts for v in f.verts)],context='FACES');bm.to_mesh(body.data);bm.free()
# Merge by visibility: body and optional hand surfaces; energy remains independently animatable.
energy=[o for o in parts if o.data.materials and o.data.materials[0]==glow];solids=[o for o in parts if o not in energy]
def join(objects,name):
 for o in bpy.context.selected_objects:o.select_set(False)
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=objects[0];o.name=name;return o
body=join(solids,'Avatar_Body_LOD0');energyObj=join(energy,'Avatar_Cymatic');body.parent=rig;energyObj.parent=rig
for o in [body,energyObj]+hands:
 for mod in list(o.modifiers):o.modifiers.remove(mod)
 select(o);bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False)
 if o==body:
  o.data.calc_loop_triangles();dec=o.modifiers.new('Game topology','DECIMATE');dec.ratio=min(1,17000/len(o.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=dec.name)
 bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False)
 skin(o)
lods=[]
for level,target in [(1,7000),(2,3000)]:
 cp=body.copy();cp.data=body.data.copy();cp.name='Avatar_Body_LOD'+str(level);sc.collection.objects.link(cp)
 for mod in list(cp.modifiers):cp.modifiers.remove(mod)
 select(cp);cp.data.calc_loop_triangles();dec=cp.modifiers.new('Distance topology','DECIMATE');dec.ratio=min(1,target/len(cp.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=dec.name);bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False);skin(cp);lods.append(cp)
clips=animate();sc.render.fps=30;sc.frame_start=1;sc.frame_end=91
objects=[body,energyObj]+hands+lods;records=[];materials=[];seen=set()
for o in objects:
 o.data.calc_loop_triangles();unweighted=sum(not v.groups for v in o.data.vertices)
 if unweighted:raise RuntimeError(o.name+' has '+str(unweighted)+' unweighted vertices')
 records.append(dict(name=o.name,triangles=len(o.data.loop_triangles),vertices=len(o.data.vertices),maxInfluences=max(len(v.groups) for v in o.data.vertices)))
 for m in o.data.materials:
  if m.name in seen:continue
  seen.add(m.name);bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if bs is None:continue
  tex=next((n for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.colorspace_settings.name=='sRGB'),None);fname=''
  if tex:
   im=tex.image;fname=(role['skin'] if m==skinmat else 'Shared_Eyes')+'.png';dest=PKG/'Textures'/fname
   if True:
    source=Path(bpy.path.abspath(im.filepath))
    if source.is_file() and source.suffix.lower()=='.png':shutil.copy2(source,dest)
    else:
     _=im.pixels[0];im.filepath_raw=str(dest);im.file_format='PNG';im.save()
  materials.append(dict(name=m.name,color=list(bs.inputs['Base Color'].default_value),metallic=bs.inputs['Metallic'].default_value,roughness=bs.inputs['Roughness'].default_value,emission=list(bs.inputs['Emission Color'].default_value),strength=bs.inputs['Emission Strength'].default_value,texture=fname))
for o in bpy.context.selected_objects:o.select_set(False)
for o in [rig]+objects:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig;rig.animation_data.action=bpy.data.actions[role['id']+'_Idle'];sc.frame_set(1)
bpy.ops.export_scene.fbx(filepath=str(PKG/'Models'/(role['id']+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,add_leaf_bones=False,use_armature_deform_only=True,armature_nodetype='NULL',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=.15,path_mode='STRIP')
for o in lods:o.hide_set(True);o.hide_render=True
report=dict(role,slot=slot,bones=len(rig.data.bones),meshes=records,materials=materials,clips=clips)
(PKG/(role['id']+'_manifest.json')).write_text(json.dumps(report,indent=2));(OUT/(role['id']+'_manifest.json')).write_text(json.dumps(report,indent=2))
for im in bpy.data.images:
 if im.has_data:
  try:im.pack()
  except:pass
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(role['id']+'.blend')))
# An independently rendered proof for each exported source.
for o in bpy.context.selected_objects:o.select_set(False)
camd=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',camd);sc.collection.objects.link(cam);sc.camera=cam;cam.location=(h*1.3,-h*3,h*.82);cam.rotation_euler=(Vector((0,0,h*.53))-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=h*1.5
world=bpy.data.worlds.new('Studio');sc.world=world;world.use_nodes=True;bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.045,.052,.065,1);bg.inputs[1].default_value=.5
for pos,power,col in [((-3,-4,5),1000,(1,.86,.68)),((3,-2,3),800,(.55,.74,1)),((0,3,4),1300,(.35,.65,1))]:
 ld=bpy.data.lights.new('Studio','AREA');lo=bpy.data.objects.new('Studio',ld);sc.collection.objects.link(lo);lo.location=pos;lo.rotation_euler=(Vector((0,0,h*.6))-lo.location).to_track_quat('-Z','Y').to_euler();ld.energy=power;ld.color=col;ld.shape='DISK';ld.size=3
try:sc.render.engine='CYCLES'
except TypeError as e:raise RuntimeError(str(e))
sc.cycles.samples=16;sc.render.resolution_x=720;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/(role['id']+'_Blender.png'));bpy.ops.render.render(write_still=True)
print('AVATAR_COMPLETE',json.dumps({k:report[k] for k in ['id','slot','bones','meshes']}),flush=True)
