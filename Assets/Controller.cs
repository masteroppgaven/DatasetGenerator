using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter
using System.IO;
using System.Text;
using Random = UnityEngine.Random;

public class Controller : MonoBehaviour
{

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

        //Apply something here
        List<Mesh> meshes = new List<Mesh>();
        for (int i = 8; i < 11; i++) 
        {
            meshes.Add(Objhandler.LoadMesh(objFiles[i]));
            Debug.Log("Mesh " + i + " loaded");
        }
        
        Mesh combinedMesh = FilterGenerators.combineObjects(meshes, this);

        //Writes mesh to Obj file
        Objhandler.WriteToObj(combinedMesh, pathToDataset + saveTo + fileNameOfNewObj + ".obj");

        //Open preview of the new .obj file
        string pathToPreview = "/System/Applications/Preview.app/Contents/MacOS/Preview";
        System.Diagnostics.Process.Start("open", "-a " + pathToPreview + " " + pathToDataset + saveTo + fileNameOfNewObj + ".obj");

        //________________________________________________________________

    }




}