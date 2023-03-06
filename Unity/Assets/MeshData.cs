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
}
