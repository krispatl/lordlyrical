import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');OUT=ROOT/'outputs/LordLyricalFinishing_Unity/Models';records=[]
def select(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def reduce(src,level,ratio):
 cp=src.copy();cp.data=src.data.copy();cp.name=src.name+'_LOD'+str(level);bpy.context.scene.collection.objects.link(cp)
 for mod in list(cp.modifiers):cp.modifiers.remove(mod)
 shapes=src.data.shape_keys
 if shapes:
  cp.shape_key_clear()
 select(cp);dec=cp.modifiers.new('Distance silhouette reduction','DECIMATE');dec.ratio=ratio;bpy.ops.object.modifier_apply(modifier=dec.name)
 bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False)
 if shapes:
  # Project onto source triangles and transfer every facial delta barycentrically.
  src.data.calc_loop_triangles();triangles=[tuple(tr.vertices) for tr in src.data.loop_triangles];base=[v.co.copy() for v in shapes.key_blocks[0].data];tree=BVHTree.FromPolygons(base,triangles,all_triangles=True);maps=[]
  for v in cp.data.vertices:
   point,normal,index,distance=tree.find_nearest(v.co);ids=triangles[index];a,b,c=[base[i] for i in ids];v0=b-a;v1=c-a;v2=point-a;d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);den=d00*d11-d01*d01
   if abs(den)<1e-15:w=(1,0,0)
   else:
    wb=(d11*v2.dot(v0)-d01*v2.dot(v1))/den;wc=(d00*v2.dot(v1)-d01*v2.dot(v0))/den;w=(1-wb-wc,wb,wc)
   maps.append((ids,w))
  cp.shape_key_add(name='Basis')
  for key in list(shapes.key_blocks)[1:]:
   dst=cp.shape_key_add(name=key.name)
   for i,(ids,w) in enumerate(maps):
    delta=sum(((key.data[j].co-base[j])*weight for j,weight in zip(ids,w)),Vector());dst.data[i].co=cp.data.vertices[i].co+delta
 parent=src.find_armature();mod=cp.modifiers.new('Bound original skeleton','ARMATURE');mod.object=parent;cp.parent=parent
 cp.data.calc_loop_triangles();records.append(dict(mesh=cp.name,triangles=len(cp.data.loop_triangles),blendshapes=len(cp.data.shape_keys.key_blocks)-1 if cp.data.shape_keys else 0));return cp
def export(path,rig,meshes):
 bpy.ops.object.select_all(action='DESELECT')
 for o in [rig]+meshes:o.hide_set(False);o.hide_render=False;o.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,armature_nodetype='NULL',bake_anim=False,path_mode='STRIP')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'outputs/Jeru_Likeness/LordLyrical_Jeru_Working.blend'),load_ui=False)
scene=bpy.data.scenes['Lord Lyrical | Unity export'];bpy.context.window.scene=scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.animation_data_clear()
for b in rig.pose.bones:b.location=(0,0,0);b.rotation_euler=(0,0,0);b.scale=(1,1,1)
original=[o for o in scene.objects if o.type=='MESH'];meshes=[]
for o in original:
 if o.data.shape_keys:
  o.data.shape_keys.animation_data_clear()
  for k in o.data.shape_keys.key_blocks:k.value=0
for level,ratio in [(1,.47),(2,.20)]:
 for o in original:meshes.append(reduce(o,level,ratio))
export(OUT/'Jeru_Distance_Meshes.fbx',rig,meshes)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'outputs/LordLyrical_Finishing/Jeru_Distance_Rigs.blend'))
for path in sorted((ROOT/'outputs/Participant_Avatars').glob('*_manifest.json')):
 role=json.loads(path.read_text());bpy.ops.wm.open_mainfile(filepath=str(path.parent/(role['id']+'.blend')),load_ui=False);scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.animation_data_clear()
 for b in rig.pose.bones:b.location=(0,0,0);b.rotation_euler=(0,0,0);b.scale=(1,1,1)
 originals=[bpy.data.objects[n] for n in ['Avatar_LeftHand','Avatar_RightHand']];meshes=[]
 for level,ratio in [(1,.28),(2,.13)]:
  for o in originals:meshes.append(reduce(o,level,ratio))
 export(OUT/(role['id']+'_Distance_Hands.fbx'),rig,meshes)
(ROOT/'outputs/LordLyrical_Finishing/Distance_Mesh_Manifest.json').write_text(json.dumps(records,indent=2));print('DISTANCE_MESHES_READY',flush=True)
