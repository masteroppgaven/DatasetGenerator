using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;


//Extends unity mesh class
public static class Objhandler
{

    public static Action<List<int>, string> WriteVerticesMappingToFile = (vertices, path) => {
        //Save vertices to a file seperated by a new line
        File.WriteAllLines(path, vertices.ConvertAll(i => i.ToString()));
    };


    //public static func that writes mesh to a obj file
    public static Action<Mesh, string> WriteMeshToObj = (mesh, path) => {
        //Use stringbuilder and string.format to write all vertice values to a .obj file
        StringBuilder sb = new StringBuilder();

        foreach (Vector3 vertex in mesh.vertices)
        {
            sb.AppendLine(string.Format("v {0} {1} {2}", vertex.x, vertex.y, vertex.z));
        }
        foreach (Vector3 normal in mesh.normals)
        {
            sb.AppendLine(string.Format("vn {0} {1} {2}", normal.x, normal.y, normal.z));
        }
        // writes "f" to file
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            sb.AppendLine(string.Format("f {0}//{0} {1}//{1} {2}//{2}",
            mesh.triangles[i] + 1, mesh.triangles[i + 1] + 1, mesh.triangles[i + 2] + 1)); // Denne er feil. Den skal peke på normalen som tilhører fjeset.
        }

        //Write all vertice values to a .obj file
        File.WriteAllText(path, sb.ToString());

    };

    public static Func<string, Mesh> LoadMesh = (path) => {

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
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        return mesh;
    };
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
