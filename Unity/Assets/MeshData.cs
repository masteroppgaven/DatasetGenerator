using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class MeshData
{
    public List<System.Numerics.Vector3> vertices { get; set; }
    public List<int> triangles { get; set; }
    public List<System.Numerics.Vector3> normals { get; set; }
    public string Name { get; set; }

    public MeshData(Mesh mesh)
    {
        vertices = mesh.vertices.Select(vertex => new System.Numerics.Vector3(vertex.x, vertex.y, vertex.z)).ToList();
        triangles = mesh.triangles.ToList();
        normals = mesh.normals.Select(normal => new System.Numerics.Vector3(normal.x, normal.y, normal.z)).ToList();
        Name = new string(mesh.name.ToCharArray());
    }
    public MeshData()
    {
        vertices = new();
        triangles = new();
        normals = new();
        Name = "";
    }

    public MeshData Clone()
    {
        MeshData clone = new MeshData(new Mesh());
        Debug.Log("2 what");
        // Deep copy the vertices list
        clone.vertices = new List<System.Numerics.Vector3>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            clone.vertices.Add(new System.Numerics.Vector3(vertices[i].X, vertices[i].Y, vertices[i].Z));
        }
        Debug.Log("3 what");
        // Deep copy the triangles list
        clone.triangles = new List<int>(triangles);

        // Deep copy the normals list
        clone.normals = new List<System.Numerics.Vector3>(normals.Count);
        for (int i = 0; i < normals.Count; i++)
        {
            clone.normals.Add(new System.Numerics.Vector3(normals[i].X, normals[i].Y, normals[i].Z));
        }
        Debug.Log("4 what");
        // Copy the name string
        clone.Name = new string(Name.ToCharArray());

        return clone;
    }
}
