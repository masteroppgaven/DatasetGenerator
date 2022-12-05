using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter
using System.IO;
using System.Text;
using Random = UnityEngine.Random;

public class Controller : MonoBehaviour
{

    public int numItemsToSpawn = 10;

    public float itemXSpread = 10;
    public float itemYSpread = 0;
    public float itemZSpread = 10;

    public Vector3 randomRotationConstraints;
    public GameObject[] meshColorObjects;

    private List<GameObject> gameObjectsToCombine = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        string pathToDataset = "/Users/haakongunnarsli/masterprosjekt/datasett/";
        string loadFrom = "GoogleDatasett";
        string saveTo = "";
        string fileNameOfNewObj = "NewObj";

        //Creates a list of all .obj files in the dataset folder
        List<string> objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));

        objFiles.ForEach(objFile => Debug.Log(objFile));

        //Load mesh from objhandlers
        //Mesh mesh = Objhandler.LoadMesh(objFiles[4]);

        // Create a new game object to hold the first mesh.
        GameObject meshObject1 = new GameObject("Mesh 1");
        // Set the mesh object as a child of this game object.
        meshObject1.transform.parent = this.transform;
        // Add a mesh filter and mesh renderer to the mesh object.
        MeshFilter meshFilter1 = meshObject1.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer1 = meshObject1.AddComponent<MeshRenderer>();
        // Set the mesh and material for the mesh renderer.
        meshFilter1.mesh = Objhandler.LoadMesh(objFiles[4]);
        gameObjectsToCombine.Add(meshObject1);

        // Repeat the process for the other three meshes.
        GameObject meshObject2 = new GameObject("Mesh 2");
        meshObject2.transform.parent = this.transform;
        MeshFilter meshFilter2 = meshObject2.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer2 = meshObject2.AddComponent<MeshRenderer>();
        meshFilter2.mesh = Objhandler.LoadMesh(objFiles[5]);
        gameObjectsToCombine.Add(meshObject2);


        GameObject meshObject3 = new GameObject("Mesh 3");
        meshObject3.transform.parent = this.transform;
        MeshFilter meshFilter3 = meshObject3.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer3 = meshObject3.AddComponent<MeshRenderer>();
        meshFilter3.mesh = Objhandler.LoadMesh(objFiles[6]);
        gameObjectsToCombine.Add(meshObject3);



        //Temporarily set position to zero to make matrix math easier
        Vector3 position = this.transform.position;
        this.transform.position = Vector3.zero;

        //Get all mesh filters and combine
        MeshFilter[] meshFilters = this.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 1;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            i++;
        }

        this.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        this.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);
        this.transform.gameObject.SetActive(true);

        //Return to original position
        this.transform.position = position;

        //Add collider to mesh (if needed)
        //obj.AddComponent<MeshCollider>();


        // Set the mesh and material for the mesh renderer.
        //meshFilter.mesh = Objhandler.LoadMesh(objFiles[4]);




        //loads the first 3 objfiles into a list
        /*
        CombineInstance[] combine = new CombineInstance[3];

        combine[0].mesh = Objhandler.LoadMesh(objFiles[4]);
        combine[1].mesh = Objhandler.LoadMesh(objFiles[5]);
        combine[2].mesh = Objhandler.LoadMesh(objFiles[6]);


        Mesh mesh = new();
        mesh.CombineMeshes(combine, true);
        */
        //Writes mesh to Obj file
        Objhandler.WriteToObj(this.transform.GetComponent<MeshFilter>().mesh, pathToDataset + saveTo + fileNameOfNewObj + ".obj");

        //Open preview of the new .obj file
        string pathToPreview = "/System/Applications/Preview.app/Contents/MacOS/Preview";
        System.Diagnostics.Process.Start("open", "-a " + pathToPreview + " " + pathToDataset + saveTo + fileNameOfNewObj + ".obj");

        //_______________________________________________________________

    }


}


/*
 * 
        // Create a new game object to hold the first mesh.
        GameObject meshObject1 = new GameObject("Mesh 1");
        // Set the mesh object as a child of this game object.
        meshObject1.transform.parent = this.transform;
        // Add a mesh filter and mesh renderer to the mesh object.
        MeshFilter meshFilter1 = meshObject1.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer1 = meshObject1.AddComponent<MeshRenderer>();
        // Set the mesh and material for the mesh renderer.
        meshFilter1.mesh = Objhandler.LoadMesh(objFiles[4]);

        // Repeat the process for the other three meshes.
        GameObject meshObject2 = new GameObject("Mesh 2");
        meshObject2.transform.parent = this.transform;
        MeshFilter meshFilter2 = meshObject2.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer2 = meshObject2.AddComponent<MeshRenderer>();
        meshFilter2.mesh = Objhandler.LoadMesh(objFiles[5]);

        GameObject meshObject3 = new GameObject("Mesh 3");
        meshObject3.transform.parent = this.transform;
        MeshFilter meshFilter3 = meshObject3.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer3 = meshObject3.AddComponent<MeshRenderer>();
        meshFilter3.mesh = Objhandler.LoadMesh(objFiles[6]);




        CombineInstance[] combine = new CombineInstance[3];

        // Loop through all of the children of the game object that this script is attached to
        for (int i = 0; i < transform.childCount; i++)
        {
            // Get the mesh for the current child
            MeshFilter meshFilter = transform.GetChild(i).GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Debug.Log(meshFilter.sharedMesh.vertices);
                combine[i].mesh = meshFilter.sharedMesh;
            }
        }

        // Combine all of the meshes
        Mesh combinedMesh = new();
        combinedMesh.CombineMeshes(combine, true, true);

        // Add a MeshFilter component to the game object and set its mesh to the combined mesh
        MeshFilter combinedMeshFilter = gameObject.AddComponent<MeshFilter>();

        //try catch to set combinedMeshfilter.mesh to combinedMesh
        try
        {
            combinedMeshFilter.mesh = combinedMesh;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }


        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        combinedMesh.Optimize();
*/