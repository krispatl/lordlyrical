import bpy, math, json
from pathlib import Path
from mathutils import Matrix, Vector, Quaternion

WORK=Path(bpy.path.abspath('//'))/'Rebake'
WORK.mkdir(parents=True,exist_ok=True)
OUT=Path(bpy.path.abspath('//'))/'FBX_Rebaked'
OUT.mkdir(parents=True,exist_ok=True)
target=bpy.data.objects['Sonic_Orc_Rig']
scene=bpy.context.scene
scene.render.fps=30

# Anatomical vectors, not the arbitrary FBX display-bone tails, define correspondence.
mapping={
 'pelvis':('bip_hordePelvis',None),
 'spine_01':('bip_hordeSpine1','bip_hordeSpine2'),
 'spine_02':('bip_hordeSpine3','bip_hordeSpine4'),
 'spine_03':('bip_hordeSpine4','bip_hordeRibcage'),
 'neck_01':('bip_hordeNeck1','bip_hordeHead'),
 'head':('bip_hordeHead',None),
}
for side,S in [('l','L'),('r','R')]:
 for dst,src,end in [('clavicle','Collarbone','Upperarm'),('upperarm','Upperarm','Forearm'),('lowerarm','Forearm','Palm'),('hand','Palm','Digit21'),('thigh','Thigh','Calf'),('calf','Calf','Foot'),('foot','Foot','Digit11'),('ball','Digit11',None)]:
  mapping[dst+'_'+side]=('bip_horde'+S+src, 'bip_horde'+S+end if end else None)

def bake(label,filename,start,end,loop=False):
 # Source objects belong only to this new workshop; the original scenes remain untouched.
 for o in list(scene.objects):
  if o.get('motion_source'):bpy.data.objects.remove(o,do_unlink=True)
 old=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath=str(WORK/'vendor-orc'/filename),use_anim=True)
 imported=set(bpy.data.objects)-old
 source=next(o for o in imported if o.type=='ARMATURE')
 for o in imported:o['motion_source']=True
 sw=source.matrix_world.copy()
 swq=sw.to_quaternion()
 target.animation_data_clear()
 action=bpy.data.actions.new('Orc_Motion_'+label)
 target.animation_data_create();target.animation_data.action=action
 corrections={}
 for dst,(src,child) in mapping.items():
  tb=target.data.bones[dst];sb=source.data.bones[src]
  sr=(sw@sb.matrix_local).to_quaternion()
  tr=tb.matrix_local.to_quaternion()
  align=Quaternion()
  if child:
   source_direction=sw.to_3x3()@(source.data.bones[child].head_local-sb.head_local)
   target_direction=tb.tail_local-tb.head_local
   align=target_direction.rotation_difference(source_direction)
  corrections[dst]=(sr.inverted(),align,tr)
 base=sw@source.data.bones['bip_hordePelvis'].head_local
 leg_scale=1.30/.7149
 samples=[]
 for i,f in enumerate(range(start+1,end+2)):
  scene.frame_set(f)
  for pb in target.pose.bones:pb.matrix_basis=Matrix.Identity(4)
  bpy.context.view_layer.update()
  for dst,(src,child) in mapping.items():
   pb=target.pose.bones[dst];sr_inv,align,tr=corrections[dst]
   rot=(sw@source.pose.bones[src].matrix).to_quaternion()@sr_inv@align@tr
   rest=pb.bone.matrix_local
   position=(pb.parent.matrix@pb.parent.bone.matrix_local.inverted()@rest).translation if pb.parent else rest.translation
   if dst=='pelvis':
    hip=sw@source.pose.bones[src].matrix.translation
    # In-place navigation owns horizontal displacement; retain the source's weight shifts.
    position=rest.translation+Vector(((hip.x-base.x)*leg_scale,(hip.y-base.y)*leg_scale,(hip.z-base.z)*leg_scale))
   pb.matrix=Matrix.Translation(position)@rot.to_matrix().to_4x4()
   bpy.context.view_layer.update()
  # Preserve knee/ankle angles and lift the whole body only when a sole crosses the floor.
  if label not in ('Defeat',):
   lowest=min(target.pose.bones['foot_'+s].head.z-.19 for s in ('l','r'))
   pb=target.pose.bones['pelvis'];m=pb.matrix.copy();m.translation.z-=lowest;pb.matrix=m
   bpy.context.view_layer.update()
  for pb in target.pose.bones:
   if pb.bone.use_deform:
    pb.rotation_mode='QUATERNION'
    pb.keyframe_insert('location',frame=i)
    pb.keyframe_insert('rotation_quaternion',frame=i)
    pb.keyframe_insert('scale',frame=i)
  samples.append({'frame':i,'left':list(target.pose.bones['hand_l'].head),'right':list(target.pose.bones['hand_r'].head),'leftFoot':list(target.pose.bones['foot_l'].head),'rightFoot':list(target.pose.bones['foot_r'].head)})
 action.use_fake_user=True
 scene.frame_start=0;scene.frame_end=end-start
 scene.frame_set(0)
 for o in scene.objects:o.select_set(False)
 target.hide_set(False);target.select_set(True);bpy.context.view_layer.objects.active=target
 bpy.ops.export_scene.fbx(filepath=str(OUT/('Orc_'+label+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',armature_nodetype='NULL',path_mode='STRIP')
 (WORK/('Orc_'+label+'_samples.json')).write_text(json.dumps(samples))
 for o in imported:o.hide_set(True);o.hide_render=True
 for n in ['Sonic_Orc_LOD1','Sonic_Orc_LOD2']:bpy.data.objects[n].hide_set(True)
 return {'clip':label,'frames':len(samples),'seconds':(end-start)/30,'action':action.name}
