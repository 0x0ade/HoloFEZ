using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public class FezUnityArtObjectInstance : MonoBehaviour, IFillable<ArtObjectInstance> {
	
	[HideInInspector]
	public ArtObjectInstance ArtObject;
	
	public void Fill(ArtObjectInstance ao) {
		ArtObject = ao;
		Fill(gameObject, ao);
		

	}
	
	public static void Fill(GameObject obj, ArtObjectInstance ao) {
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = ao.GetUnityMesh();
		
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.material = Instantiate(ao.ArtObject.Cubemap.GenMaterial());
        meshRenderer.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
    }
	
	public void Update() {
		
	}
	
}
