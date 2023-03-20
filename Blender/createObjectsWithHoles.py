import os
import sys
import bpy
import bmesh
import numpy as np
import math
import concurrent.futures
from threading import Thread
from concurrent.futures import ThreadPoolExecutor
import traceback
import random

def print(data):
    for window in bpy.context.window_manager.windows:
        screen = window.screen
        for area in screen.areas:
            if area.type == 'CONSOLE':
                override = {'window': window, 'screen': screen, 'area': area}
                bpy.ops.console.scrollback_append(override, text=str(data), type="OUTPUT")

def read_obj_files(path):
    path = os.path.abspath(os.path.join(os.path.dirname(__file__), path))
    obj_files = []
    for root, dirs, files in os.walk(path):
        for filename in files:
            if filename.endswith(".obj"):
                obj_files.append(os.path.join(root, filename))
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
    file_path = os.path.abspath(os.path.join(os.path.dirname(__file__), file_path))
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
        k = 1.0 - ((x - 0.055) / 0.1) ** 2
        return max(0.0, k)
    else:
        return 0.0

def create_rounded_cube_grid(num_rows, num_cols, size, length):
    shapes = []
    #Every other row is shifted by half the size of the cylinder
    for i in range(num_rows):
        for j in range(num_cols):
            x = i * size*get_space_beween(size) + (j % 2) * size*get_space_beween(size)/2
            y = j * size*get_space_beween(size) * np.sqrt(3)/2
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
            write_to_file("halla.txt", "true \n\n")
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
    global attempts
    obj1, vertex_map = createFilterAndMapping(obj_file, size)
    pivot = get_percentage_changed(vertex_map)
    write_to_file("halla.txt", str(pivot)+'\n')
    upper, lower = check_subcategory_ranges(pivot, subcategory_ranges, obj1, output_dir, vertex_map)
    attempts +=1
    if attempts > 160:
        write_to_file("halla.txt", "Skipper skippy \n\n")
        return
    if len(upper) != 0:
        multiplierU = new_cylinder_radius(size, get_volume(obj1)*5)
        for s in upper:
            write_to_file("halla.txt", "Upper "+str(s[0])+"M = "+str(multiplierU)+ " Size = " + str(size) +"\n\n")
        recursive_filter(obj_file, multiplierU, upper)
    if len(lower) != 0:
        multiplierL = new_cylinder_radius(size, (-get_volume(obj1)/5))
        for s in lower:
            write_to_file("halla.txt", "Lower "+str(s[0])+"M = "+str(multiplierL)+ " Size = " + str(size) +"\n\n")
        recursive_filter(obj_file, multiplierL, lower)
    return



def write_to_file(filename, data):
    p = os.path.abspath(os.path.join(os.path.dirname(__file__), output_dir))
    filename = os.path.join(p, filename)
    os.makedirs(p, exist_ok=True)
    with open(filename, 'a+') as file:
        file.write(data)

def skip(obj):
    p = os.path.abspath(os.path.join(os.path.dirname(__file__), output_dir))
    # Create the directory for the object if it does not already exist
    for root, dirs, files in os.walk(p):
        for filename in files:
            if filename == os.path.basename(obj):
                return True
    return False

def print_m_values():
    clear_scene()
    obj = load_obj_file(obj_files[0], (0,0,0))
    sizes = [0.01, 0.02, 0.03, 0.04, 0.045, 0.05, 0.055, 0.056, 0.057, 0.058, 0.059,  0.06, 0.07, 0.08, 0.09, 0.1]
    create_rounded_cube_grid(10, 10, 0.055, 3)
    for size in sizes:
        m = new_cylinder_radius(size, get_volume(obj)*5)
        m2 = new_cylinder_radius(size, (-get_volume(obj)/5))
        b = get_bevel_width(size)
        print(f"size={size}, m={m}, m2 = {m2}, b ={b}")

def new_cylinder_radius(old_radius, volume_change):
    old_volume = math.pi * (old_radius ** 2)
    target_volume = old_volume + volume_change
    new_radius = math.sqrt(target_volume /math.pi)
    return new_radius

def get_volume(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    volume = bm.calc_volume()
    bm.clear()
    return volume
    


output_dir = "../../../Dataset/ObjectsWithHoles"
obj_files = read_obj_files("../../../Dataset/RecalculatedNormals")
print(obj_files[1])
random.seed(2)
attempts = 0
counter = 0
initial_size = 0.04
subcategories = [(5.1, 15.0), (15.1, 25.0), (25.1, 35.0), (35.1, 45.0), (45.1, 55.0), (55.1, 65.0), (65.1, 75.0), (75.1, 85.0), (85.1, 95.1)]
print_m_values()

#clear_scene()

#for obj_file in obj_files:
#    if(counter > 1):
#        break
#    if skip(obj_file):
#        write_to_file("halla.txt", "Skipping: " + os.path.basename(obj_file) + "\n")
#        continue
#    try:
#        recursive_filter(obj_file, initial_size, subcategories.copy())
#    except Exception as e:
#        print(f"An exception occurred while processing {obj_file}: {str(e)}")
#        print(traceback.format_exc())
#        break
#    write_to_file("halla.txt", "-----------Object: " + os.path.basename(obj_file) + " ------ Attempts" +str(attempts)+"-----------------\n\n")
#    counter += 1
#    attempts = 0


#testObj, vertex_map = createFilterAndMapping(obj_files[0], initial_size)
#print(str(get_percentage_changed(vertex_map)))

#x = 0.005
#print("----------------NyTest------------")
#test_cases = [0.005, 0.01, 0.02, 0.03, 0.04, 0.05, 0.055, 0.0575, 0.05875, 0.06]
#for x in test_cases:
#    print("[x = {:.3f}, k = {:.2f}]".format(x, get_space_beween(x)))