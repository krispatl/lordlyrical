import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');OUT=ROOT/'outputs/LordLyrical_Finishing';PKG=ROOT/'outputs/LordLyricalFinishing_Unity';sc=bpy.context.scene
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
parts=[]
def mat(name,c,metal=.0,rough=.5,em=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*c,1);p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=em;return m
basalt=mat('Fork_Blackened_Steel',(.022,.029,.041),.78,.32);bronze=mat('Fork_Burnished_Bronze',(.32,.115,.026),.82,.3);edge=mat('Fork_Worn_Edge',(.39,.32,.21),.87,.25);leather=mat('Fork_Wrapped_Grip',(.024,.010,.007),.05,.8);energy=mat('Fork_Resonance',(.98,.20,.007),.1,.3,3)
stone=mat('Vortex_Obsidian',(.015,.019,.029),.45,.38);silver=mat('Vortex_Stone_Edges',(.075,.10,.14),.65,.4);blue=mat('Vortex_Cyan',(.025,.50,.98),.0,.3,2);violet=mat('Vortex_Violet',(.19,.055,.75),.0,.3,1.8)
def select(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o

def finish(o,name,m,bevel=0):
 o.name=name;o.data.materials.append(m);parts.append(o)
 if bevel:
  select(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);mod=o.modifiers.new('Machined edge bevel','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o

def box(name,loc,scale,m,bev=.012):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=scale;return finish(o,name,m,bev)
def cylinder(name,loc,r,d,m,segments=24,rotation=None):
 bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=r,depth=d,location=loc);o=bpy.context.object
 if rotation:o.rotation_euler=rotation
 return finish(o,name,m,.004)
def mesh(name,verts,faces,m):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);return finish(o,name,m)
def tube(name,points,r,m,n=8):
 vs=[];fs=[]
 for j,p in enumerate(points):
  direction=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized();q=direction.to_track_quat('Z','Y')
  for k in range(n):vs.append(Vector(p)+q@Vector((r*math.cos(k*math.tau/n),r*math.sin(k*math.tau/n),0)))
 for j in range(len(points)-1):
  for k in range(n):fs.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
 fs.extend([tuple(range(n-1,-1,-1)),tuple((len(points)-1)*n+k for k in range(n))]);return mesh(name,vs,fs,m)
def join(objs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();objs[0].name=name;return objs[0]
def export(name,objs):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0]
 bpy.ops.export_scene.fbx(filepath=str(PKG/'Models'/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,bake_anim=False,path_mode='STRIP')
 for o in objs:o.data.calc_loop_triangles()
 return [{'name':o.name,'triangles':len(o.data.loop_triangles)} for o in objs]
# Origin is the centre of the palm grip; the long axis exports as Unity +Y.
cylinder('Forged core shaft',(0,0,.38),.048,1.55,basalt)
cylinder('Grip leather body',(0,0,-.02),.067,.42,leather)
for j in range(16):
 z=-.22+j*.026;points=[(.07*math.cos(t*math.tau/24),.07*math.sin(t*math.tau/24),z+t*.022/24) for t in range(25)];tube('Helical grip lacing',points,.004,bronze,6)
for z in [-.26,.23,.52]:cylinder('Grip collar',(0,0,z),.089,.055,edge)
for z in [-.28,.25]:cylinder('Resonating collar',(0,0,z),.091,.012,energy)
box('Tuning fork yoke',(0,0,.72),(.60,.19,.20),basalt,.032)
box('Yoke bronze inlay',(0,-.101,.72),(.44,.012,.10),bronze,.009)
for s in [-1,1]:
 # Paired fork heads: outer long tines and inner offset harmonics.
 for x,z,w in [(s*.26,1.13,.14),(s*.13,1.055,.068)]:
  box('Resonator tine',(x,0,z),(w,.19 if w>.1 else .10,.76 if w>.1 else .59),basalt,.02)
  box('Resonator machined lip',(x,-.103 if w>.1 else -.058,z),(w*.72,.013,.63 if w>.1 else .48),edge,.005)
  box('Resonance channel',(x,-.114 if w>.1 else -.070,z),(w*.14,.011,.57 if w>.1 else .42),energy,.003)
  box('Tine cap',(x,0,z+(.37 if w>.1 else .29)),(w*1.12,.21 if w>.1 else .12,.065),bronze,.012)
  for j in range(5):box('Engraved tine notch',(x,-.121,z-.20+j*.095),(w*.46,.011,.012),basalt,.002)
 for z in [.65,.79]:cylinder('Yoke fastener',(s*.22,-.119,z),.028,.022,bronze,12,(math.pi/2,0,0))
 # Forked pommel counterweight.
 box('Pommel tine',(s*.10,0,-.39),(.075,.115,.28),bronze,.014)
box('Pommel bridge',(0,0,-.29),(.25,.13,.075),basalt,.016)
cylinder('Resonance jewel',(0,-.122,.72),.063,.025,energy,12,(math.pi/2,0,0))
for a in [i*math.tau/8 for i in range(8)]:tube('Cymatic yoke engraving',[(.085*math.cos(a),-.127,.72+.055*math.sin(a)),(.19*math.cos(a),-.127,.72+.055*math.sin(a))],.003,energy,5)
solids=[o for o in parts if o.data.materials[0]!=energy];energies=[o for o in parts if o.data.materials[0]==energy]
solid=join(solids,'TuningFork_LOD0');glow=join(energies,'TuningFork_Resonance');lods=[]
for i,ratio in [(1,.44),(2,.17)]:
 cp=solid.copy();cp.data=solid.data.copy();sc.collection.objects.link(cp);cp.name='TuningFork_LOD'+str(i);select(cp);d=cp.modifiers.new('Distance detail','DECIMATE');d.ratio=ratio;bpy.ops.object.modifier_apply(modifier=d.name);lods.append(cp)
records={'weapon':export('Double_Headed_Tuning_Fork',[solid,glow]+lods)}
for o in lods:o.hide_render=True;o.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Double_Headed_Tuning_Fork.blend'))
# Camera/lights for the weapon detail proof.
camd=bpy.data.cameras.new('Weapon camera');cam=bpy.data.objects.new('Weapon camera',camd);sc.collection.objects.link(cam);cam.location=(2.6,-4,1.7);cam.rotation_euler=(Vector((0,0,.55))-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=2.65;sc.camera=cam
world=bpy.data.worlds.new('Weapon studio');sc.world=world;world.use_nodes=True;bg=world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.035,.045,.065,1);bg.inputs[1].default_value=.45
for p,e,c in [((-2,-3,4),750,(1,.82,.62)),((2,-1,3),700,(.4,.7,1)),((0,2,2),1100,(1,.34,.08))]:
 ld=bpy.data.lights.new('Studio','AREA');lo=bpy.data.objects.new('Studio',ld);sc.collection.objects.link(lo);lo.location=p;lo.rotation_euler=(Vector((0,0,.6))-lo.location).to_track_quat('-Z','Y').to_euler();ld.energy=e;ld.color=c;ld.size=3
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.render.resolution_x=900;sc.render.resolution_y=1100;sc.render.resolution_percentage=100;sc.render.filepath=str(OUT/'Tuning_Fork_Blender.png');bpy.ops.render.render(write_still=True)
# Store the prop review setup as part of the source.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Double_Headed_Tuning_Fork.blend'))
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
parts=[]
# A hollow, camera-stationary corridor: scrolling highlights and moving shards imply travel.
for strand in range(8):
 points=[]
 for j in range(181):
  depth=-8+j*.40;angle=depth*.075+strand*math.tau/8;radius=5.6+.5*math.sin(depth*.08+strand)
  points.append((radius*math.cos(angle),-depth,2+radius*math.sin(angle)))
 tube('Vortex energy helix',points,.028,blue if strand%2 else violet,5)
for j in range(24):
 depth=j*3.0-5;radius=6.35
 for k in range(8):
  a=k*math.tau/8+j*.055;a2=a+.52;verts=[]
  for d in [depth-.13,depth+.13]:
   for r in [radius,radius+.35]:
    for ang in [a,a2]:verts.append((r*math.cos(ang),-d,2+r*math.sin(ang)))
  mesh('Carved obsidian ribs',verts,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],stone)
  if j%2==0:
   a3=(a+a2)/2;pos=Vector((6.27*math.cos(a3),-depth,2+6.27*math.sin(a3)));radial=Vector((math.cos(a3),0,math.sin(a3)));tangent=Vector((-math.sin(a3),0,math.cos(a3)))
   tube('Cymatic rib inset',[pos+tangent*.26,pos+radial*.04,pos-tangent*.26],.022,blue,5)
for m,name in [(stone,'Vortex_Obsidian_Ribs'),(blue,'Vortex_Cyan_Channels'),(violet,'Vortex_Violet_Channels')]:
 objs=[o for o in list(sc.objects) if o.type=='MESH' and o.data.materials[0]==m]
 if objs:join(objs,name)
vortex=[o for o in sc.objects if o.type=='MESH'];records['vortex']=export('Frequency_Vortex',vortex)
# Camera points down the authored corridor, matching Unity's stationary XR view.
camd=bpy.data.cameras.new('Arrival view');cam=bpy.data.objects.new('Arrival view',camd);sc.collection.objects.link(cam);cam.location=(0,3,2);cam.rotation_euler=(Vector((0,-40,2))-cam.location).to_track_quat('-Z','Y').to_euler();camd.lens=21;sc.camera=cam
sc.render.resolution_x=1500;sc.render.resolution_y=850;sc.world=world;bg.inputs[1].default_value=.16;sc.render.filepath=str(OUT/'Frequency_Vortex_Blender.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Frequency_Vortex.blend'));bpy.ops.render.render(write_still=True)
records['materials']=[]
for m in [basalt,bronze,edge,leather,energy,stone,silver,blue,violet]:
 p=m.node_tree.nodes.get('Principled BSDF');records['materials'].append(dict(name=m.name,color=list(p.inputs['Base Color'].default_value),metallic=p.inputs['Metallic'].default_value,roughness=p.inputs['Roughness'].default_value,emission=list(p.inputs['Emission Color'].default_value),strength=p.inputs['Emission Strength'].default_value))
(PKG/'art_manifest.json').write_text(json.dumps(records,indent=2));(OUT/'Art_Manifest.json').write_text(json.dumps(records,indent=2));print('FINISHING_GEOMETRY_READY',flush=True)
