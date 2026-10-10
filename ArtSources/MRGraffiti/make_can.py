import bpy, math
from pathlib import Path
W=Path(__file__).parent;A=W/'Assets/MRGraffiti';(A/'Models').mkdir(parents=True,exist_ok=True);(A/'Blender').mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metal=0,rough=.3):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
silver=mat('Brushed aluminium',(.55,.62,.68),.9,.23);black=mat('Graphite label',(.018,.025,.04),.4,.3);cyan=mat('Colour indicator',(.015,.65,.88),.5,.22);white=mat('Ivory lettering',(.85,.91,.94),.25,.3);rubber=mat('Nozzle polymer',(.035,.045,.065),.15,.4)
objects=[]
def cylinder(name,r,depth,z,m,vertices=64):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=(0,0,z));o=bpy.context.object;o.name=name;o.data.materials.append(m);bevel=o.modifiers.new('Machined edges','BEVEL');bevel.width=.0008;bevel.segments=2;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');objects.append(o);return o
cylinder('Can body',.032,.17,.025,black)
for z in [-.062,.111]:cylinder('Rolled aluminium rim',.0335,.005,z,silver)
cylinder('Shoulder',.031,.009,.114,silver);cylinder('Valve cup',.023,.006,.12,silver);cylinder('ColourBand',.0322,.019,-.043,cyan)
for z in [-.029,.098]:cylinder('Fine foil rule',.0323,.001,z,silver)
for i,(radius,height) in enumerate([(.009,.016),(.011,.018),(.014,.02)]):
 o=cylinder('Cap'+str(i),radius,height,.13,rubber,32)
 bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.0023+i*.0006,depth=.006,location=(0,-radius,.13));n=bpy.context.object;n.name='Aerosol jet '+str(i);n.rotation_euler=(math.pi/2,0,0);n.data.materials.append(silver);n.parent=o;n.matrix_parent_inverse=o.matrix_world.inverted();objects.append(n)
def text(name,words,size,z):
 curve=bpy.data.curves.new(name,'FONT');curve.body=words;curve.align_x='CENTER';curve.size=size;curve.resolution_u=2;curve.extrude=0;curve.bevel_depth=0;o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);o.location=(0,-.0325,z);o.rotation_euler=(math.pi/2,0,0);o.data.materials.append(white);objects.append(o)
text('Brand','CHROMA',.011,.068);text('Brand second','/ WALL',.010,.054);text('Label descriptor','SURFACE SYSTEM',.0032,.032);text('Volume','400 / CYBER SERIES',.0028,.017)
text('Label rule','---------------',.003,.007);text('Pigment line','PIGMENT / 01',.003,-.006)
for original in list(objects):
 if original.type=='FONT':
  rear=original.copy();rear.data=original.data.copy();rear.name=original.name+' rear';bpy.context.collection.objects.link(rear);rear.location.y=.0325;rear.rotation_euler=(math.pi/2,0,math.pi);objects.append(rear)
# Export only this deliberately created can; no existing Blender scene is touched.
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
label_names={o.name for o in objects if o.type=='FONT'}
for o in list(objects):
 if o.type=='FONT':bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
for o in bpy.context.selected_objects:
 if o.name in label_names:
  inverse=o.matrix_world.inverted();side=-1 if o.location.y<0 else 1
  for v in o.data.vertices:
   p=o.matrix_world@v.co;angle=p.x/.03265;p.x=.03265*math.sin(angle);p.y=side*.03265*math.cos(angle);v.co=inverse@p
bpy.ops.wm.save_as_mainfile(filepath=str(A/'Blender/ChromaWallCan.blend'))
bpy.ops.export_scene.fbx(filepath=str(A/'Models/ChromaWallCan.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,use_mesh_modifiers=True)
print('CAN_ASSET_COMPLETE',sum(len(o.data.polygons) for o in bpy.context.selected_objects if o.type=='MESH'))
