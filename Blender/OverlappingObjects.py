import os
import sys
import bpy
import bmesh
import numpy as np
import math
import random
import mathutils
import traceback
import bmesh

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


def load_obj_file(obj_file, location=(0,0,0), rotation=(0,0,0)):
    try:
        bpy.ops.import_scene.obj(filepath=obj_file)
        obj = bpy.context.selected_objects[0]
        obj.location = location
        obj.rotation_euler = rotation
        return obj
    except Exception as e:
        print(f"An exception occurred while loading {obj_file}: {str(e)}")
        return None

def clear_scene():
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            bpy.data.objects.remove(obj)

def save_object(obj, file_path, vertex_map):
    # Create the directory for the object if it does not already exist
    folder_path = os.path.join(file_path, obj.name[:4])
    os.makedirs(folder_path, exist_ok=True)

    # Apply the Triangulate modifier if it has not been applied already
    triangulate_mod = obj.modifiers.get("Triangulate") or obj.modifiers.new(name='Triangulate', type='TRIANGULATE')
    if not triangulate_mod.show_viewport and not (bpy.ops.object.modifier_apply(modifier=triangulate_mod.name) or True):
        print("Failed to triangulate object: {}".format(obj.name))
        return
    # Export the object to the specified file path
    obj_file_path = os.path.join(folder_path, obj.name[:4] + ".obj")
    context = bpy.context
    context.view_layer.objects.active = obj
    for ob in objects:
        ob.select_set(False)
    obj.select_set(True)
    bpy.ops.export_scene.obj(filepath=obj_file_path, use_selection=True, use_materials=False)

    # Remove the Triangulate modifier if it was added in this function
    if not triangulate_mod.show_viewport:
        obj.modifiers.remove(triangulate_mod)

    # Save the vertex map to a file with the same name as the OBJ file, but with a .txt extension
    vertex_map_file_path = os.path.join(folder_path, obj.name[:4] + '.txt')
    np.savetxt(vertex_map_file_path, vertex_map, fmt='%s')

def perform_boolean_operation(operation_type):
    # Loop over the list of objects and perform a boolean union operation between each pair
    result_obj = objects.pop(0)
    for obj in objects:
        # Add boolean modifier to result_obj
        bool_mod = result_obj.modifiers.new(name="bool_mod", type="BOOLEAN")
        bool_mod.object = obj
        bool_mod.operation = operation_type
        bool_mod.solver = 'EXACT'
        # Apply the boolean modifier to result_obj
        bpy.context.view_layer.objects.active = result_obj
        bpy.ops.object.modifier_apply(modifier="bool_mod")
    return result_obj

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
    

def get_new_position(obj):
    # Calculate the center point of the object
    center_point = obj.matrix_world @ obj.data.vertices[0].co
    for v in obj.data.vertices:
        center_point += obj.matrix_world @ v.co
    center_point /= len(obj.data.vertices)

    # Calculate a new location based on the center point
    new_location = obj.location - center_point

    return new_location


def addNewObjectsToScene(obj_files):
    obj_list = []
    obj_list.append(load_obj_file(obj_files[0]))

    # Calculate a new random location within a specified distance from the reference point
    for obj_file in obj_files[1:]:
        rotation = mathutils.Euler((random.uniform(0, 2*math.pi), random.uniform(0, 2*math.pi), random.uniform(0, 2*math.pi)), 'XYZ')
        obj = load_obj_file(obj_file, (0,0,0), rotation)
        obj_list.append(obj)
    return obj_list

def createFilterAndMapping(obj_files, scale):
    for obj in objects[1:]:
        obj.scale = (scale, scale, scale)
        new_location = get_new_position(obj)
        obj.location = new_location
    result_obj = perform_boolean_operation('UNION')
    obj3 = load_obj_file(obj_files[0])
    vertex_map = generate_vertex_mapping(obj3, result_obj)
    objects.insert(0, obj3)
    # Remove and unlink all objects in obj_list except the first element which are the one we are returning.
    return result_obj, vertex_map


def get_percentage_changed(vertex_map):
    num_changed_vertices = sum([1 for v in vertex_map if v == '~'])
    return  100 * num_changed_vertices / len(vertex_map)

#checks if precentage is in the subcategory range and removes the subcategory if it is. always return two lists where precentage is used as pivot to return the upper and lower part of the list
def check_subcategory_ranges(precentage, subcategory_ranges, obj1, base_dir, vertex_map, scale):
    upper = []
    lower = []
    to_remove = None
    for subcategory_range in subcategory_ranges:
        if precentage >= subcategory_range[0] and precentage <= subcategory_range[1]:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects/halla", "true \n\n")
            save_object(obj1, base_dir+"/"+str(subcategory_range[0])+"-"+str(subcategory_range[1]), vertex_map)
            to_remove = subcategory_range
        elif precentage > subcategory_range[1]:
            lower.append(subcategory_range)
        elif precentage < subcategory_range[0]:
            upper.append(subcategory_range)
    if to_remove:
        subcategory_ranges.remove(to_remove)
    if(len(lower)<=0 and precentage<lastPresentage or scale>8):
        write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects/halla", "New filter object. Scale was:"+str(scale)+" \n\n")
        random_files = get_random_files(currentObjFile, num_files=1)
        bpy.data.objects.remove(objects.pop(), do_unlink=True)
        objects.append(addNewObjectsToScene(random_files)[0])
        scale = initial_scale
    bpy.data.objects.remove(obj1, do_unlink=True)
    return upper, lower, scale

#Uses divide and conquer to create a function that will recursivly call itself until the correct radius is found for all subcategories

def recursive_filter(obj_files, scale, subcategory_ranges):
    obj1, vertex_map = createFilterAndMapping(obj_files, scale)
    pivot = get_percentage_changed(vertex_map)
    write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects/halla", str(pivot)+'\n')
    upper, lower, newScale = check_subcategory_ranges(pivot, subcategory_ranges, obj1, base_dir, vertex_map, scale)
    lastPresentage = pivot
    if len(upper) != 0:
        for s in upper:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects/halla", "Upper "+str(s[0])+"\n\n")
        recursive_filter(obj_files, newScale*1.1, upper)
    if len(lower) != 0:
        for s in lower:
            write_to_file("/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects/halla", "Lower "+str(s[0])+"\n\n")
        recursive_filter(obj_files, newScale*0.94, lower)
    return

def write_to_file(filename, data):
    with open(filename, 'a+') as file:
        file.write(data)

def get_random_files(current_file, num_files=3):
    # Get a list of all object files except the current one
    all_files = [f for f in obj_fily if f != current_file]

    # Select a unique subset of num_files random files
    random_files = random.sample(all_files, k=num_files)

    # Return the selected files
    return random_files
    

obj_fily = read_obj_files("/Users/haakongunnarsli/masterprosjekt/dataset/RecalculatedNormals")
base_dir = "/Users/haakongunnarsli/masterprosjekt/dataset/OverlappingObjects"
counter = 0
initial_scale = 1.5
subcategories = [(5.1, 15.0), (15.1, 25.0), (25.1, 35.0), (35.1, 45.0), (45.1, 55.0), (55.1, 65.0), (65.1, 75.0), (75.1, 85.0), (85.1, 95.1)]
objects = []
lastPresentage = 0
currentObjFile = obj_fily[0]

# Loop over the obj_files list, passing in a list of files to the recursive_filter function
for i, obj_file in enumerate(obj_fily):
    clear_scene()
    currentObjFile = obj_file
    # Combine the next element with the unique random subset
    file_list = [obj_file] + ["/Users/haakongunnarsli/masterprosjekt/dataset/RecalculatedNormals/0003.obj"]#get_random_files(obj_file, num_files=1)
    objects = addNewObjectsToScene(file_list)
    #createFilterAndMapping(objects, obj_files, initial_scale)
    try:
        recursive_filter(file_list, initial_scale, subcategories.copy())
    except Exception as e:
        print(f"An exception occurred while processing {obj_file}: {str(e)}")
        print(traceback.format_exc())
        break
    #clear_scene()
    break


#for obj_file in obj_files:
#    try:
#        recursive_filter(obj_file, initial_size, subcategories.copy())
#    except Exception as e:
#        print(f"An exception occurred while processing {obj_file}: {str(e)}")
#        break

#testObj, vertex_map = createFilterAndMapping(obj_files)
#print(str(get_percentage_changed(vertex_map)))

#x = 0.005
#print("----------------NyTest------------")
#test_cases = [0.005, 0.01, 0.02, 0.03, 0.04, 0.05, 0.055, 0.0575, 0.05875, 0.06]
#for x in test_cases:
#    print("[x = {:.3f}, k = {:.2f}]".format(x, get_space_beween(x)))