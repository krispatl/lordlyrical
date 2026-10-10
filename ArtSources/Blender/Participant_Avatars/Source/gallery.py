import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');out=root/'outputs'/'Participant_Avatars';sc=bpy.context.scene
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
sc.name='Ten Root Keys | Participant gallery'
files=sorted(out.glob('*_manifest.json'),key=lambda p:json.loads(p.read_text())['slot'])
for path in files:
 record=json.loads(path.read_text());slot=record['slot'];id=record['id']
 with bpy.data.libraries.load(str(out/(id+'.blend')),link=False) as (src,dst):
  dst.objects=[n for n in src.objects if n=='Avatar_Rig' or n.startswith('Avatar_Body_') or n in ['Avatar_Cymatic','Avatar_LeftHand','Avatar_RightHand']]
  dst.actions=[n for n in src.actions if n.startswith(id+'_')]
 col=bpy.data.collections.new(record['key']+' | '+record['name']);sc.collection.children.link(col)
 for obj in dst.objects:
  if obj:col.objects.link(obj)
 rig=next(o for o in dst.objects if o and o.type=='ARMATURE');rig.name=id+'_Rig';rig.location=((slot%5-2)*2.1,(slot//5)*3.2,0)
 rig.animation_data.action=bpy.data.actions[id+'_Idle']
 for a in dst.actions:
  if a:a.use_fake_user=True
 for o in dst.objects:
  if o:
   o.hide_set('_LOD1' in o.name or '_LOD2' in o.name);o.hide_render=('_LOD1' in o.name or '_LOD2' in o.name)
sc.frame_set(22);sc.render.fps=30;sc.frame_end=121
world=bpy.data.worlds.new('Gallery world');sc.world=world;world.use_nodes=True;bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.05,.065,.09,1);bg.inputs[1].default_value=.7
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,1.5,-.02));floor=bpy.context.object;floor.name='Gallery ground';mat=bpy.data.materials.new('Gallery charcoal');mat.use_nodes=True;bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.024,.03,.042,1);bs.inputs['Roughness'].default_value=.76;floor.data.materials.append(mat)
for position,power,color,size in [((-4,-5,8),1800,(1,.92,.82),7),((5,-3,5),1100,(.56,.74,1),6),((0,6,7),2400,(.55,.72,1),6)]:
 ld=bpy.data.lights.new('Gallery light','AREA');lo=bpy.data.objects.new('Gallery light',ld);sc.collection.objects.link(lo);lo.location=position;lo.rotation_euler=(Vector((0,1.5,1))-lo.location).to_track_quat('-Z','Y').to_euler();ld.energy=power;ld.color=color;ld.shape='DISK';ld.size=size
camd=bpy.data.cameras.new('Gallery Camera');cam=bpy.data.objects.new('Gallery Camera',camd);sc.collection.objects.link(cam);cam.location=(5,-12,7);cam.rotation_euler=(Vector((0,1.5,.7))-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=12;sc.camera=cam
sc.render.engine='CYCLES';sc.cycles.samples=24;sc.render.resolution_x=1800;sc.render.resolution_y=1050;sc.render.resolution_percentage=100
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'LordLyrical_Ten_Avatars.blend'))
sc.render.filepath=str(out/'All_Ten_Blender_Gallery.png');bpy.ops.render.render(write_still=True)
print('ALL_TEN_GALLERY_READY',flush=True)
