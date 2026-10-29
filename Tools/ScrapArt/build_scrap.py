"""Deterministic original scrap art. Run with Blender --background --python this_file.
Exports a single mesh/material per FBX, a shared palette, editable blend, and previews.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Eco/Data/Models/OriginalScrap'
PREVIEW = ROOT / 'Docs/Art/Scrap'
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
random.seed(29)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

SOURCE_COLORS = ['#f4dfb3','#eacb85','#cb9a50','#885634','#513d32','#d95343','#f07a58','#88a85a',
          '#e9bd38','#f7db6b','#ad8133','#f6efda','#d1cbb5','#a6a994','#425458','#f7f5e9',
          '#ae78cb','#8054a6','#d8b7e6','#644589','#83bec1','#b4dddb','#508e9f','#edf8ed',
          '#388c73','#5db596','#225848','#91d7b8','#ba7d32','#dfa449','#72542f','#e1c698']

# Each category owns a full row: every surface stays within its identifying hue.
COLORS = ['#858000','#aaa100','#cbc200','#e4dc00','#f1e91c','#fff43c','#fff76b','#fffaa0',
          '#85898c','#a6aaac','#c4c7c8','#dbddde','#e7e8e8','#f0f1f0','#f7f8f6','#ffffff',
          '#402066','#542781','#6b329f','#803eb5','#934dca','#a65fdb','#b879e7','#cb98f0',
          '#124175','#155696','#196db5','#2085d1','#329be4','#51aff0','#78c3f7','#a3d9ff']
CATEGORY_ROW = {'Food':0,'Paper':1,'Plastic':2,'Glass':3}

def rgba(s):
    return tuple(int(s[i:i+2],16)/255 for i in (1,3,5))+(1,)
def linear(c):
    return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4

# Solid swatches are sampled in their centers; no text texture or external source art.
palette=bpy.data.images.new('ScrapPalette',width=256,height=128,alpha=True)
pixels=[]
for y in range(128):
    for x in range(256): pixels.extend(tuple(linear(v) for v in rgba(COLORS[(y//32)*8+x//32])[:3])+(1,))
palette.pixels=pixels
palette.filepath_raw=str(OUT/'ScrapPalette.png')
palette.file_format='PNG'
palette.save()
palette.pack()
mats=[]
for i,h in enumerate(COLORS):
    m=bpy.data.materials.new('Swatch_%02d'%i); m.diffuse_color=rgba(h); m.use_nodes=True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=tuple(linear(v) for v in rgba(h)[:3])+(1,)
    mats.append(m)
atlas=bpy.data.materials.new('ScrapPalette'); atlas.use_nodes=True
bsdf=atlas.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=.65
tex=atlas.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=palette; tex.interpolation='Closest'
atlas.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
category_materials={}
for category,roughness in [('Food',.7),('Paper',.9),('Plastic',.4),('Glass',.2)]:
    m=atlas.copy(); m.name='Scrap_'+category
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=roughness
    category_materials[category]=m
parts=[]; models=[]; records=[]

def material(obj, color):
    obj.data.materials.append(mats[color]); parts.append(obj); return obj
def mesh(name, verts, faces, color):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    return material(obj,color)
def box(name, loc, size, color, bevel=.02, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rotation)
    obj=bpy.context.object; obj.name=name; obj.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Soft worn edges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return material(obj,color)
def sphere(name,loc,scale,color,segments=12,rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
    obj=bpy.context.object; obj.name=name; obj.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return material(obj,color)
def tube(name,points,radii,color,sides=8):
    verts=[]; previous_axis=None
    for j,p in enumerate(points):
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize()
        axis=(previous_axis-tangent*previous_axis.dot(tangent)) if previous_axis is not None else tangent.cross(Vector((0,0,1)))
        if axis.length < .01: axis=tangent.cross(Vector((0,1,0)))
        axis.normalize(); previous_axis=axis.copy(); other=tangent.cross(axis).normalized()
        for i in range(sides):
            d=axis*math.cos(i*math.tau/sides)+other*math.sin(i*math.tau/sides)
            verts.append(Vector(p)+d*radii[j])
    faces=[tuple(reversed(range(sides)))]
    for j in range(len(points)-1):
        for i in range(sides):
            a=j*sides+i;b=j*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    return mesh(name,verts,faces,color)
def lathe(name,profile,color,n=16,warp=None,cap=True):
    verts=[]
    for j,(z,r) in enumerate(profile):
        for i in range(n):
            a=i*math.tau/n; p=(r*math.cos(a),r*math.sin(a),z)
            verts.append(warp(p,j,i) if warp else p)
    faces=[]
    for j in range(len(profile)-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n; faces.append((a,b,b+n,a+n))
    if cap: faces.extend([tuple(reversed(range(n))),tuple(range((len(profile)-1)*n,len(profile)*n))])
    return mesh(name,verts,faces,color)
def ring(name,z,r,width,color,n=16):
    return lathe(name,[(z-width,r-width),(z-width,r),(z+width,r),(z+width,r-width)],color,n,cap=False)
def sheet(name,verts,faces,color,thickness=.012):
    obj=mesh(name,verts,faces,color)
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Paper thickness','SOLIDIFY'); mod.thickness=thickness
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj
def finish(name,category,extent):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts: obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name
    # Assign a single UV swatch to every face, then merge all material slots.
    while obj.data.uv_layers: obj.data.uv_layers.remove(obj.data.uv_layers[0])
    uv=obj.data.uv_layers.new(name='PaletteUV')
    for poly in obj.data.polygons:
        source=int(obj.data.materials[poly.material_index].name.split('_')[-1])
        rgb=rgba(SOURCE_COLORS[source])
        value=.2126*rgb[0]+.7152*rgb[1]+.0722*rgb[2]
        shade=max(0,min(7,round(value*7)))
        # Keep paper visibly white; dark ink remains a small readable accent.
        if category=='Paper' and source!=14: shade=max(4,shade)
        sw=CATEGORY_ROW[category]*8+shade
        for li in poly.loop_indices: uv.data[li].uv=((sw%8+.5)/8,(sw//8+.5)/4)
        poly.material_index=0
    obj.data.materials.clear(); obj.data.materials.append(category_materials[category])
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    # Center pivot, metre scale, flat normals for a readable faceted style.
    coords=[obj.matrix_world@v.co for v in obj.data.vertices]
    low=Vector(tuple(min(v[i] for v in coords) for i in range(3)))
    high=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    center=(low+high)/2; factor=extent/max(high-low)
    for v,co in zip(obj.data.vertices,coords): v.co=(co-center)*factor
    obj.location=(0,0,0)
    bpy.context.view_layer.objects.active=obj
    # Recalculate closed-shell winding and triangulate explicitly for reproducible exports.
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT')
    tri=obj.modifiers.new('Export triangles','TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=tri.name)
    folder=OUT/category; folder.mkdir(exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,
        use_mesh_modifiers=True,path_mode='STRIP')
    models.append(obj); records.append(dict(name=name,category=category,triangles=len(obj.data.polygons),
        vertices=len(obj.data.vertices),extent=extent,fbx=str((folder/(name+'.fbx')).relative_to(ROOT)).replace('\\','/')))
    obj.hide_render=True; obj.hide_set(True); parts=[]

# FOOD — concave core, three open peel lobes, and an articulated fish skeleton.
lathe('Bitten flesh',[(0,.22),(.08,.28),(.16,.20),(.26,.13),(.44,.12),(.58,.20),(.64,.29),(.70,.22)],0,14,
      lambda p,j,i:(p[0]*(1+.06*math.sin(i*2.3)),p[1],p[2]+.012*math.sin(i*2)))
lathe('Bottom red skin',[(-.01,.12),(0,.23),(.06,.285),(.13,.24)],5,14)
lathe('Top red skin',[(.59,.23),(.65,.30),(.71,.25),(.74,.10)],5,14)
tube('Stem',[(0,0,.70),(.02,0,.83),(.06,.01,.89)],[.04,.03,.015],4)
sheet('Leaf',[(.02,0,.81),(.12,-.06,.86),(.28,0,.87),(.13,.05,.85)],[(0,1,2),(0,2,3)],7)
for z in [.28,.43]: sphere('Seed',(.0,-.125,z),(.025,.013,.042),4,8,4)
finish('Food_AppleCore','Food',.86)

lathe('Peel center',[(.06,.05),(.15,.14),(.35,.13),(.53,.07)],8,10)
for k in range(3):
    a=k*math.tau/3+.25; verts=[]
    for j in range(7):
        t=j/6; r=.05+.62*t; z=.48*(1-t)**2+.035+.065*t**5
        width=.11*math.sin(math.pi*(t*.87+.10))
        for s in [-1,0,1]:
            verts.append((r*math.cos(a)-s*width*math.sin(a),r*math.sin(a)+s*width*math.cos(a),z+abs(s)*.035))
    faces=[]
    for j in range(6):
        for q in range(2):
            v=j*3+q; faces.append((v,v+3,v+4,v+1))
    sheet('Curled peel',verts,faces,9,.025)
    tube('Dark peel edge',[verts[j*3] for j in range(7)],[.014]*7,8,5)
tube('Dry stem',[(0,0,.49),(.02,0,.59),(.04,0,.65)],[.055,.04,.03],10)
finish('Food_BananaPeel','Food',1.10)

tube('Spine',[(-.49,0,.14),(0,0,.17),(.43,0,.12)],[.04,.045,.03],11)
for i in range(5):
    x=-.29+i*.145; w=.25*(1-i*.12)
    for s in [-1,1]: tube('Rib',[(x,0,.16),(x+.07,s*w*.65,.17),(x+.11,s*w,.10)], [.029,.023,.009],11,6)
sphere('Fish head',(-.58,0,.15),(.19,.16,.13),11)
sphere('Eye socket',(-.60,-.128,.19),(.052,.027,.053),4,10,5)
sphere('Eye socket',(-.60,.128,.19),(.052,.027,.053),4,10,5)
sheet('Tail fin',[(.38,0,.12),(.67,-.22,.12),(.61,0,.13),(.67,.22,.12)],[(0,1,2),(0,2,3)],12,.04)
finish('Food_FishBones','Food',1.20)

# PAPER — torn-open gable carton, folded printed sheet, creased paper ball.
box('Carton body',(0,0,.30),(.40,.34,.60),15,.025)
box('Blue front panel',(0,-.176,.28),(.34,.008,.34),20,.002)
box('Blue back panel',(0,.176,.28),(.34,.008,.34),20,.002)
sheet('Folded gable',[(-.20,-.17,.59),(.20,-.17,.59),(.20,0,.77),(-.20,0,.77),(-.20,.17,.59),(.20,.17,.59)],[(0,1,2,3),(3,2,5,4)],11,.018)
box('Crimp top',(0,0,.79),(.40,.035,.06),12,.008,rotation=(0,.07,0))
box('Printed bar',(0,-.183,.37),(.22,.008,.035),15,.003)
sphere('Milk drop',(0,-.19,.24),(.055,.008,.075),15)
for x in [-.09,-.05,0,.03,.07]: box('Barcode',(x,.183,.25),(.012,.006,.11),14,.0)
finish('Paper_MilkCarton','Paper',1.05)

for i in range(4): box('Folded pages',(0,.005*i,.018*i),(.65,.46,.014),11 if i%2 else 12,.004)
sheet('Curling page',[(-.33,-.23,.067),(.32,-.23,.067),(-.32,.05,.095),(.32,.05,.095),(-.29,.23,.17),(.32,.23,.14)],[(0,1,3,2),(2,3,5,4)],15,.008)
box('Headline',(-.03,-.16,.077),(.49,.027,.008),14,.0)
for i in range(5):
    for x in [-.17,.13]: box('Ink column',(x,-.11+i*.026,.080+i*.0024),(.20-(i%3)*.024,.008,.006),13,.0)
finish('Paper_FoldedNewspaper','Paper',.90)

bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.28)
obj=bpy.context.object; obj.name='Creased paper'
for v in obj.data.vertices:
    v.co*=random.uniform(.70,1.15)
obj.scale=(1,1,.78)
material(obj,11)
obj.data.materials.append(mats[12]); obj.data.materials.append(mats[15])
for p in obj.data.polygons: p.material_index=random.choices([0,1,2],[.5,.2,.3])[0]
sheet('Loose torn flap',[(.15,0,.09),(.34,.08,.11),(.30,.20,.24),(.08,.16,.18)],[(0,1,2),(0,2,3)],15,.012)
finish('Paper_CrumpledSheet','Paper',.68)

# PLASTIC — asymmetric crushed ribbing, a peeled yogurt seal, integral jug handle.
def dent(p,j,i):
    x,y,z=p; t=max(0,1-abs(z-.36)/.25); a=i*math.tau/16
    return (x*(1-.50*t)+.06*t,y*(1+.12*math.sin(i*2+j)),z+.025*t*math.sin(a*3))
profile=[(0,.12),(.04,.16)]
for j in range(1,10): profile.append((.04+j*.055,.155 if j%2 else .175))
profile.extend([(.62,.12),(.70,.065),(.79,.065)])
lathe('Crushed PET body',profile,18,16,dent)
lathe('Crumpled label',[(.26,.175),(.31,.176),(.43,.176),(.48,.175)],16,16,dent,False)
lathe('Violet screw cap',[(.77,.075),(.84,.075)],17,16)
ring('Cap rim',.78,.078,.008,19)
finish('Plastic_CrushedBottle','Plastic',1.05)

lathe('Cup shell',[(0,.16),(.035,.18),(.36,.255),(.39,.26),(.39,.235),(.34,.229),(.05,.155)],15,16)
lathe('Violet label',[(.10,.198),(.28,.239)],16,16,cap=False)
ring('Cup lip',.385,.255,.018,18)
lathe('Dark empty interior',[(.055,.152),(.06,.152)],19,16)
sheet('Peeled foil',[(-.23,0,.41),(-.17,.17,.41),(0,.24,.41),(.13,.19,.47),(.16,.09,.63),(.02,.01,.64)],[(0,1,2,3),(0,3,4,5)],12,.008)
box('Label mark',(0,-.226,.20),(.16,.01,.075),15,.005)
finish('Plastic_YogurtCup','Plastic',.72)

box('Jug body',(0,0,.30),(.42,.27,.58),16,.07)
lathe('Sloping shoulder',[(.51,.18),(.62,.13),(.68,.07)],16,12,lambda p,j,i:(p[0]-.05,p[1]*.72,p[2]))
cap=lathe('Jug cap',[(.67,.083),(.77,.083)],19,12); cap.location.x=-.05
tube('Open handle',[(.12,0,.57),(.32,0,.58),(.38,0,.47),(.38,0,.30),(.20,0,.21)],[.047]*5,17,8)
box('Cream label',(-.025,-.139,.31),(.25,.012,.25),15,.028)
sphere('Label leaf',(-.025,-.151,.31),(.06,.009,.085),7,8,4)
finish('Plastic_DetergentJug','Plastic',.98)

# GLASS — thick visible lips and interior walls; opaque stylized glass avoids sorting issues.
lathe('Bottle body',[(0,.15),(.045,.19),(.50,.19),(.61,.15),(.69,.075),(.87,.075),(.90,.09),(.93,.09),(.93,.053),(.87,.053),(.71,.053)],24,16)
ring('Bottle heel',.05,.19,.012,26)
lathe('Paper label',[(.20,.192),(.44,.192)],31,16,cap=False)
box('Label emblem',(0,-.194,.32),(.12,.006,.09),26,.012)
tube('Glass glint',[(-.12,-.14,.48),(-.12,-.14,.16)],[.014,.014],27,4)
finish('Glass_GreenBottle','Glass',1.15)

lathe('Amber jar',[(0,.22),(.04,.26),(.44,.26),(.50,.22),(.56,.22),(.56,.19),(.49,.19),(.43,.23),(.06,.23)],28,16)
ring('Jar thread',.52,.23,.015,29)
ring('Jar lip',.56,.22,.012,29)
lathe('Worn paper band',[(.12,.263),(.34,.263)],31,16,cap=False)
for z in [.18,.23,.28]: box('Label lines',(0,-.266,z),(.22,.008,.013),30,.0)
finish('Glass_AmberJar','Glass',.78)

n=14; verts=[]
for z,r in [(0,.20),(.05,.23),(.37,.23),(.37,.202),(.055,.202)]:
    for i in range(n):
        a=i*math.tau/n; zz=z
        if z==.37: zz+=[.10,-.06,.02,.14,-.04,.07,-.08][i%7]
        verts.append((r*math.cos(a),r*math.sin(a),zz))
faces=[]
for j in range(4):
    for i in range(n): faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
faces.extend([tuple(reversed(range(n))),tuple(range(4*n,5*n))])
mesh('Jagged bottle base',verts,faces,20)
ring('Heavy glass heel',.04,.23,.018,22)
sheet('Attached torn label',[(-.16,-.19,.09),(.13,-.20,.09),(.14,-.20,.22),(.02,-.23,.27),(-.14,-.20,.23)],[(0,1,2,3,4)],11,.008)
finish('Glass_BrokenBottle','Glass',.70)

# Keep the editable pack outside Assets so Unity never invokes Blender on import.
for i,obj in enumerate(models):
    obj.hide_set(False); obj.hide_render=False; obj.location=((i//3)*1.8,(i%3)*1.8,0)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Tools/ScrapArt/OriginalScrap.blend'))
(OUT/'catalog.json').write_text(json.dumps(records,indent=2)+'\n')

# Individual consistent orthographic product views for the review sheet.
for obj in models: obj.hide_render=True; obj.location=(0,0,0)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.cycles.use_denoising=True; scene.render.resolution_x=480; scene.render.resolution_y=420
scene.render.resolution_percentage=100; scene.render.film_transparent=True
scene.world.color=(.22,.22,.22)
scene.view_settings.view_transform='AgX'
bpy.ops.object.camera_add(location=(2.5,-4.2,2.7)); camera=bpy.context.object
camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.type='ORTHO'; camera.data.ortho_scale=1.62
scene.camera=camera
for loc,power,size in [((1,-3,5),500,4),((-3,-1,2),220,3),((1,3,3),350,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object
    light.data.energy=power;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
for obj,rec in zip(models,records):
    obj.hide_render=False
    bsdf.inputs['Roughness'].default_value={'Food':.7,'Paper':.9,'Plastic':.4,'Glass':.2}[rec['category']]
    scene.render.filepath=str(PREVIEW/(obj.name+'.png')); bpy.ops.render.render(write_still=True)
    obj.hide_render=True
print('SCRAP_COMPLETE '+json.dumps(records))
