using UnityEngine;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;

public class FezUnityTrileSet : IFillable<TrileSet> {
	
	public string Name {
		get {
			return TrileSet.Name;
		}
		set {
			TrileSet.Name = value;
		}
	}
	
	public Dictionary<int, Mesh> Triles = new Dictionary<int, Mesh>();
	
	public Texture2D TextureAtlas {
		get {
			return TrileSet.TextureAtlas;
		}
		set {
			TrileSet.TextureAtlas = value;
		}
	}
	
	public Material Material;
	
	public TrileSet TrileSet;
	
	public FezUnityTrileSet() {
	}
	
	public FezUnityTrileSet(TrileSet trileSet)
		: this() {
		Fill(trileSet);
	}
	
	public void Fill(TrileSet trileSet) {
        TrileSet = trileSet;
        Triles.Clear();
		foreach (KeyValuePair<int, Trile> pair in trileSet.Triles) {
			Triles[pair.Key] = FezManager.Instance.GenMesh(pair.Value);
		}
		Material = Object.Instantiate(TextureAtlas.GenMaterial());
        Material.mainTexture.wrapMode = TextureWrapMode.Clamp;
    }

}
