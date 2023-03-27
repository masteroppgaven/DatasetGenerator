import bpy
import bmesh
from mathutils import Matrix
import os

def clear_scene():
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            bpy.data.objects.remove(obj)

outputCSV = "dataset,category,object,volume,x,y,z\n"

fileFolder = "/mnt/VOID/projects/shape_descriptors_benchmark/Dataset/RandomVertexDisplacedObjectsDataset"

for catRoot, catDirs, catFiles in os.walk(fileFolder):
    for dirs in sorted(catDirs):
        for _, objDirs, _ in os.walk(catRoot+"/"+dirs):
            for obj in sorted(objDirs):
                clear_scene()
                objFile = catRoot + "/" + dirs + "/" + obj + "/" + obj + ".obj"
                bpy.ops.import_scene.obj(filepath=objFile)
                object = bpy.context.selected_objects[0]

                bm = bmesh.new()
                bm.from_mesh(object.data)

                output = "RandomVertexDisplacedObjectsDataset," + dirs + "," + obj + "," + str(bm.calc_volume()) + "," +  str(object.dimensions.x) + "," + str(object.dimensions.y) + "," + str(object.dimensions.z) + "\n"

                print(output)

                outputCSV += output

                bm.clear()


file = open("RandomVertexDisplacedObjectsDataset_volumes.csv", "w")
for row in outputCSV:
    file.write(row)
file.close()
