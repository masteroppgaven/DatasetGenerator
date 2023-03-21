using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter
using System.IO;
using UnityEngine.AI;
using System.Linq;
//Add substance for unity
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.XR;
using System.Threading.Tasks;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Data;
using System.Text;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using UnityEditor;

public class Controller : MonoBehaviour
{
    //Sett generator navn lik det datasettet du ønsker å kjøre.

    private string generatorName = "ClusteredObjectsDataset"; 
    private static string pathToDataset = "../../dataset/";//"/mnt/VOID/projects/shape_descriptors_benchmark/Dataset/"
    private static string fileNameOfNewObj = "NewObj";
    private static int numberOfObjects = 5;//Number of objects that will be created.
    private static int clusterSize = 100;// Set cluster size if necessary
    private static List<GameObject> objects = new();
    private GameObject obj;
    private static List<string> objFiles;
    private ObjHandler objhandler;
    public Material wood, bricks1;
    private static float timer;
    private static int counter = 0;
    private static Mesh mesh;

    //private static GameObject ob;

    //private float updateTime = 1f; // update every 1 second
    // 

    // Start is called before the first frame update
    void Start()
    {
        switch (generatorName)
        {
            case "UnfilteredObjectsWithRecalulatedNormals":
                CreateUnfilteredObjectsWithRecalulatedNormals("NewRecalculatedNormals8", "Backup");
                UnityEngine.Random.InitState(1);
                break;
            case "ResizedObjectsDatset":
                CreateResizedObjectsDatset("ResizedObjects", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(3);
                break;
            case "MirroredObjectsDatset":
                CreateMirroredObjectsDatset("MirroredObjectsDatset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(4);
                break;
            case "RotatedObjectsDatset":
                CreateRotatedObjectsDatset("RotatedObjectsDatset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(5);
                break;
            case "MovedObjectsDataset":
                CreateMovedObjectsDataset("MovedObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(6);
                break;
            case "ClusteredObjectsDataset":
                CreateClusteredObjectsDataset("ClusteredObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(7);
                break;
            case "RippledObjectsDataset":
                CreateRippledObjectsDataset("RippledObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(8);
                break;
            case "TwistedObjectsDataset":
                CreateTwistedObjectsDataset("TwistedObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(9);
                break;
            case "RandomVertexDisplacedObjectsDataset":
                CreateRandomVertexDisplacementDataset("RandomVertexDisplacedObjectsDataset2", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(10);
                break;
            case "RandomRotatedNormalObjectsDataset":
                CreateRandomRotatedNormalObjectsDataset("RandomRotatedNormalObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(11);
                break;
            case "FrustrumCullingObjectsDataset":
                CreateFrustrumCullingObjectsObjectsDataset("FrustrumCullingObjectsDataset", "NewRecalculatedNormals");
                UnityEngine.Random.InitState(12);
                break;
            default:
                //string pathToPreview = "/System/Applications/Preview.app/Contents/MacOS/Preview";
                //System.Diagnostics.Process.Start("open", "-a " + pathToPreview + " " + pathToDataset + saveTo + fileNameOfNewObj + ".obj");
                //________________________________________________________________
                //Exit application
                Application.Quit();
                break;

        }
    }
    private void FixedUpdate()
    {
        switch (generatorName)
        {
            case "ClusteredObjectsDataset":
                if (timer > 3.0f && timer < 15.0f)
                {
                    timer += 15.0f;
                    List<GameObject> l = new();
                    l.Add(objects[0]);
                    Mesh mappingMesh = Utilities.combineMeshes(l);
                    Vector3[] ov = mappingMesh.vertices;
                    Mesh combinedMesh = Utilities.combineMeshes(objects);
                    objhandler.saveObjectsPositionAndRotation(objects);//Because we are saving position and orientation will this function take vare of hiding and destroying objects.
                    objhandler.saveToFile(new MeshData(combinedMesh), ov, clusterSize.ToString());
                    Utilities.removeGameObjects(objects);
                    CreateClusteredObjectsDatasetHelper();
                    break;
                }
                timer += Time.fixedDeltaTime;
                List<Rigidbody> rigidbodies = new();
                objects.ForEach(obj => rigidbodies.Add(obj.GetComponent<Rigidbody>()));
                if (timer < 1.5f) Utilities.addPhysicsForClusterMeshesDataset(rigidbodies, -5.0f + (timer * 3));
                break;
            default:
                //string pathToPreview = "/System/Applications/Preview.app/Contents/MacOS/Preview";
                //System.Diagnostics.Process.Start("open", "-a " + pathToPreview + " " + pathToDataset + saveTo + fileNameOfNewObj + ".obj");
                //________________________________________________________________
                //Exit applications
                break;
        }
    }
    public void CreateClusteredObjectsDataset(String saveTo, String loadFrom)
    {
        timer = -2;
        objhandler = new(saveTo, pathToDataset, true);
        Utilities.addFloorToScene(25, 25);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        CreateClusteredObjectsDatasetHelper();
    }

    public void CreateClusteredObjectsDatasetHelper()
    {
        //Makes sure that the counter is not out of bounds
        if (counter >= objFiles.Count || counter >= numberOfObjects)
        {
            objhandler.CompleteWriting();
            Application.Quit();
            return;
        }
        List<Mesh> meshes = new();
        //adds this mesh to a list and four random other meshes that is not the same a the first one
        meshes.Add(objhandler.LoadMesh(objFiles[counter]));
        Utilities.GenerateRandomNumbers(0, (objFiles.Count - 1), (objFiles.Count) < clusterSize ? (objFiles.Count - 1) : (clusterSize - 1), counter)
            .ForEach(randomIndex => meshes
            .Add(objhandler.LoadMesh(objFiles[randomIndex])));
        objects = Utilities.createGameObjectsFromMeshes(meshes, true, new Vector3(0.0f, 1.0f, 0.0f));
        objects.ForEach(obj =>
        {
            Utilities.addRigidbody(obj, true);
            Utilities.addMeshCollider(obj);
            obj.GetComponent<MeshRenderer>().material = bricks1;
        });
        Debug.Log(objects);
        counter++;
        timer = 0;
    }



    public void CreateRandomRotatedNormalObjectsDataset(string saveTo, string loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> deltaAngles = new List<float>() { 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 30.0f, 35.0f, 40.0f, 45.0f };
        objFiles.ForEach(objFile =>
        {
            if (counter > 1) return; // To be removed in final version
            Mesh mesh = objhandler.LoadMesh("/Users/haakongunnarsli/masterprosjekt/Dataset/RandomRotatedNormalObjectsDataset/capsule.obj");
            mesh.RecalculateNormals();
            deltaAngles.ForEach(deltaAngle =>
            {
                Vector3[] newNormals = Utilities.DeviateAllNormals(mesh.normals, deltaAngle, deltaAngle);
                counter++;
                mesh.SetNormals(newNormals);
                objhandler.saveToFile(new MeshData(mesh), mesh.vertices, "test2");
            });
            counter++;
        });
        objhandler.CompleteWriting();

    }

    public void CreateFrustrumCullingObjectsObjectsDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        objFiles.ForEach(objFile =>
        {
            if (counter >= 1) return;//To be removed in final version
            mesh = objhandler.LoadMesh(objFiles[4]);
            //mesh = Utilities.TransformMesh(mesh, 1000, 1000, 1000);
            obj = Utilities.createGameObjectFromMesh(mesh);
            Utilities.AddCollidersToMesh(obj);

            Camera.main.transform.position = new(0.0f, 0.0f, 0.5f);
            Camera.main.transform.LookAt(obj.transform);

            //Mesh newMesh = Utilities.GetNonHitMesh(mesh, 10);
            //objhandler.saveToFile(new MeshData(newMesh), mesh.vertices);
            //GameObject.Destroy(obj);
            counter++;
        });
        objhandler.CompleteWriting();
    }


    public void CreateRandomVertexDisplacementDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> displacementRanges = new List<float>() { 0.0001f, 0.0002f, 0.0005f, 0.0010f, 0.0015f, 0.0020f, 0.0025f, 0.0030f, 0.004f, 0.005f };
        objFiles.ForEach(objFile =>
        {
            Mesh mesh = objhandler.LoadMesh(objFile);
            mesh.RecalculateNormals();
            Vector3[] originalVertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            foreach (float displacementRange in displacementRanges)
            {
                Vector3[] displacedVertices = new Vector3[originalVertices.Length];
                for (int i = 0; i < originalVertices.Length; i++)
                {
                    displacedVertices[i] = originalVertices[i] + (displacementRange * normals[i] * UnityEngine.Random.Range(-1f, 1f));
                }
                mesh.vertices = displacedVertices;
                mesh.RecalculateNormals();
                objhandler.saveToFile(new MeshData(mesh), mesh.vertices, displacementRange.ToString());
            }
            counter++;
        });
        objhandler.CompleteWriting();
    }


    public void CreateResizedObjectsDatset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> floatList = new List<float>() { 1.1f, 2.0f };
        objFiles.ForEach(objFile =>
        {
            if (counter > 1) return; // To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFile);
            foreach (float scale in floatList)
            {
                Mesh resizedMesh = Utilities.TransformMesh(Utilities.Copy(mesh), scale, scale, scale);
                objhandler.saveToFile(new MeshData(resizedMesh), mesh.vertices, scale.ToString());
            }
            counter++;
        });
        objhandler.CompleteWriting();
    }

    public void CreateRippledObjectsDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> freqList = new List<float>() { 40.0f, 40.0f, 40.0f, 40.0f, 40.0f, 40.0f };
        List<float> multiplierList = new List<float>() { 0.005f, 0.010f, 0.015f, 0.020f, 0.025f, 0.030f };
        objFiles.ForEach(objFile =>
        {
            Mesh mesh = objhandler.LoadMesh(objFile);
            foreach (var p in freqList.Zip(multiplierList, (freq, multiplier) => new { freq, multiplier }))
            {
                GameObject go = Utilities.createGameObjectFromMesh(mesh);
                go.AddComponent<RippleDeformer>();
                RippleDeformer script = go.GetComponent<RippleDeformer>();
                script.Frequency = p.freq;
                script.PeakMultiplier = p.multiplier;
                script.Run();
                objhandler.saveToFile(new MeshData(go.GetComponent<MeshFilter>().mesh), mesh.vertices, p.multiplier.ToString());
                Destroy(go);
            }
            counter++;
        });
        objhandler.CompleteWriting();
    }

    public void CreateTwistedObjectsDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> degreeList = new List<float>() { 2.0f, 4.0f, 6.0f, 8.0f, 10.0f };
        objFiles.ForEach(objFile =>
        {
            Mesh mesh = objhandler.LoadMesh(objFile);
            foreach (float d in degreeList)
            {
                GameObject go = Utilities.createGameObjectFromMesh(mesh);
                go.AddComponent<TwistDeformer>();
                TwistDeformer script = go.GetComponent<TwistDeformer>();
                script.angleOfTwist = d;
                script.Run();
                objhandler.saveToFile(new MeshData(go.GetComponent<MeshFilter>().mesh), mesh.vertices, d.ToString());
                Destroy(go);
            }
            counter++;
        });
        objhandler.CompleteWriting();
    }

    public void CreateMirroredObjectsDatset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        objFiles.ForEach(objFile =>
        {
            if (counter > 1) return; // To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFile);
            Mesh resizedMesh = Utilities.TransformMesh(Utilities.Copy(mesh), -1.0f);
            objhandler.saveToFile(new MeshData(resizedMesh), mesh.vertices, "-1.0f");
            counter++;
        });
        objhandler.CompleteWriting();
    }

    public void CreateMovedObjectsDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        int counter = 0;
        objFiles.ForEach(objFile =>
        {
            Mesh mesh = objhandler.LoadMesh(objFile);
            // Move small distance
            Vector3 direction = UnityEngine.Random.onUnitSphere;
            Mesh movedMesh = Utilities.MoveMesh(Utilities.Copy(mesh), direction, 0.2f);
            objhandler.saveToFile(new MeshData(movedMesh), mesh.vertices, "SmallMove - 0.2f");

            // Move big distance
            direction = UnityEngine.Random.onUnitSphere;
            movedMesh = Utilities.MoveMesh(Utilities.Copy(mesh), direction, 10.0f);
            objhandler.saveToFile(new MeshData(movedMesh), mesh.vertices, "BigMove - 10.0f");

            counter++;
        });
        objhandler.CompleteWriting();
    }


    public void CreateRotatedObjectsDatset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        int counter = 0;
        objFiles.ForEach(objFile =>
        {
            if (counter > 2) return;//To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFile);
            List<string> mapping = new List<string>();
            for (int i = 0; i < mesh.vertices.Length; i++) mapping.Add(i.ToString());

            // Rotate randomly along X axis
            Mesh rotatedMesh = Utilities.RandomRotateMesh(Utilities.Copy(mesh), 2.0f, -1f, -1f);
            objhandler.saveToFile(new MeshData(rotatedMesh), mesh.vertices, "X");

            // Rotate randomly along Y axis
            rotatedMesh = Utilities.RandomRotateMesh(Utilities.Copy(mesh), -1f, 2.0f, -1f);
            objhandler.saveToFile(new MeshData(rotatedMesh), mesh.vertices, "Y");

            // Rotate randomly along Z axis
            rotatedMesh = Utilities.RandomRotateMesh(Utilities.Copy(mesh), -1f, -1f, 2.0f);
            objhandler.saveToFile(new MeshData(rotatedMesh), mesh.vertices, "Z");

            // Rotate randomly along all axes
            rotatedMesh = Utilities.RandomRotateMesh(Utilities.Copy(mesh), 2.0f, 2.0f, 2.0f);
            objhandler.saveToFile(new MeshData(rotatedMesh), mesh.vertices, "XYZ");

            counter++;
        });
        objhandler.CompleteWriting();
    }


    //DEPRICATED, see blender script overlapping objects    Helper method for printing out a list
    public void CreateCombinedObjectsDataset(String saveTo, String loadFrom)
    {
        objhandler = new(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        int counter = 0;
        objFiles.ForEach(objFile =>
        {
            //if (counter > 3) return;
            List<Mesh> meshes = new();
            //adds this mesh to a list and four random other meshes that is not the same a the first one
            meshes.Add(objhandler.LoadMesh(objFile));
            Utilities.GenerateRandomNumbers(0, (objFiles.Count - 1), (objFiles.Count) < numberOfObjects ? (objFiles.Count - 1) : (numberOfObjects - 1), counter).ForEach(randomIndex => meshes.Add(objhandler.LoadMesh(objFiles[randomIndex])));
            List<GameObject> gameobjects = Utilities.createGameObjectsFromMeshes(meshes);
            Mesh combinedMesh = Utilities.combineMeshes(gameobjects);
            Utilities.removeGameObjects(gameobjects);
            objhandler.saveToFile(new MeshData(combinedMesh), meshes[0].vertices);
            counter++;
        });
        objhandler.CompleteWriting();

    }







    //Takes in the google datset and recalulates the normals and saves it.
    //We need to do this because the normals will get recalulated when applying the filters 
    public void CreateUnfilteredObjectsWithRecalulatedNormals(String saveTo, String loadFrom)
    {
        objhandler = new(saveTo, pathToDataset);
        //Creates a list of all .obj files in the dataset folder
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom + "/RecalcRemovedLoose", "*.obj", SearchOption.AllDirectories));
        //List<string> metaFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.pbtxt", SearchOption.AllDirectories)); //get metadata
        //List<string> recalculated = new List<string>(Directory.GetFiles(pathToDataset + saveTo + "/0-100", "*.obj", SearchOption.AllDirectories));
        //For each objfile in objFiles does Loadmesh get called and then WriteMeshToObj
        //Creates a counter that is used to name the new .obj files that always consist of four digits
        objFiles.ForEach(objFile =>
        {

            string newName = counter.ToString().PadLeft(4, '0');
            Mesh mesh = objhandler.LoadMesh(objFile, newName);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            objhandler.saveToFile(new MeshData(mesh));

            /* get metadata
            string metadataDirectory = Path.Combine(pathToDataset, "Metadata");
            if (!Directory.Exists(metadataDirectory))
            {
                Directory.CreateDirectory(metadataDirectory);
            }
            string destinationPath = Path.Combine(metadataDirectory, mesh.name + ".pbtxt");
            File.Copy(metaFiles[counter], destinationPath, true);
            */
            /*
            
            */
            counter++;

        });
        //Utilities.CopyFromTo(recalculated, pathToDataset + saveTo, pathToDataset + "rec");
        objhandler.CompleteWriting();
    }


}


/*

public void CreateRandomRotatedNormalObjectsDataset(string saveTo, string loadFrom)
{
    //objhandler = new(saveTo, pathToDataset, true);
    //ob = GameObject.CreatePrimitive(PrimitiveType.Plane);
    //ob.transform.position = new Vector3(0f, 0f, 0f);
    //Debug.Log("load");
    //lineHolder = new GameObject("Line Holder");

    //Users/haakongunnarsli/masterprosjekt/Dataset/RandomRotatedNormalObjectsDataset/triangle.obj");
    //Users/haakongunnarsli/masterprosjekt/Dataset/RecalculatedNormals/0-100/0000/0000.obj
    //m1 = ob.GetComponent<MeshFilter>().mesh;

    objhandler = new ObjHandler(saveTo, pathToDataset);
    objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
    List<float> deltaAngles = new List<float>() { 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 30.0f, 35.0f, 40.0f, 45.0f };
    objFiles.ForEach(objFile =>
    {
        if (counter > 1) return; // To be removed in final version
        Mesh mesh = objhandler.LoadMesh("/Users/haakongunnarsli/masterprosjekt/Dataset/RandomRotatedNormalObjectsDataset/triangle.obj");
        Utilities.createGameObjectFromMesh(mesh);

        deltaAngles.ForEach(deltaAngle =>
        {
            if (counter >= 1) return;
            mesh.RecalculateNormals();
            Vector3[] newNormals = new Vector3[mesh.normals.Length];
            string savePath = "/Users/haakongunnarsli/masterprosjekt/Dataset/RandomRotatedNormalObjectsDataset/log.txt";

            // Create or overwrite the log file
            StreamWriter logFile = new StreamWriter(savePath, false);

            for (int i = 0; i < mesh.normals.Length; i++)
            {
                Vector3 A = mesh.vertices[i];
                Vector3 B = mesh.normals[i];
                float distance = Vector3.Distance(A, B);

                // Calculate AB vector and perpendicular direction to AB in xz-plane
                Vector3 AB = B - A;

                // float radians = 45f * Mathf.Deg2Rad;
                // float length = Mathf.Sin(radians) / Mathf.Sin(45f * Mathf.Deg2Rad);
                GameObject cyl = Utilities.CreateCylinder(distance, distance);

                Debug.Log($"{A}   {B}    {distance}");

                int index = Utilities.GetRandomHighestVertexIndex(cyl, logFile);

                Debug.Log(cyl.GetComponent<MeshFilter>().mesh.vertices[index] + "before");
                //Adjusting the chosen highest Vertex to have the same distance to origo as AB.

                Vector3[] temp = cyl.GetComponent<MeshFilter>().mesh.vertices;

                Vector3 OToB = new Vector3(temp[index].normalized.x * distance, temp[index].normalized.y * distance, temp[index].normalized.z * distance);
                temp[index] = OToB;
                cyl.GetComponent<MeshFilter>().mesh.vertices = temp;
                Debug.Log($"O-A distance: {Vector3.Distance(new Vector3(0, 0, 0), OToB)}   {cyl.GetComponent<MeshFilter>().mesh.vertices[index]}  normal AdjustedDistance");

                Transform transe = cyl.GetComponent<Transform>();
                temp = temp.Select(vertex => vertex + A).ToArray();
                //temp = temp.Select((vertex, index) => transe.InverseTransformPoint(temp[index])).ToArray();
                cyl.GetComponent<MeshFilter>().mesh.vertices = temp;
                Debug.Log($"A-B distance: {Vector3.Distance(A, temp[index])}  {cyl.GetComponent<MeshFilter>().mesh.vertices[index]}  - B Moved to pos A   {A}");


                //transe.LookAt(B);
                //temp = temp.Select((vertex, index) => transe.InverseTransformPoint(temp[index])).ToArray();

                CombineInstance[] combine = new CombineInstance[1];

                MeshFilter mfTest = cyl.GetComponent<MeshFilter>();
                combine[0].mesh = mfTest.sharedMesh;
                combine[0].transform = mfTest.transform.localToWorldMatrix;

                Mesh test1 = new();
                test1.CombineMeshes(combine, true, true, false);
                cyl.GetComponent<MeshFilter>().mesh = test1;



                //cyl.GetComponent<MeshFilter>().mesh.vertices = temp;
                Debug.Log($"A-B rotation: {Vector3.Distance(A, cyl.GetComponent<MeshFilter>().mesh.vertices[index])}  {cyl.GetComponent<MeshFilter>().mesh.vertices[index]}  - B Moved to pos A   {A}");

                //logFile.WriteLine(string.Join("\n", Enumerable.Range(0, cyl.GetComponent<MeshFilter>().mesh.vertices.Length).Select(i => $"Vertex {i}: {cyl.GetComponent<MeshFilter>().mesh.vertices[i].ToString("F3")}")));



                Vector3 B_prime = cyl.GetComponent<MeshFilter>().mesh.vertices[index];
                Debug.Log(B_prime + "after");
                //GameObject.Destroy(cyl);

                newNormals[i] = B_prime;
                /*
                Vector3 ABxz = Vector3.ProjectOnPlane(AB, Vector3.up);

                // Calculate desired direction of B' based on AB and perpendicular direction in xz-plane
                Vector3 desiredDir = Quaternion.AngleAxis(45f, Vector3.up) * ABxz.normalized;

                // Calculate new position of point B
                Vector3 B_prime = A + desiredDir * (float)distance;
                
                // Calculate angles between A and B, and between A and B'
                double angle2 = Vector3.Angle(B, N);
                double angle3 = Vector3.Angle(B, N_prime);
                double distance2 = Vector3.Distance(A, B_prime);
                // Write log information to file
                logFile.WriteLine($"Index {i}: A = {A}, B_prime = {B_prime} B = {B}, Deltadistance = {distance - distance2}");
                //logFile.WriteLine($"AB = {AB}, ABxz = {ABxz}");
                //logFile.WriteLine($"desiredDir = {desiredDir}");
                //logFile.WriteLine($"B' = {B_prime}");
                logFile.WriteLine($"OAB: {angle2} degrees, NewOAB: {angle3} degrees, Delta angle = {Math.Abs(angle2 - angle3)} degrees");

                //newNormals[i] = B_prime;
                counter++;
            }

            // Close the log file
            logFile.Close();

            counter++;
            mesh.normals = newNormals;
            objhandler.saveToFile(new MeshData(mesh), mesh.vertices, "test2");
        });
        counter++;
    });
    objhandler.CompleteWriting();

}

*/
