using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter


public static class FilterGenerators
{
	public static Mesh combineObjects(List<Mesh> meshes)
	{
		//Creates a game object for each mesh
		List<GameObject> gameObjectsToCombine = new List<GameObject>();
		for (int i = 0; i < meshes.Count; i++)
		{
			GameObject meshObject = new GameObject("Mesh " + i);
			//meshObject.transform.parent = controller.transform;
			MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
			meshFilter.mesh = meshes[i];
			gameObjectsToCombine.Add(meshObject);
			Debug.Log("Mesh " + i + " added to game object");
		}
		//Vector3 position = controller.transform.position;
        //controller.transform.position = Vector3.zero;

		// Combine the meshes into a single mesh.
		//MeshFilter[] meshFilters = controller.GetComponentsInChildren<MeshFilter>();
		//Debug.Log(meshFilters.Length);
		CombineInstance[] combine = new CombineInstance[gameObjectsToCombine.Count];
        for (int i = 0; i < combine.Length; i++)
		{
			MeshFilter meshFilter = gameObjectsToCombine[i].GetComponent<MeshFilter>();
            combine[i].mesh = meshFilter.sharedMesh;
			combine[i].transform = meshFilter.transform.localToWorldMatrix;
            meshFilter.gameObject.SetActive(false);
		}
		Mesh newMesh = new();
        newMesh.CombineMeshes(combine, true, true);


		// Return the combined mesh.
		return newMesh;
		
	}

	//takes in a Mesh and resize it to a random size
	public static Mesh randomResizeMesh(Mesh mesh){
		//Randomize the size of the mesh
		float randomSize = UnityEngine.Random.Range(0.1f, 10.0f);
		Vector3[] vertices = mesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = vertices[i] * randomSize;
		}
		mesh.vertices = vertices;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		return mesh;
	}


}

