using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public class FezUnityTrileInstance : MonoBehaviour, IFillable<TrileInstance>, IFillable<TrileInstance, FezUnityTrileSet> {
	
	[HideInInspector]
	public TrileInstance Trile;
	
	[HideInInspector]
	public FezUnityTrileSet TrileSet;
	
	public void Fill(TrileInstance trile) {
		Fill(trile, FezManager.Instance.GetUnityTrileSet(trile.Trile.TrileSet.Name));
	}
	
	public void Fill(TrileInstance trile, FezUnityTrileSet trileSet) {
		Trile = trile;
		TrileSet = trileSet;
		Fill(gameObject, trile, trileSet);
	}
	
	public static void Fill(GameObject obj, TrileInstance trile, FezUnityTrileSet trileSet = null) {
		if (trileSet == null) {
			trileSet = FezManager.Instance.GetUnityTrileSet(trile.Trile.TrileSet.Name);
		}
		
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = trile.GetUnityMesh();
		
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = trileSet.Material; // TrileSet.Material already instantiated
    }
	
	public void Update() {
		
	}
	
}
