import os
import pickle
import sys
import traceback
import bpy
import bmesh
import numpy as np
import math
import random
import gc

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
    obj_files.sort()
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
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.delete()
            gc.collect()

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

def get_space_between(x):
    global max_radius
    return 12.0 / (1 + 11 * x / max_radius)


def create_cylinder_grid_bounding_object(obj, radius, length):
    global initial_radius
    shapes = []
    space_between = get_space_between(radius)
    initial_space_between = get_space_between(initial_radius)
    print(f"Radius= {str(radius)}")
    obj_dimensions = obj.dimensions
    num_rows = int(np.ceil(obj_dimensions.x / (initial_radius * initial_space_between))) + 1
    num_cols = int(np.ceil(obj_dimensions.z / (initial_radius * initial_space_between * np.sqrt(3) / 2))) + 1

    # Choose the largest dimension as the inner loop
    if obj_dimensions.x > obj_dimensions.z:
        outer_range = num_cols
        inner_range = num_rows
        inner_dim = 'x'
    else:
        outer_range = num_rows
        inner_range = num_cols
        inner_dim = 'z'

    for i in range(outer_range):
        for j in range(inner_range):
            if inner_dim == 'x':
                x = obj.location.x + j * radius * space_between + (i % 2) * radius * space_between / 2
                y = obj.location.y + i * radius * space_between * np.sqrt(3) / 2
            else:
                x = obj.location.x + i * radius * space_between * np.sqrt(3) / 2
                y = obj.location.y + j * radius * space_between + (i % 2) * radius * space_between / 2

            z = 0
            bpy.ops.mesh.primitive_cylinder_add(radius=radius / 2, depth=length, location=(x, y, z))
            shape = bpy.context.object
            shapes.append(shape)

    for shape in shapes:
        shape.select_set(True)

    bpy.context.view_layer.objects.active = shapes[0]
    bpy.ops.object.join()
    bpy.ops.object.origin_set(type='ORIGIN_CENTER_OF_MASS')

    # Set the location of the grid to match the object's center in the Y axis
    bpy.context.object.location.x = obj.location.x
    bpy.context.object.location.y = - ((num_cols-1) * radius * space_between)/ 2
    return bpy.context.object


def createFilterAndMapping(obj_file, radius):
    clear_scene()
    obj1 = load_obj_file(obj_file, (0,0,0))
    obj2 = create_cylinder_grid_bounding_object(obj1, radius, 1)
    perform_boolean_operation(obj1, obj2, 'DIFFERENCE')
    mesh_data2= obj2.data
    bpy.data.objects.remove(obj2, do_unlink=True)
    bpy.data.meshes.remove(mesh_data2)
    obj3 = load_obj_file(obj_file, (0,0,0))
    vertex_map = generate_vertex_mapping(obj3, obj1)
    mesh_data3 = obj3.data
    bpy.data.objects.remove(obj3, do_unlink=True)
    bpy.data.meshes.remove(mesh_data3)
    bpy.ops.render.render(write_still=True)
    return obj1, vertex_map

def get_percentage_changed(vertex_map):
    num_changed_vertices = sum([1 for v in vertex_map if v == '~'])
    return 100 * num_changed_vertices / len(vertex_map)

#checks if precentage is in the subcategory range and removes the subcategory if it is. always return two lists where precentage is used as pivot to return the upper and lower part of the list
def check_subcategory_ranges(precentage, subcategory_ranges, obj1, base_dir, vertex_map):
    global attempts
    upper = []
    lower = []
    to_remove = None
    for subcategory_range in subcategory_ranges:
        if precentage >= subcategory_range[0] and precentage <= subcategory_range[1]:
            write_to_file("halla.txt", "true \n\n")
            attempts = 1
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

def recursive_filter(obj_file, radius, subcategory_ranges):
    global attempts
    global max_attempts
    global total_attempts
    obj1, vertex_map = createFilterAndMapping(obj_file, radius)
    pivot = get_percentage_changed(vertex_map)
    upper, lower = check_subcategory_ranges(pivot, subcategory_ranges, obj1, output_dir, vertex_map)
    attempts +=1
    total_attempts +=1
    printString = f"Pivot = {str(pivot)} radius = {radius} " 
    if attempts > max_attempts:
        write_to_file("halla.txt", "Skipper skippy \n\n")
        return
    if len(upper) != 0:
        multiplierU = calculate_fraction(attempts, max_attempts, 1.05)
        printString += f"upper linear decay = {multiplierU} radius = {radius*multiplierU}\n"
        printString += "Upper "
        for s in upper:
            printString += str(s[0]) + " "
        write_to_file("halla.txt", printString + "\n")
        recursive_filter(obj_file, radius*multiplierU, upper)
    if len(lower) != 0:
        multiplierL = calculate_fraction(attempts, max_attempts, 0.94)
        printString += f"upper linear decay = {multiplierL} radius = {radius*multiplierL}\n"
        printString += "Lower " 
        for s in lower:
            printString += str(s[0]) + " "
        write_to_file("halla.txt", printString + "\n")
        recursive_filter(obj_file, radius*multiplierL, lower)
    return

def calculate_fraction(a, max_a, x, scaling_factor=2):
    if a <= 15:
        return x
    adjusted_a = a - 15
    adjusted_max_a = max_a - 15
    fraction = adjusted_a / adjusted_max_a
    exponent = -scaling_factor * math.log2(x) * fraction
    result = x * math.exp(-exponent) + (1 - math.exp(-exponent))

    return result

def write_to_file(filename, data):
    p = os.path.abspath(os.path.join(os.path.dirname(__file__), output_dir))
    filename = os.path.join(p, filename)
    os.makedirs(p, exist_ok=True)
    with open(filename, 'a+') as file:
        file.write(data)

def skip(i):
    p = os.path.abspath(os.path.join(os.path.dirname(__file__), output_dir))
    next_obj_file = None
    if i + 1 < len(obj_files):
        next_obj_file = obj_files[i + 1]
    
    if next_obj_file == None:
        return False
    
    count = 0
    for root, dirs, files in os.walk(p):
        for filename in files:
            if os.path.basename(next_obj_file) == os.path.basename(filename):
                count += 1
                if count == len(subcategories):
                    return True
    return False



def print_m_values():
    global volume
    global max_radius
    global initial_radius
    clear_scene()
    file = obj_files[20]
    obj = load_obj_file(file, (0,0,0))
    #sizes = [0.01]
    sizes = [0.01, 0.02, 0.05, 0.07]
    volume = get_volume(file)
    initial_radius, max_radius = get_initial_and_max_radius(file)
    create_cylinder_grid_bounding_object(obj, initial_radius, 0.5)
#    m = initial_radius
#    m2 = initial_radius
#    for size in sizes:
#        m*=1.05
#        m2*=0.94
#        obj1, vertex_map1 = createFilterAndMapping(file, m)
#        pivot1 = get_percentage_changed(vertex_map1)
#        obj2, vertex_map2 = createFilterAndMapping(file, m2)
#        pivot2 = get_percentage_changed(vertex_map2)
#      
#        print(f"size={size}, m={m}, m2 = {m2} pivot1 = {pivot1} pivot2 = {pivot2}")

def new_cylinder_radius(old_radius, volume_change):
    old_volume = math.pi * (old_radius ** 2)
    target_volume = old_volume + volume_change
    new_radius = math.sqrt(target_volume /math.pi)
    return new_radius

def get_volume(obj_file):
    obj = load_obj_file(obj_file, (0,0,0))
    v = get_bounding_box_volume(obj)
    mesh_data= obj.data
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(mesh_data)
    print("volume =" + str(v))
    return v

#initial radius fraction makes the radius smaler and the max radius fraction will result in change of number of cylinders
def get_initial_and_max_radius(obj_file, initial_radius_fraction=0.1, max_radius_fraction=0.25):
    obj = load_obj_file(obj_file, (0,0,0))
    obj_dimensions = obj.dimensions
    avg_dimension = (obj_dimensions.x + obj_dimensions.y + obj_dimensions.z) / 3

    initial_radius = avg_dimension * initial_radius_fraction
    max_radius = avg_dimension * max_radius_fraction

    return initial_radius, max_radius

def get_bounding_box_volume(obj):
    bounding_box = obj.bound_box
    min_x = min([v[0] for v in bounding_box])
    max_x = max([v[0] for v in bounding_box])
    min_y = min([v[1] for v in bounding_box])
    max_y = max([v[1] for v in bounding_box])
    min_z = min([v[2] for v in bounding_box])
    max_z = max([v[2] for v in bounding_box])

    width = max_x - min_x
    length = max_y - min_y
    height = max_z - min_z

    v = width * length * height
    return v

output_dir = "../../../Dataset/ObjectsWithHoles"
obj_files = read_obj_files("../../../Dataset/NewRecalculatedNormals")
rng_states_path = os.path.join(os.path.abspath(os.path.join(os.path.dirname(__file__), output_dir)), "rng_states.pkl")


random.seed(2)
np.random.seed(2)
random_state = random.getstate()
numpy_state = np.random.get_state()
#volume = 0
max_attempts = 40
total_attempts = 0
attempts = 0
counter = 0
initial_radius = 0
max_radius = 0
subcategories = [(10.1, 20.0), (20.1, 30.0), (30.1, 40.0), (40.1, 50.0), (50.1, 60.0), (60.1, 70.0), (70.1, 80.0), (80.1, 90.0)]
#print_m_values()

if os.path.exists(rng_states_path):
    try:
        with open(rng_states_path, "rb") as f:
            loaded_random_state, loaded_numpy_state = pickle.load(f)
        random.setstate(loaded_random_state)
        np.random.set_state(loaded_numpy_state)
    except (FileNotFoundError, TypeError, ValueError) as e:
        print(f"An error occurred while loading the RNG states: {e}")

clear_scene()

for i, obj_file in enumerate(obj_files):
    if skip(i):
        write_to_file("halla.txt", f"Skipping: {os.path.basename(obj_file)}\n")
        continue
    try:
        #volume = get_volume(obj_file)
        initial_radius, max_radius = get_initial_and_max_radius(obj_file)
        recursive_filter(obj_file, initial_radius, subcategories.copy())
    except Exception as e:
        print(f"An exception occurred while processing {obj_file}: {str(e)}")
        print(traceback.format_exc())
        break

    # Save the last state, not the current one (same behavior as with the skip function)
    if i != 0:
        with open(rng_states_path, "wb") as f:
            pickle.dump((random_state, numpy_state), f)
    
    random_state = random.getstate()
    numpy_state = np.random.get_state()

    write_to_file("halla.txt", f"-----------Object: {os.path.basename(obj_file)} ------ Attempts {total_attempts}-----------------\n\n")
    counter += 1
    attempts = 0
    total_attempt = 0

