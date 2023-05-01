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

    private string generatorName = "RandomRotatedNormalObjectsDataset";
    private static string pathToDataset = "/mnt/VOID/projects/shape_descriptors_benchmark/Dataset/";
    private static string fileNameOfNewObj = "NewObj";
    private static int numberOfObjects = 999999999;//Number of objects that will be created.
    private static int clusterSize = 10;// Set cluster size if necessary
    private static List<GameObject> objects = new();
    private GameObject obj;
    private static List<string> objFiles;
    private ObjHandler objhandler;
    public Material wood, bricks1;
    private static float timer;
    private static int counter = 0;
    private static Mesh mesh;

    private UnityEngine.Random.State randomState;


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
                break;
            case "ResizedObjectsDatset"://done
                CreateResizedObjectsDatset("ResizedObjects2", "NewRecalculatedNormals");
                break;
            case "MirroredObjectsDatset"://done
                CreateMirroredObjectsDatset("MirroredObjectsDatset2", "NewRecalculatedNormals");
                break;
            case "RotatedObjectsDatset"://done
                CreateRotatedObjectsDatset("RotatedObjectsDatset2", "NewRecalculatedNormals");
                break;
            case "MovedObjectsDataset"://done
                CreateMovedObjectsDataset("MovedObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "ClusteredObjectsDataset"://done
                CreateClusteredObjectsDataset("ClusteredObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "RippledObjectsDataset"://done
                CreateRippledObjectsDataset("RippledObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "TwistedObjectsDataset": //done
                CreateTwistedObjectsDataset("TwistedObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "RandomVertexDisplacedObjectsDataset"://done
                CreateRandomVertexDisplacementDataset("RandomVertexDisplacedObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "RandomRotatedNormalObjectsDataset"://done
                CreateRandomRotatedNormalObjectsDataset("RandomRotatedNormalObjectsDataset2", "NewRecalculatedNormals");
                break;
            case "RayCastedObjects"://done
                CreateRayCastedObjects("RayCastedObjects2", "NewRecalculatedNormals");
                break;
            case "FalsePostiveTestDataset"://done
                CreateFalsePostiveTestDataset("FalsePostiveTestDataset2", "NewRecalculatedNormals");
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
                    Utilities.RemoveGameObjects(objects);
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


    public void CreateRayCastedObjects(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset, true);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            mesh = objhandler.LoadMesh(objFiles[counter]);
            mesh.RecalculateNormals();

            obj = Utilities.createGameObjectFromMesh(mesh);
            List<GameObject> faceColliders = Utilities.AddCollidersToMesh(obj);
            Mesh newMesh = Utilities.GetNonHitMesh(mesh, 10);
            Utilities.RemoveGameObjects(faceColliders);
            //Camera.main.transform.position = new(0.0f, 0.0f, 0.5f);
            //Camera.main.transform.LookAt(obj.transform);
            objhandler.saveToFile(new MeshData(newMesh), mesh.vertices);
            GameObject.Destroy(obj);
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
        Application.Quit();
    }


    public void CreateClusteredObjectsDataset(String saveTo, String loadFrom)
    {
        UnityEngine.Random.InitState(1);
        timer = -2;
        objhandler = new(saveTo, pathToDataset, true);
        Utilities.addFloorToScene(25, 25);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        while(counter < objFiles.Count) {
            string filePath = pathToDataset + saveTo + "/" +clusterSize.ToString()+ "/" + Path.GetFileNameWithoutExtension(objFiles[counter]) + "/" + Path.GetFileName(objFiles[counter]);
            if (File.Exists(filePath)) counter++;
            else break;
        }
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
        counter++;
        timer = 0;
    }

    public void CreateRandomRotatedNormalObjectsDataset(string saveTo, string loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> deltaAngles = new List<float>() { 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 30.0f, 35.0f, 40.0f, 45.0f };
        for (int y = 0; y < objFiles.Count; y++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
            mesh.RecalculateNormals();
            deltaAngles.ForEach(deltaAngle =>
                {
                    float randomDeltaAngle = UnityEngine.Random.Range(0, 360);
                    Vector3[] newNormals = Utilities.DeviateAllNormals(mesh.normals, deltaAngle, randomDeltaAngle);
                    mesh.SetNormals(newNormals);
                    objhandler.saveToFile(new MeshData(mesh), mesh.vertices, deltaAngle.ToString());
                });
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();

    }



public void CreateRandomVertexDisplacementDataset(String saveTo, String loadFrom)
{
    randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
    objhandler = new ObjHandler(saveTo, pathToDataset);
    objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
    List<float> displacementRanges = new List<float>() { 0.0001f, 0.0002f, 0.0005f, 0.0010f, 0.0015f, 0.0020f, 0.0025f, 0.0030f, 0.004f, 0.005f };
    int[] rangeCounters = new int[10];


    for (int y = 0; y < objFiles.Count; y++)
    {
        if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }


        Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
        mesh.RecalculateNormals();
        Vector3[] originalVertices = mesh.vertices;
        Vector3[] normals = mesh.normals;


        foreach (float displacementRange in displacementRanges)
        {
            Vector3[] displacedVertices = new Vector3[originalVertices.Length];
            for (int i = 0; i < originalVertices.Length; i++)
            {
                (float gaussianNoise, int rangeIndex) = GaussianNoise(0, 1, -1, 1);
                rangeCounters[rangeIndex]++;
                displacedVertices[i] = originalVertices[i] + (displacementRange * normals[i] * gaussianNoise);
            }
            mesh.vertices = displacedVertices;
            mesh.RecalculateNormals();
            objhandler.saveToFile(new MeshData(mesh), mesh.vertices, displacementRange.ToString());
        }
        if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
        randomState = UnityEngine.Random.state;
        counter++;
    }


    for (int i = 0; i < rangeCounters.Length; i++)
    {
        Debug.Log($"Range {i + 1}: {rangeCounters[i]}");
    }


    objhandler.CompleteWriting();
}


private (float, int) GaussianNoise(float mean, float stdDev, float minRange, float maxRange)
{
    float noise;
    int rangeIndex;
    do
    {
        float u1 = UnityEngine.Random.Range(0f, 1f);
        float u2 = UnityEngine.Random.Range(0f, 1f);
        float z1 = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        noise = mean + stdDev * z1;
        rangeIndex = Mathf.FloorToInt((noise - minRange) / ((maxRange - minRange) / 10));
    } while (noise < minRange || noise > maxRange || rangeIndex < 0 || rangeIndex >= 10);


    return (noise, rangeIndex);
}




    public void CreateResizedObjectsDatset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> floatList = new List<float>() {0.5f, 0.9f, 1.1f, 2.0f };
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
            foreach (float scale in floatList)
            {
                Mesh resizedMesh = Utilities.TransformMesh(Utilities.Copy(mesh), scale, scale, scale);
                objhandler.saveToFile(new MeshData(resizedMesh), mesh.vertices, scale.ToString());
            }
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }

    public void CreateRippledObjectsDataset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> freqList = new List<float>() { 40.0f, 40.0f, 40.0f, 40.0f, 40.0f, 40.0f };
        List<float> multiplierList = new List<float>() { 0.005f, 0.010f, 0.015f, 0.020f, 0.025f, 0.030f };
         for (int i = 0; i < objFiles.Count; i++)
        {
            if (counter > 2) break;
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
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
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }

    public void CreateTwistedObjectsDataset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> degreeList = new List<float>() { 2.0f, 4.0f, 6.0f, 8.0f, 10.0f };
         for (int i = 0; i < objFiles.Count; i++)
        {
            if (counter > 2) break;
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
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
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }

    public void CreateMirroredObjectsDatset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
            Mesh resizedMesh = Utilities.TransformMesh(Utilities.Copy(mesh), -1.0f);
            objhandler.saveToFile(new MeshData(resizedMesh), mesh.vertices, "-1.0f");

            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }

    public void CreateMovedObjectsDataset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter+1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[i]);

            // Move small distance
            Vector3 direction = UnityEngine.Random.onUnitSphere;
            Mesh movedMesh = Utilities.MoveMesh(Utilities.Copy(mesh), direction, 0.2f);
            objhandler.saveToFile(new MeshData(movedMesh), mesh.vertices, "SmallMove-0.2f");

            // Move big distance
            direction = UnityEngine.Random.onUnitSphere;
            movedMesh = Utilities.MoveMesh(Utilities.Copy(mesh), direction, 10.0f);
            objhandler.saveToFile(new MeshData(movedMesh), mesh.vertices, "BigMove-10.0f");

      
            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        }
        objhandler.CompleteWriting();
    }


    public void CreateFalsePostiveTestDataset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset, true);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
            // Move small distance
            int randomNumber = Utilities.GenerateRandomNumbers(0, objFiles.Count, 1, counter)[0];
            Mesh randomMesh = objhandler.LoadMesh(objFiles[randomNumber]);
            randomMesh.name = mesh.name;
            randomMesh.RecalculateNormals();
            objhandler.saveToFile(new MeshData(randomMesh), mesh.vertices);

            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }

    public void CreateRotatedObjectsDatset(String saveTo, String loadFrom)
    {
        randomState = Utilities.LoadRandomStateFromFileOrInit(pathToDataset + saveTo, 1);
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        for (int i = 0; i < objFiles.Count; i++)
        {
            if (Utilities.skipObject(objFiles, pathToDataset + saveTo, counter + 1)) { counter++; continue; }

            Mesh mesh = objhandler.LoadMesh(objFiles[counter]);
            List<string> mapping = new List<string>();
            for (int y = 0; y < mesh.vertices.Length; y++) mapping.Add(y.ToString());

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

            if (counter != 0) Utilities.SaveRandomStateToFile(randomState, pathToDataset + saveTo);
            randomState = UnityEngine.Random.state;
            counter++;
        };
        objhandler.CompleteWriting();
    }


    //DEPRICATED, see blender script overlapping objects    Helper method for printing out a list
    public void CreateCombinedObjectsDataset(String saveTo, String loadFrom)
    {
        UnityEngine.Random.InitState(1);
        objhandler = new(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        objFiles.ForEach(objFile =>
        {
            //if (counter > 3) return;
            List<Mesh> meshes = new();
            //adds this mesh to a list and four random other meshes that is not the same a the first one
            meshes.Add(objhandler.LoadMesh(objFile));
            Utilities.GenerateRandomNumbers(0, (objFiles.Count - 1), (objFiles.Count) < numberOfObjects ? (objFiles.Count - 1) : (numberOfObjects - 1), counter).ForEach(randomIndex => meshes.Add(objhandler.LoadMesh(objFiles[randomIndex])));
            List<GameObject> gameobjects = Utilities.createGameObjectsFromMeshes(meshes);
            Mesh combinedMesh = Utilities.combineMeshes(gameobjects);
            Utilities.RemoveGameObjects(gameobjects);
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
