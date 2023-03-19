using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AI;
using System.Security.AccessControl;
using System.Linq;

public class AutomatedOcclusionProcess
{


    [MenuItem("Custom Tools/Run Automated Process")]
    public static void RunAutomatedProcess()
    {
        GameObject[] oldO = GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.name.Contains("Face Collider")).ToArray();
        foreach (GameObject o in oldO) Object.DestroyImmediate(o);
        
        string generatorName = "FrustrumCullingObjectsDataset";
        string loadFrom = "NewRecalculatedNormals";
        string pathToDataset = "/Users/haakongunnarsli/masterprosjekt/dataset/";

        int numberOfObjects = 1;//Number of objects that will be created.
        List<GameObject> objects = new();
        GameObject obj;
        Material wood, bricks1;
        int counter = 0;

        List<string> objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        ObjHandler objhandler = new ObjHandler(generatorName, loadFrom);

        Mesh mesh = PopulateScene(objFiles, objhandler, generatorName, loadFrom);
      // Bake occlusion data
        BakeOcclusionCullingData();

        // Perform runtime functions
        PerformRuntimeFunctions(objhandler, mesh);

        // Repeat the process as needed (you can use a loop or adjust the code to your requirements)
    }

    private static Mesh PopulateScene(List<string> objFiles, ObjHandler objhandler, string saveTo, string loadFrom)
    {
        int counter = 0;
        Mesh newMesh = new();
        objFiles.ForEach(objFile =>
        {
            if (counter >= 1) return;//To be removed in final version
            Mesh mesh = objhandler.LoadMesh(objFiles[4]);
            mesh = Utilities.TransformMesh(mesh, 1000, 1000, 1000);
            GameObject obj = Utilities.createGameObjectFromMesh(mesh);
            Utilities.SplitObjectIntoFaceSizedColliderObjects(obj);

            Camera.main.transform.position = new(0.0f, 0.0f, 10f);
            Camera.main.transform.LookAt(obj.transform);


            //Mesh newMesh = Utilities.GetNonHitMesh(mesh, 10);
            //objhandler.saveToFile(new MeshData(newMesh), mesh.vertices);
            //GameObject.DestroyImmediate(obj);
            counter++;
        });
        return newMesh;
    }

    private static void BakeOcclusionCullingData()
    {
        StaticOcclusionCulling.Compute();
        StaticOcclusionCulling.smallestOccluder = 0.001f;
        Debug.Log("Occlusion Culling data has been baked!");
        StaticOcclusionCullingVisualization.showOcclusionCulling = true;
        StaticOcclusionCullingVisualization.showGeometryCulling = true;

    }

    private static void PerformRuntimeFunctions(ObjHandler objhandler, Mesh mesh)
    {

        //objhandler.saveToFile(new MeshData(mesh), mesh.vertices);
        //objhandler.CompleteWriting();
        Debug.Log("done");
    }





}
