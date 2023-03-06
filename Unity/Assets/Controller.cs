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

public class Controller : MonoBehaviour
{
    //Sett generator navn lik det datasettet du ønsker å kjøre.
    private string generatorName = "ClusteredObjectsDataset";
    private static string pathToDataset = "/Users/haakongunnarsli/masterprosjekt/dataset/";
    private static string fileNameOfNewObj = "NewObj";
    private static int numberOfObjects = 20;//Number of objects that will be created.
    private static int clusterSize = 20;// Set cluster size if necessary
    private static List<GameObject> objects = new();
    private static List<string> objFiles;
    private ObjHandler objhandler;
    public Material wood, bricks1;
    private static float timer;
    private static int counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        switch (generatorName)
        {
            case "UnfilteredObjectsWithRecalulatedNormals":
                CreateUnfilteredObjectsWithRecalulatedNormals("RecalculatedNormals", "GoogleDataset");
                break;
            case "CombinedObjectsDataset":
                CreateCombinedObjectsDataset("CombinedObjects2", "RecalculatedNormals");
                break;
            case "ResizedObjectsDatset":
                CreateResizedObjectsDatset("ResizedObjects", "RecalculatedNormals");
                break;
            case "MirroredObjectsDatset":
                CreateMirroredObjectsDatset("MirroredObjectsDatset", "RecalculatedNormals");
                break;
            case "RotatedObjectsDatset":
                CreateRotatedObjectsDatset("RotatedObjectsDatset", "RecalculatedNormals");
                break;
            case "MovedObjectsDataset":
                CreateMovedObjectsDataset("MovedObjectsDataset", "RecalculatedNormals");
                break;
            case "ClusteredObjectsDataset":
                CreateClusteredObjectsDataset("ClusteredObjectsDataset", "RecalculatedNormals");
                break;
            case "RippledObjectsDataset":
                CreateRippledObjectsDataset("RippledObjectsDataset", "RecalculatedNormals");
                break;
            case "TwistedObjectsDataset":
                CreateTwistedObjectsDataset("TwistedObjectsDataset", "RecalculatedNormals");
            break;
            case "RandomDisplacedObjectsDataset":
                CreateRandomDisplacementDataset("RandomDisplacedObjectsDataset", "RecalculatedNormals");
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
                if (timer > 3.0f)
                {
                    List<GameObject> l = new();
                    l.Add(objects[0]);
                    Mesh mappingMesh = Utilities.combineMeshes(l);
                    Vector3[] ov = mappingMesh.vertices;

                    Mesh combinedMesh = Utilities.combineMeshes(objects);
 
                    objhandler.saveToFile(new MeshData(combinedMesh), ov, clusterSize.ToString());
                    Utilities.removeGameObjects(objects);
                    CreateClusteredObjectsDatasetHelper();
                    break;
                }
                timer += Time.fixedDeltaTime;
                List<Rigidbody> rigidbodies = new();
                objects.ForEach(obj => rigidbodies.Add(obj.GetComponent<Rigidbody>()));
                if (timer < 1.5f) Utilities.addPhysicsForClusterMeshesDataset(rigidbodies, -5.0f+(timer*3));
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
        counter++;
        timer = 0;
    }

    public void CreateRandomDisplacementDataset(String saveTo, String loadFrom)
    {
        objhandler = new ObjHandler(saveTo, pathToDataset);
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        List<float> deformationAmounts = new List<float>() { 0.0001f, 0.0002f, 0.0005f, 0.0010f, 0.0015f, 0.0020f, 0.0025f, 0.0030f, 0.004f, 0.005f};
        objFiles.ForEach(objFile =>
        {
            if (counter > 1) return; // To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFile);
            Vector3[] originalVertices = mesh.vertices;
            foreach (float deformationAmount in deformationAmounts)
            {
                mesh.vertices = mesh.vertices.Select((vertex, index) => originalVertices[index] + UnityEngine.Random.insideUnitSphere * deformationAmount).ToArray();
                mesh.RecalculateNormals();
                objhandler.saveToFile(new MeshData(mesh), mesh.vertices, deformationAmount.ToString());
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
        List<float> freqList = new List<float>() { 40.0f, 40.0f, 40.0f, 40.0f, 40.0f, 40.0f};
        List<float> multiplierList = new List<float>() { 0.005f, 0.010f, 0.015f, 0.020f, 0.025f, 0.030f};
        objFiles.ForEach(objFile =>
        {
            if (counter > 1) return; // To be removed in final version
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
            if (counter > 1) return; // To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFile);
            foreach (float d in degreeList)
            {
                GameObject go = Utilities.createGameObjectFromMesh(mesh);
                go.AddComponent<TwistDeformer>();
                TwistDeformer script = go.GetComponent<TwistDeformer>();
                script.angleOfTwist = d;
                script.Run();
                objhandler.saveToFile(new MeshData(go.GetComponent<MeshFilter>().mesh), mesh.vertices, d.ToString());
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
            if (counter > 1) return; // To be removed in final version
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
        objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
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