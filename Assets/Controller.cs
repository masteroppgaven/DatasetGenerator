using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter
using System.IO;

public class Controller : MonoBehaviour
{

    private static string pathToDataset = "/Users/haakongunnarsli/masterprosjekt/dataset/";
    private static string fileNameOfNewObj = "NewObj";

    //

    // Start is called before the first frame update
    void Start()
    {
        //objFiles.ForEach(objFile => Debug.Log(objFile));
        //CreateUnfilteredObjectsWithRecalulatedNormals("RecalculatedNormals", "GoogleDataset");
        CreateCombinedMeshesDataset("ClusteredObjects", "RecalculatedNormals");
        //CreateResizedMeshDatset("ResizedObjects", "RecalculatedNormals");
    
        //Open preview of the new .obj file
        //string pathToPreview = "/System/Applications/Preview.app/Contents/MacOS/Preview";
        //System.Diagnostics.Process.Start("open", "-a " + pathToPreview + " " + pathToDataset + saveTo + fileNameOfNewObj + ".obj");
        //________________________________________________________________
        
        //Exit application
        Application.Quit();
    }
    
    public static void CreateResizedMeshDatset(String saveTo, String loadFrom)
    {
        List<string> objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        
        int counter = 0;
        objFiles.ForEach(objFile => {
            Mesh mesh = Objhandler.LoadMesh(objFile);
            Mesh resizedMesh = FilterGenerators.randomResizeMesh(mesh);
       
            List<int> mapping = new List<int>();
            for(int i = 0; i < mesh.vertices.Length; i++) mapping.Add(i);
            
            string counterStringWithFourDigits = counter.ToString().PadLeft(4, '0');
            Directory.CreateDirectory(pathToDataset + saveTo + "/" + counterStringWithFourDigits);

            Objhandler.WriteMeshToObj(resizedMesh, pathToDataset + saveTo +"/"+ counterStringWithFourDigits +"/"+ counterStringWithFourDigits + ".obj");
            Objhandler.WriteVerticesMappingToFile(mapping, pathToDataset + saveTo +"/"+ counterStringWithFourDigits +"/"+ counterStringWithFourDigits + ".txt");
            counter++;
        });
    }


    //Helper method for printing out a list
    public static void CreateCombinedMeshesDataset(String saveTo, String loadFrom)
    {
        List<string> objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        int counter = 0;
        objFiles.ForEach(objFile => {
            if(counter > 1) return;
            //adds this mesh to a list and four random other meshes that is not the same a the first one
            List<Mesh> meshes = new List<Mesh>();
            meshes.Add(Objhandler.LoadMesh(objFile));
            for (int i = 0; i < 3; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, objFiles.Count);
                while (randomIndex == counter) randomIndex = UnityEngine.Random.Range(0, objFiles.Count-1);
                meshes.Add(Objhandler.LoadMesh(objFiles[randomIndex]));
            }
            Mesh combinedMesh = FilterGenerators.combineObjects(meshes);
            
            //Creates a new folder for the new .obj file
            string counterStringWithFourDigits = counter.ToString().PadLeft(4, '0');
            Directory.CreateDirectory(pathToDataset + saveTo + "/" + counterStringWithFourDigits);

            //Writes mesh to Obj file and creates a new .txt file with the vertices mapping
            Objhandler.WriteMeshToObj(combinedMesh, pathToDataset + saveTo + "/" +counterStringWithFourDigits + "/" + counterStringWithFourDigits + ".obj");

            List<int> mapping = new List<int>();
            for(int i = 0; i < meshes[0].vertices.Length; i++) mapping.Add(i);
            Objhandler.WriteVerticesMappingToFile(mapping, pathToDataset + saveTo +"/"+ counterStringWithFourDigits +"/"+ counterStringWithFourDigits + ".txt");
            counter++;
        });
    }


//Takes in the google datset and recalulates the normals and saves it.
//We need to do this because the normals will get recalulated when applying the filters 
    public static void CreateUnfilteredObjectsWithRecalulatedNormals(String saveTo, String loadFrom)
    {

        //Creates a list of all .obj files in the dataset folder
        List<string> objFiles = new List<string>(Directory.GetFiles(pathToDataset + loadFrom, "*.obj", SearchOption.AllDirectories));
        //For each objfile in objFiles does Loadmesh get called and then WriteMeshToObj
        //Creates a counter that is used to name the new .obj files that always consist of four digits
        int counter = 0;
        objFiles.ForEach(objFile => {
            Mesh mesh = Objhandler.LoadMesh(objFile);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            string counterStringWithFourDigits = counter.ToString().PadLeft(4, '0');
            Objhandler.WriteMeshToObj(mesh, pathToDataset + saveTo + "/"+counterStringWithFourDigits+".obj");
            counter++;
        });
    }






}