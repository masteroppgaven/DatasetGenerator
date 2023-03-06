using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
using UnityEngine;

class ObjHandler
{
    private string pathToDataset;
    private string saveTo;
    private BlockingCollection<Tuple<MeshData, string, List<string>, string>> queue;
    private Task consumerTask;
    private int counter = 0;

    public ObjHandler(string saveTo, string pathToDataset)
    {
        this.pathToDataset = pathToDataset;
        this.saveTo = saveTo;
        Awake();
    }

    private void Awake()
    {
        this.queue = new BlockingCollection<Tuple<MeshData, string, List<string>, string>>(new ConcurrentQueue<Tuple<MeshData, string, List<string>, string>>(), 9);

        this.consumerTask = Task.Run(() =>
        {
            while (!queue.IsCompleted)
            {
                Tuple<MeshData, string, List<string>, string> task;
                try
                {
                    task = queue.Take();
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                WriteMeshToObjAsync(task.Item1, task.Item2).Wait();
                if (task.Item3 != null)
                {
                    WriteVerticesMappingToFileAsync(task.Item3, task.Item4).Wait();
                }
            }
        });
    }

    public void CompleteWriting()
    {
        queue.CompleteAdding();
        consumerTask.Wait();
    }

    public void saveToFile(MeshData meshy, List<string> mapping = null, string subFolder = null)
    {
        subFolder = "/" + (subFolder == null ? "0-100" : subFolder);
        string filename = pathToDataset + saveTo + subFolder + "/" + meshy.Name + "/" + meshy.Name + ".obj";
        string mappingFilename = pathToDataset + saveTo + subFolder + "/" + meshy.Name + "/" + meshy.Name + ".txt";
        queue.Add(new Tuple<MeshData, string, List<string>, string>(meshy, filename, mapping, mappingFilename));
    }
    
    public async Task WriteVerticesMappingToFileAsync(List<string> mapping, string path)
    {

        // Save vertices to a file separated by a new line
        await Task.Run(() =>
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllLines(path, mapping);
        });
    }

    public async Task WriteMeshToObjAsync(MeshData mesh, string path)
    {
        //Use stringbuilder and string.format to write all vertice values to a .obj file
        StringBuilder sb = new StringBuilder();
        object sbLock = new object();
        lock (sbLock)
        {
            {
                foreach (System.Numerics.Vector3 vertex in mesh.vertices)
                {
                    sb.AppendLine(string.Format("v {0:F6} {1:F6} {2:F6}", vertex.X, vertex.Y, vertex.Z));
                }
                foreach (System.Numerics.Vector3 normal in mesh.normals)
                {
                    sb.AppendLine(string.Format("vn {0} {1} {2}", normal.X, normal.Y, normal.Z));
                }
                // writes "f" to file
                for (int i = 0; i < mesh.triangles.Count; i += 3)
                {
                    sb.AppendLine(string.Format("f {0}//{0} {1}//{1} {2}//{2}",
                    mesh.triangles[i] + 1, mesh.triangles[i + 1] + 1, mesh.triangles[i + 2] + 1)); // Denne er feil. Den skal peke på normalen som tilhører fjeset.
                }
            }
        }
        // Write all vertice values to a .obj file
        await Task.Run(() =>
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, sb.ToString());
        });
    }

    public Mesh LoadMesh(string path, string newName = "")
    {
        //Import verticies and faces from a .obj file -----------------------------------------------------
        List<string> lines = new(System.IO.File.ReadAllLines(path));
        List<Vector3> vertices = new();
        List<int> triangles = new();

        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                string[] values = line.Split(' ');
                vertices.Add(new Vector3(float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])));
            }
            if (line.StartsWith("f "))
            {
                //split on space and 
                string[] values = line.Split(' ');
                string[] values2 = values[1].Split('/');
                string[] values3 = values[2].Split('/');
                string[] values4 = values[3].Split('/');

                if (values.Length > 0)
                {
                    triangles.Add(int.Parse(values2[0]) - 1);
                    triangles.Add(int.Parse(values3[0]) - 1);
                    triangles.Add(int.Parse(values4[0]) - 1);
                }
            }
        }
        Mesh mesh = new();
        mesh.name = string.IsNullOrEmpty(newName) ? Path.GetFileNameWithoutExtension(path) : newName;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }
    /* Tests
    foreach (int triangle in triangles)
    {
        if (triangle > 3024)
        {
            Debug.Log(vertices.Count);
            Debug.Log(triangle);
            Debug.Log(vertices[triangle]);
        }
    }
    if(int.Parse(values2[0]) > 3024 || int.Parse(values3[0]) > 3024 || int.Parse(values4[0]) > 3024){
        Debug.Log("values2: " + values2[0] + " values3: " + values3[0] + " values4: " + values4[0]);
        Debug.Log("values2: " + values2[1] + " values3: " + values3[1] + " values4: " + values4[1]);
        Debug.Log("values2: " + values2[2] + " values3: " + values3[2] + " values4: " + values4[2]);
    }

    Debug.Log("Triangle length: " + triangles.Count);
    Debug.Log("Vertex length: " + vertices.Count);

    Debug.Log(triangles.ToArray().Length);
    */

}


//Normals estimation by loooking at neighboring verticies connected by faces.
