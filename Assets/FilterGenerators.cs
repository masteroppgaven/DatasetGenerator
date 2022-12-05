using System;
using System.Collections.Generic;
using UnityEngine;
//usin obj exporter


public static class FilterGenerators
{
	public static Mesh combineObjects(List<Mesh> meshes, Controller controller)
	{
		//Creates a game object for each mesh
		List<GameObject> gameObjectsToCombine = new List<GameObject>();
		for (int i = 0; i < meshes.Count; i++)
		{
			GameObject meshObject = new GameObject("Mesh " + i);
			meshObject.transform.parent = controller.transform;
			MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter.mesh = meshes[i];
			Debug.Log("Mesh " + i + " added to game object");
		}
		Vector3 position = controller.transform.position;
        controller.transform.position = Vector3.zero;

		// Combine the meshes into a single mesh.
		MeshFilter[] meshFilters = controller.GetComponentsInChildren<MeshFilter>();
		Debug.Log(meshFilters.Length);
		CombineInstance[] combine = new CombineInstance[meshFilters.Length-1];
		for (int i = 0; i < combine.Length; i++)
		{
			combine[i].mesh = meshFilters[i+1].sharedMesh;
			combine[i].transform = meshFilters[i+1].transform.localToWorldMatrix;
			meshFilters[i].gameObject.SetActive(false);
		}
		controller.transform.GetComponent<MeshFilter>().mesh = new Mesh();
		controller.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);
		controller.transform.gameObject.SetActive(true);

		// Return to original position.
		controller.transform.position = position;

		// Return the combined mesh.
		return controller.transform.GetComponent<MeshFilter>().mesh;
		
	}
}

