using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.UIElements;

public class Utilities
{

    public static void addFloorToScene(float width, float length, Material mat = null)
    {
        //Sets material to the floor as leafes. not standard
        GameObject floor = new GameObject("Floor");
        /*
        Rigidbody rigidbody = floor.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        */
        MeshFilter meshFilter = floor.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = floor.AddComponent<MeshRenderer>();
        BoxCollider boxCollider = floor.AddComponent<BoxCollider>();

        // Set up the mesh filter
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-width / 2, 0, -length / 2),
            new Vector3(width / 2, 0, -length / 2),
            new Vector3(width / 2, 0, length / 2),
            new Vector3(-width / 2, 0, length / 2)
        };
        int[] triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;

        // Set up the mesh renderer
        meshRenderer.material = mat;

        // Set up the box collider
        boxCollider.size = new Vector3(width, 0, length);
    }

    public static void addRigidbody(GameObject gameObject, bool useGravity)
    {
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = useGravity;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.isKinematic = false;
    }

    public static void addMeshCollider(GameObject gameObject)
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
    }

    public static void addPhysicsForClusterMeshesDataset(List<Rigidbody> rigidbodies, float forceAmount)
    {
        for (int i = 0; i < rigidbodies.Count; i++)
        {
            Rigidbody rigidbody1 = rigidbodies[i];
            var otherRigidbodies = rigidbodies.Where((rb, j) => j != i);
            foreach (Rigidbody rigidbody2 in otherRigidbodies)
            {
                Vector3 direction = rigidbody1.transform.position - rigidbody2.transform.position;
                float distance = direction.magnitude;
                direction = direction.normalized;
                rigidbody1.AddForce(direction * forceAmount * (distance + 1f));
            }
        }
    }

    public static List<GameObject> createGameObjectsFromMeshes(List<Mesh> meshes, bool placeOnUnitSphere = false, Vector3 position = default(Vector3))
    {
        List<GameObject> gameObjects = new List<GameObject>();
        meshes.ForEach(mesh =>
        {
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
            gameObject.GetComponent<MeshFilter>().mesh.name = mesh.name;
            gameObject.transform.position = position;

            if (placeOnUnitSphere)
            {
                float minDistance = 0.2f;
                float maxDistance = 1.0f;
                float distance = UnityEngine.Random.Range(minDistance, maxDistance);
                gameObject.transform.position += UnityEngine.Random.onUnitSphere * distance + new Vector3(0, distance, 0);
                gameObject.transform.rotation = Quaternion.Euler(UnityEngine.Random.value * 360.0f, UnityEngine.Random.value * 360.0f, UnityEngine.Random.value * 360.0f);
            }
            gameObjects.Add(gameObject);
        });
        return gameObjects;
    }

    public static GameObject createGameObjectFromMesh(Mesh mesh, bool placeOnUnitSphere = false, Vector3 position = default(Vector3))
    {
        GameObject gameObject = new GameObject();
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshFilter>().mesh.name = mesh.name;
        gameObject.transform.position = position;

        if (placeOnUnitSphere)
        {
            float minDistance = 0.2f;
            float maxDistance = 1.0f;
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            gameObject.transform.position += UnityEngine.Random.onUnitSphere * distance + new Vector3(0, distance, 0);
            gameObject.transform.rotation = Quaternion.Euler(UnityEngine.Random.value * 360.0f, UnityEngine.Random.value * 360.0f, UnityEngine.Random.value * 360.0f);
        }

        return gameObject;
    }

    //remove gameObjects from the scene
    public static void removeGameObjects(List<GameObject> gameObjects)
    {
        for (int i = gameObjects.Count - 1; i >= 0; i--)
        {
            GameObject gameObject = gameObjects[i];
            GameObject.Destroy(gameObject);
            gameObjects.RemoveAt(i);
        }

    }

    public static Mesh combineMeshes(List<GameObject> gameObjects)
    {
        CombineInstance[] combine = new CombineInstance[gameObjects.Count];
        for (int i = 0; i < combine.Length; i++)
        {
            MeshFilter meshFilter = gameObjects[i].GetComponent<MeshFilter>();
            combine[i].mesh = meshFilter.sharedMesh;
            combine[i].transform = meshFilter.transform.localToWorldMatrix;
            meshFilter.gameObject.SetActive(false);
        }
        Mesh newMesh = new();
        newMesh.name = gameObjects[0].GetComponent<MeshFilter>().mesh.name;
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.CombineMeshes(combine, true, true, false);
        newMesh.RecalculateNormals();
        Debug.Log(newMesh.normals[0].ToString());
        // Return the combined mesh.
        return newMesh;
    }

    public static Mesh MoveMesh(Mesh mesh, Vector3 direction, float distance)
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += direction * distance;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        return mesh;
    }

    //takes in a Mesh and resize it to a random size.TOOODOOO.public static Mesh RandomRotateMesh(Mesh mesh, float degreeX = -1f, float degreeY = -1f, float degreeZ = -1f)
    public static Mesh RandomRotateMesh(Mesh mesh, float degreeX = -1f, float degreeY = -1f, float degreeZ = -1f)
    {
        //Rotating by the product lhs * rhs is the same as applying the two rotations in sequence.
        Vector3[] vertices = mesh.vertices;
        Quaternion rotation = Quaternion.identity;
        if (degreeX == -1f) rotation *= Quaternion.Euler(UnityEngine.Random.Range(1f, 359f), 0f, 0f);
        else rotation *= Quaternion.Euler(degreeX, 0f, 0f);
        if (degreeY == -1f) rotation *= Quaternion.Euler(0f, UnityEngine.Random.Range(1f, 359f), 0f);
        else rotation *= Quaternion.Euler(0f, degreeY, 0f);
        if (degreeZ == -1f) rotation *= Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(1f, 359f));
        else rotation *= Quaternion.Euler(0f, 0f, degreeZ);
        for (int i = 0; i < vertices.Length; i++) vertices[i] = rotation * vertices[i];
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        return mesh;
    }


    //takes in a Mesh and resize it to a random size
    public static Mesh TransformMesh(Mesh mesh, float scaleX = 1f, float scaleY = 1f, float scaleZ = 1f)
    {
        mesh.RecalculateNormals();
        Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
        mesh.vertices = mesh.vertices.Select(v => Vector3.Scale(v, scale)).ToArray();
        if (scaleX < 0 || scaleY < 0 || scaleZ < 0)
        {
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                float x = mesh.normals[i].x;
                float y = mesh.normals[i].y;
                float z = mesh.normals[i].z;
                if (scaleX < 0) x = -x;
                if (scaleY < 0) y = -y;
                if (scaleZ < 0) z = -z;
                normals[i] = new Vector3(x, y, z);
            }
            mesh.normals = normals;
            mesh.triangles = mesh.triangles.Reverse().ToArray();
        }

        return mesh;
    }


    public static List<int> GenerateRandomNumbers(int rangeStart, int rangeEnd, int numberOfInts, int illegalNumber = -1)
    {
        List<int> randomNumbers = new List<int>();
        System.Random random = new System.Random();

        while (randomNumbers.Count < numberOfInts)
        {
            int randomNumber = random.Next(rangeStart, rangeEnd);

            if (randomNumber == illegalNumber)
            {
                continue;
            }

            if (!randomNumbers.Contains(randomNumber))
            {
                randomNumbers.Add(randomNumber);
            }
        }

        return randomNumbers;
    }
    public static Mesh Copy(Mesh mesh)
    {
        return new Mesh
        {
            name = mesh.name,
            vertices = mesh.vertices == null || mesh.vertices.Length == 0 ? null : (Vector3[])mesh.vertices.Clone(),
            triangles = mesh.triangles == null || mesh.triangles.Length == 0 ? null : (int[])mesh.triangles.Clone(),
            normals = mesh.normals == null || mesh.normals.Length == 0 ? null : (Vector3[])mesh.normals.Clone()
        };
    }

    public static void AddDeformation(GameObject obj, float forceMultiplier, float deformationLimit)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        Vector3[] originalVertices = mesh.vertices;
        Vector3[] deformedVertices = new Vector3[originalVertices.Length];
        Rigidbody rigidBody = obj.GetComponent<Rigidbody>();

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 force = (originalVertices[i] - obj.transform.InverseTransformPoint(rigidBody.worldCenterOfMass)) * rigidBody.mass * forceMultiplier;
            Vector3 deformation = force / (1f + Vector3.Dot(force, force));
            deformation = Vector3.ClampMagnitude(deformation, deformationLimit);
            deformedVertices[i] = originalVertices[i] + deformation;
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
    }
    public static void CopyFromTo(List<String> files, String loadFrom, String saveTo)
    {
        if (!Directory.Exists(saveTo))
        {
            Directory.CreateDirectory(saveTo);
        }
        files.ForEach(file =>
        {
            File.Copy(file, saveTo, true);
        });

    }

    public static GameObject CreateCylinder(float radius, float length)
    {
        // Create a new cylinder GameObject
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        // Get the mesh of the cylinder
        Mesh mesh = cylinder.GetComponent<MeshFilter>().mesh;

        // Get the vertices of the mesh
        Vector3[] vertices = mesh.vertices;

        // Scale down the vertices
        float scaleFactor = radius / 0.5f;
        float scaleFactorL = length / 2.0f;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y += length;
            vertices[i].x *= scaleFactor;
            vertices[i].y *= scaleFactorL;
            vertices[i].z *= scaleFactor;
        }

        // Update the mesh with the scaled vertices
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Return the cylinder GameObject
        return cylinder;
    }

    public static int GetRandomHighestVertexIndex(GameObject obj, StreamWriter logFile)
    {
        
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        List<int> highestIndices = new List<int>();
        float highestValue = 0f;

        // Find the highest absolute value of any vertex coordinate
        for (int i = 0; i < vertices.Length; i++)
        {
            float vertexValue = Mathf.Abs(vertices[i].x) + Mathf.Abs(vertices[i].y) + Mathf.Abs(vertices[i].z);
            if (vertexValue > highestValue)
            {
                highestIndices.Clear();
                highestIndices.Add(i);
                highestValue = vertexValue;
            }
            else if (vertexValue == highestValue)
            {
                highestIndices.Add(i);
            }
        }
        //logFile.WriteLine(string.Join("\n", Enumerable.Range(0, vertices.Length).Select(i => $"Vertex {i}: {vertices[i].ToString("F3")}")));
        //logFile.WriteLine(vertices[highestIndices[0]] + "highest!!");
        // Return a random index from among the highest vertices
        return highestIndices[UnityEngine.Random.Range(0, highestIndices.Count)];
    }
}




