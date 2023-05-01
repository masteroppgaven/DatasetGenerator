using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections;

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

    public static void addRigidbody(GameObject gameObject, bool useGravity, bool isKinematic = false)
    {
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = useGravity;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.isKinematic = isKinematic;
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
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshFilter>().mesh.name = mesh.name;
        gameObject.transform.position = position;
        gameObject.layer = 10;

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
    public static void RemoveGameObjects(List<GameObject> gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            GameObject.DestroyImmediate(gameObject);
        }
        gameObjects.Clear();
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
        mesh.RecalculateNormals();
        return mesh;
    }

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
        while (randomNumbers.Count < numberOfInts)
        {
            int randomNumber = UnityEngine.Random.Range(rangeStart, rangeEnd);

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

    public static Vector3[] DeviateAllNormals(Vector3[] normals, float deviationAngleDegrees, bool RandomRotationDeviation = false)
    {
        // Initialize arrays to store the angle deviations and degree intervals
        float[] angleDeviations = new float[normals.Length];
        int[] degreeIntervals = new int[11];
        Vector3[] deviatedNormals = new Vector3[normals.Length];
        // Create a list to store the vectors in the interval [12, 15] degrees
        List<Vector3> vectorsInInterval = new List<Vector3>();
        // Deviate all normals and store the angle deviations
        for (int i = 0; i < normals.Length; i++)
        {
            Vector3 normal = normals[i];
            Vector3 deviatedNormal = DeviateNormal(normal, deviationAngleDegrees, RandomRotationDeviation);
            deviatedNormals[i] = deviatedNormal;
            /*
            float angle = Vector3.Angle(deviatedNormal, normal.normalized);
            angleDeviations[i] = angle;
           
            // Increment the count for the corresponding degree interval
            int degreeIntervalIndex = Mathf.Clamp((int)((Mathf.Abs(angle - deviationAngleDegrees)) / 3), 0, 10);
            degreeIntervals[degreeIntervalIndex]++;

            // Check if the angle deviation is in the interval [12, 15] degrees
            if (angle >= 12 && angle <= 15)
            {
                vectorsInInterval.Add(normal);
            }
            */
        }
        /*
        // Calculate the distribution of degree intervals and concatenate in a single string
        string distributionString = $"Average deviation = {angleDeviations.Average()} target deviation = {deviationAngleDegrees} \n";
        for (int i = 0; i < degreeIntervals.Length; i++)
        {
            string intervalLabel = i == 10 ? "30+ degrees" : $"{i * 3}-{(i + 1) * 3} degrees";
            distributionString += $"{intervalLabel}: {degreeIntervals[i]} \n";
        }

        Debug.Log(distributionString);
        */
        return deviatedNormals;
    }

    public static Vector3 DeviateNormal(Vector3 normal, float deviationAngleDegrees, bool RandomRotationDeviation)
    {
        System.Numerics.Vector3 nor = new(normal.x, normal.y, normal.z);
        float randomRotationDeviationDegrees = RandomRotationDeviation ? UnityEngine.Random.Range(0, 360) : deviationAngleDegrees;
        // Start with a vector aligned with the z-axis
        System.Numerics.Vector3 xAxis = new(1, 0, 0);
        System.Numerics.Vector3 zAxis = new(0, 0, 1);
        System.Numerics.Vector3 deviatedNormal = new System.Numerics.Vector3(0, 0, 1);

        // Compute the normal rotations
        System.Numerics.Matrix4x4 deviationRotation = System.Numerics.Matrix4x4.CreateRotationX(DegreeToRadian(deviationAngleDegrees));
        System.Numerics.Matrix4x4 orientationRotation = System.Numerics.Matrix4x4.CreateRotationZ(DegreeToRadian(randomRotationDeviationDegrees));

        // Apply rotations
        deviatedNormal = System.Numerics.Vector3.Transform(deviatedNormal,  deviationRotation*orientationRotation);

        if (nor.X == 0.0f && nor.Y == 0.0f && nor.Z > 0)
        {
            deviatedNormal = new(deviatedNormal.X, deviatedNormal.Y, deviatedNormal.Z);
        }
        else if (nor.X == 0.0f && nor.Y == 0.0f && nor.Z < 0)
        {
            deviatedNormal = new(-deviatedNormal.X, -deviatedNormal.Y, -deviatedNormal.Z);
        }

        // Compute vector orthogonal to the z-axis and normal
        System.Numerics.Vector3 rotationAxis = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(nor, zAxis));

        float angle = (float)Math.Acos(System.Numerics.Vector3.Dot(nor, zAxis) / (nor.Length() * zAxis.Length()));

        // Rotate deviated normal towards target normal
        System.Numerics.Matrix4x4 rotationMatrix = System.Numerics.Matrix4x4.CreateFromAxisAngle(rotationAxis, -angle);

        System.Numerics.Vector3 result = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Transform(deviatedNormal, rotationMatrix));
        return new Vector3(result.X, result.Y, result.Z);
    }

    // Helper method to convert degrees to radians
    private static float DegreeToRadian(float degree)
    {
        return (float)(degree * (Math.PI / 180.0));
    }
    private static float RadianToDegree(float radian)
    {
        return (float)(radian * (180.0 / Math.PI));
    }

    public static List<GameObject> AddCollidersToMesh(GameObject gameObject)
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        int[] triangles = mesh.triangles;
        List<GameObject> faces = new List<GameObject>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 rayStart = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 p1 = mesh.vertices[triangles[i]];
            Vector3 p2 = mesh.vertices[triangles[i + 1]];
            Vector3 p3 = mesh.vertices[triangles[i + 2]];
            Vector3 p4 = mesh.vertices[triangles[i]] - rayStart * 0.001f;
            Vector3 p5 = mesh.vertices[triangles[i + 1]] - rayStart * 0.001f;
            Vector3 p6 = mesh.vertices[triangles[i + 2]] - rayStart * 0.001f;

            GameObject face = new GameObject("Face Collider " + i);
            face.transform.parent = gameObject.transform;
            Mesh faceMesh = new Mesh();
            faceMesh.vertices = new Vector3[] { p1, p2, p3, p4, p5, p6 };
            faceMesh.triangles = new int[] {
            0, 1, 2,
            2, 3, 0,
            1, 4, 2,
            2, 4, 3,
            3, 4, 5,
            3, 5, 0};
            faceMesh.RecalculateNormals();
            faceMesh.normals[0] = mesh.normals[triangles[i]];
            faceMesh.normals[1] = mesh.normals[triangles[i + 1]];
            faceMesh.normals[2] = mesh.normals[triangles[i + 2]];
            MeshCollider collider = face.AddComponent<MeshCollider>();
            collider.cookingOptions = 0;
            collider.sharedMesh = faceMesh;
            face.isStatic = true;
            faces.Add(face);
        }

        return faces;
    }



    public static Mesh GetNonHitMesh(Mesh mesh, LayerMask layerMask)
    {
        List<Vector3> newVertices = new List<Vector3>();
        Vector3 rayStart1 = new Vector3(0.01f, 1.0f, -0.01f);
        Vector3 rayStart2 = new Vector3(-0.01f, 1.0f, 0.01f);

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vector3 p1 = mesh.vertices[mesh.triangles[i]];
            Vector3 p2 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 p3 = mesh.vertices[mesh.triangles[i + 2]];

            Vector3 direction1 = DirectionToPointToTriangle(rayStart1, p1, p2, p3);
            Vector3 direction2 = DirectionToPointToTriangle(rayStart2, p1, p2, p3);

            AddVerticesIfRaycastHit(rayStart1, direction1, 20f, p1, p2, p3, newVertices);
            AddVerticesIfRaycastHit(rayStart2, direction2, 20f, p1, p2, p3, newVertices);
        }

        if (newVertices.Count < 1)
        {
            return new Mesh();
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = GetTrianglesWithSubsetVertices(mesh, newVertices).ToArray();
        newMesh.RecalculateNormals();
        newMesh.name = mesh.name;

        return newMesh;
    }

    private static void AddVerticesIfRaycastHit(Vector3 rayStart, Vector3 direction, float maxDistance, Vector3 p1, Vector3 p2, Vector3 p3, List<Vector3> newVertices)
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(rayStart, direction, out hitInfo, maxDistance))
        {
            MeshCollider meshCollider = hitInfo.collider as MeshCollider;
            Vector3[] colliderVertices = meshCollider.sharedMesh.vertices;

            if (colliderVertices.Contains(p1) || colliderVertices.Contains(p2) || colliderVertices.Contains(p3))
            {
                if (!newVertices.Contains(p1)) newVertices.Add(p1);
                if (!newVertices.Contains(p2)) newVertices.Add(p2);
                if (!newVertices.Contains(p3)) newVertices.Add(p3);
            }
        }
    }

    // DirectionToPointToTriangle and GetTrianglesWithSubsetVertices methods should remain the same




public static List<int> GetTrianglesWithSubsetVertices(Mesh originalMesh, List<Vector3> newVertices)
    {
        Dictionary<Vector3, int> vertexIndices = new Dictionary<Vector3, int>();
        for (int i = 0; i < newVertices.Count; i++)
        {
            vertexIndices[newVertices[i]] = i;
        }

        List<int> newTriangles = new List<int>();

        int[] originalTriangles = originalMesh.triangles;
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            Vector3 v1 = originalMesh.vertices[originalTriangles[i]];
            Vector3 v2 = originalMesh.vertices[originalTriangles[i + 1]];
            Vector3 v3 = originalMesh.vertices[originalTriangles[i + 2]];

            if (vertexIndices.ContainsKey(v1) && vertexIndices.ContainsKey(v2) && vertexIndices.ContainsKey(v3))
            {
                newTriangles.Add(vertexIndices[v1]);
                newTriangles.Add(vertexIndices[v2]);
                newTriangles.Add(vertexIndices[v3]);
            }
        }
        return newTriangles;
    }

    public static Vector3 RayDirectionToTriangle(Vector3 rayStart, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Check for collinear points
        if (Vector3.Cross(p2 - p1, p3 - p1).normalized == Vector3.zero)
        {
            Debug.Log(Vector3.Cross(p2 - p1, p3 - p1).x);
            Debug.Log("The input points p1, p2, and p3 are collinear and cannot define a plane.");
            return (p1.normalized - rayStart.normalized).normalized;
        }

        Vector3 N = Vector3.Cross(p2 - p1, p3 - p1).normalized;
        Vector3 direction = p1 - rayStart - Vector3.Dot(p1 - rayStart, N) * N;

        // Check if the ray starting point is on the plane
        if (direction == Vector3.zero)
        {
            throw new ArgumentException("The ray starting point is on the plane defined by p1, p2, and p3.");
        }

        return direction.normalized;
    }

    public static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge0 = p2 - p1;
        Vector3 edge1 = p3 - p1;
        Vector3 v0 = p1 - point;

        float a = Vector3.Dot(edge0, edge0);
        float b = Vector3.Dot(edge0, edge1);
        float c = Vector3.Dot(edge1, edge1);
        float d = Vector3.Dot(edge0, v0);
        float e = Vector3.Dot(edge1, v0);

        float det = a * c - b * b;
        float s = b * e - c * d;
        float t = b * d - a * e;

        if (s + t < det)
        {
            if (s < 0.0f)
            {
                if (t < 0.0f)
                {
                    if (d < 0.0f)
                    {
                        s = Mathf.Clamp(-d / a, 0.0f, 1.0f);
                        t = 0.0f;
                    }
                    else
                    {
                        s = 0.0f;
                        t = Mathf.Clamp(-e / c, 0.0f, 1.0f);
                    }
                }
                else
                {
                    s = 0.0f;
                    t = Mathf.Clamp(-e / c, 0.0f, 1.0f);
                }
            }
            else if (t < 0.0f)
            {
                s = Mathf.Clamp(-d / a, 0.0f, 1.0f);
                t = 0.0f;
            }
            else
            {
                float invDet = 1.0f / det;
                s *= invDet;
                t *= invDet;
            }
        }
        else
        {
            if (s < 0.0f)
            {
                float tmp0 = b + d;
                float tmp1 = c + e;
                if (tmp1 > tmp0)
                {
                    float numer = tmp1 - tmp0;
                    float denom = a - 2 * b + c;
                    s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                    t = 1 - s;
                }
                else
                {
                    t = Mathf.Clamp(-e / c, 0.0f, 1.0f);
                    s = 0.0f;
                }
            }
            else if (t < 0.0f)
            {
                if (a + d > b + e)
                {
                    float numer = c + e - b - d;
                    float denom = a - 2 * b + c;
                    s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                    t = 1 - s;
                }
                else
                {
                    s = Mathf.Clamp(-e / c, 0.0f, 1.0f);
                    t = 0.0f;
                }
            }
            else
            {
                float numer = c + e - b - d;
                float denom = a - 2 * b + c;
                s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                t = 1 - s;
            }
        }
        return p1 + s * edge0 + t * edge1;
    }
    public static Vector3 DirectionToPointToTriangle(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 closestPoint = ClosestPointOnTriangle(point, p1, p2, p3);
        return (closestPoint - point).normalized;
    }


    public static IEnumerator WaitForNextFixedUpdate()
    {
        Debug.Log("Waiting for next fixed update...");
        yield return new WaitForFixedUpdate();
        Debug.Log("Next fixed update!");
    }


public static bool skipObject(List<string> objFiles, string path, int counter)
{
    if (!Directory.Exists(path) || counter < 0 || counter >= objFiles.Count)
    {
        return false;
    }
    string[] subCategories = Directory.GetDirectories(path);
    bool skip = false;
    foreach (string subCategory in subCategories)
    {
        string filePath = subCategory + "/" + Path.GetFileNameWithoutExtension(objFiles[counter]) + "/" + Path.GetFileName(objFiles[counter]);
        if (File.Exists(filePath))
        {
            skip = true;
        }
        else
        {
            skip = false;
            break;
        }
    }
    return skip;
}




    public static void SaveRandomStateToFile(UnityEngine.Random.State state, string path)
    {
        path += "/rng_states.json";
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        string json = JsonUtility.ToJson(state);
        File.WriteAllText(path, json);
    }

    public static UnityEngine.Random.State LoadRandomStateFromFileOrInit(string path, int defaultSeed)
    {
        if (Directory.Exists(path) && File.Exists(path+"/rng_states.json"))
        {
            string json = File.ReadAllText(path+"/rng_states.json");
            UnityEngine.Random.State savedState = JsonUtility.FromJson<UnityEngine.Random.State>(json);
            UnityEngine.Random.state = savedState;
        }
        else
        {
            UnityEngine.Random.InitState(defaultSeed);
        }
        return UnityEngine.Random.state;
    }

}

