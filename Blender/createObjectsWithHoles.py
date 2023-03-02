import os
import sys
import bpy
import bmesh
import numpy as np
import math
import concurrent.futures
from threading import Thread
from concurrent.futures import ThreadPoolExecutor

def print(data):
    for window in bpy.context.window_manager.windows:
        screen = window.screen
        for area in screen.areas:
            if area.type == 'CONSOLE':
                override = {'window': window, 'screen': screen, 'area': area}
                bpy.ops.console.scrollback_append(override, text=str(data), type="OUTPUT")

def read_obj_files(path):
    obj_files = []
    for filename in os.listdir(path):
        if filename.endswith(".obj"):
            obj_files.append(os.path.join(path, filename))
    return obj_files


def load_obj_file(obj_file, location):
    try:
        bpy.ops.import_scene.obj(filepath=obj_file)
        obj = bpy.context.selected_objects[0]
        obj.location = location
        return obj
    except Exception as e:
        print(f"An exception occurred while loading {obj_file}: {str(e)}")
        return None

def clear_scene():
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            bpy.data.objects.remove(obj)

def save_object(obj, file_path, vertex_map):
    folder_path = os.path.join(file_path, obj.name[:4])
    os.makedirs(folder_path, exist_ok=True)
    
    # Apply the Triangulate modifier if it has not been applied already
    triangulate_mod = obj.modifiers.get("Triangulate") or obj.modifiers.new(name='Triangulate', type='TRIANGULATE')
    if not triangulate_mod.show_viewport and not (bpy.ops.object.modifier_apply(modifier=triangulate_mod.name) or True):
        print("Failed to triangulate object: {}".format(obj.name))
        return
    
    # Export the object to the specified file path
    obj_file_path = os.path.join(folder_path, obj.name[:4] + ".obj")
    bpy.ops.export_scene.obj(filepath=obj_file_path, use_materials=False)
    
    # Remove the Triangulate modifier if it was added in this function
    if not triangulate_mod.show_viewport:
        obj.modifiers.remove(triangulate_mod)

    # Save the vertex map to a file with the sames name as the OBJ file, but with a .txt extension
    vertex_map_file_path = os.path.join(folder_path, obj.name[:4] + '.txt')
    np.savetxt(vertex_map_file_path, vertex_map, fmt='%s')

def perform_boolean_operation(obj1, obj2, operation_type):
    # Add boolean modifier to obj1
    bool_mod = obj1.modifiers.new(name="bool_mod", type="BOOLEAN")
    bool_mod.object = obj2
    bool_mod.operation = operation_type
    bool_mod.solver = 'EXACT'
    # Apply the boolean modifier to obj1
    bpy.context.view_layer.objects.active = obj1
    bpy.ops.object.modifier_apply(modifier="bool_mod")


def generate_vertex_mapping(obj1, obj2):
    A = np.array([str(vertex.co.x) + str(vertex.co.y) + str(vertex.co.z) for vertex in obj1.data.vertices])
    B = np.array([str(vertex.co.x) + str(vertex.co.y) + str(vertex.co.z) for vertex in obj2.data.vertices])

    dict_B = {}
    for i, value in enumerate(B):
        dict_B[value] = i

    C = []
    for j, value in enumerate(A):
        if value in dict_B:
            C.append(dict_B[value])
        else:
            C.append('~')

    return np.array(C)

def get_space_beween(x):
    return 12.0 / (1 + 11 * (x - 0.005) / (0.06 - 0.005))

def get_bevel_width(x):
    if x < 0.055:
        return 1.0
    elif x >= 0.055:
        k = 1.0 - ((x - 0.055) / 0.005) ** 2
        return max(0.0, k)
    else:
        return 0.0

def create_rounded_cube_grid(num_rows, num_cols, size, length):
    shapes = []
    for i in range(num_rows):
        for j in range(num_cols):
            x = i * size*get_space_beween(size)
            y = j * size*get_space_beween(size)
            z = 0
            shape = None
            if size<=0.055:
                bpy.ops.mesh.primitive_cylinder_add(radius=size/2, depth=length, location=(x, y, z))
                shape = bpy.context.object
            else:
                bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z))
                shape = bpy.context.object
                shape.dimensions = (size, size, length)
            # Add bevel modifier to round the edges
                bevel_modifier = shape.modifiers.new(name='Bevel', type='BEVEL')
                bevel_modifier.segments = 10
                bevel_modifier.width = get_bevel_width(size)
            shapes.append(shape)
    # Select all cubes and join them into a single mesh
    for shape in shapes:
        shape.select_set(True)
    bpy.context.view_layer.objects.active = shapes[0]
    bpy.ops.object.join()
    # Center the combined mesh at (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CENTER_OF_MASS')
    bpy.context.object.location = (0, -(num_rows*size*get_space_beween(size)/2.5), 0)
    # Return the combined mesh object
    return bpy.context.object

def createFilterAndMapping(obj_file, size):
    clear_scene()
    obj1 = load_obj_file(obj_file, (0,0,0))
    obj2 = create_rounded_cube_grid(10, 10, size, 3)
    perform_boolean_operation(obj1, obj2, 'DIFFERENCE')
    bpy.data.objects.remove(obj2, do_unlink=True)
    obj3 = load_obj_file(obj_file, (0,0,0))
    vertex_map = generate_vertex_mapping(obj3, obj1)
    bpy.data.objects.remove(obj3, do_unlink=True)
    bpy.ops.render.render(write_still=True)
    return obj1, vertex_map


def get_percentage_changed(vertex_map):
    num_changed_vertices = sum([1 for v in vertex_map if v == '~'])
    return 100 * num_changed_vertices / len(vertex_map)

#checks if precentage is in the subcategory range and removes the subcategory if it is. always return two lists where precentage is used as pivot to return the upper and lower part of the list
def check_subcategory_ranges(precentage, subcategory_ranges, obj1, base_dir, vertex_map):
    upper = []
    lower = []
    to_remove = None
    for subcategory_range in subcategory_ranges:
        if precentage >= subcategory_range[0] and precentage <= subcategory_range[1]:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/ObjectsWithHoles/halla", "true \n\n")
            #Extends base_dir path with name of subcategory
            save_object(obj1, base_dir+"/"+str(subcategory_range[0])+"-"+str(subcategory_range[1]), vertex_map)
            to_remove = subcategory_range
        elif precentage > subcategory_range[1]:
            lower.append(subcategory_range)
        elif precentage < subcategory_range[0]:
            upper.append(subcategory_range)
    if to_remove:
        subcategory_ranges.remove(to_remove)
    return upper, lower

#Uses divide and conquer to create a function that will recursivly call itself until the correct radius is found for all subcategories

def recursive_filter(obj_file, size, subcategory_ranges):
    obj1, vertex_map = createFilterAndMapping(obj_file, size)
    pivot = get_percentage_changed(vertex_map)
    write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/ObjectsWithHoles/halla", str(pivot)+'\n')
    upper, lower = check_subcategory_ranges(pivot, subcategory_ranges, obj1, base_dir, vertex_map)
    if len(upper) != 0:
        for s in upper:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/ObjectsWithHoles/halla", "Upper "+str(s[0])+"\n\n")
        multiplier = 1.1 if size < 2 else 1.05
        recursive_filter(obj_file, size*multiplier, upper)
    if len(lower) != 0:
        for s in lower:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/ObjectsWithHoles/halla", "Lower "+str(s[0])+"\n\n")
        recursive_filter(obj_file, size*0.94, lower)
    return

def write_to_file(filename, data):
    with open(filename, 'a+') as file:
        file.write(data)



obj_files = read_obj_files("/Users/haakongunnarsli/masterprosjekt/dataset/RecalculatedNormals")
base_dir = "/Users/haakongunnarsli/masterprosjekt/dataset/ObjectsWithHoles"


initial_size = 0.04
subcategories = [(5.1, 15.0), (15.1, 25.0), (25.1, 35.0), (35.1, 45.0), (45.1, 55.0), (55.1, 65.0), (65.1, 75.0), (75.1, 85.0), (85.1, 95.1)]
clear_scene()
for obj_file in obj_files:
    try:
        recursive_filter(obj_file, initial_size, subcategories.copy())
    except Exception as e:
        print(f"An exception occurred while processing {obj_file}: {str(e)}")
        break

#testObj, vertex_map = createFilterAndMapping(obj_files[0], initial_size)
#print(str(get_percentage_changed(vertex_map)))

#x = 0.005
#print("----------------NyTest------------")
#test_cases = [0.005, 0.01, 0.02, 0.03, 0.04, 0.05, 0.055, 0.0575, 0.05875, 0.06]
#for x in test_cases:
#    print("[x = {:.3f}, k = {:.2f}]".format(x, get_space_beween(x)))