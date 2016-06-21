using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;
using System.Collections;

public class FezUnityLevel : MonoBehaviour, IFillable<Level> {
	
	public static FezUnityLevel Current;

    public static int AssetsPerFrame = 32;
    public static int ChunksPerFrame = 8;
    public static bool SetupLightingOnFill = true;
    public static int DefaultTrileVisibilityDepth = 1;
    public static int ChunkSize = 16;

    public string Name {
		get {
			return Level.Name;
		}
		set {
			Level.Name = value;
		}
	}

	[HideInInspector]
	public Dictionary<TrileEmplacement, GameObject> Triles = new Dictionary<TrileEmplacement, GameObject>();
	[HideInInspector]
	public Dictionary<int, GameObject> ArtObjects = new Dictionary<int, GameObject>();
	[HideInInspector]
	public Dictionary<int, GameObject> BackgroundPlanes = new Dictionary<int, GameObject>();
    [HideInInspector]
    public Dictionary<int, GameObject> NPCs = new Dictionary<int, GameObject>();

    [HideInInspector]
	public FezUnityTrileSet TrileSet;
	
	[HideInInspector]
	public FezUnitySky Sky;
    [HideInInspector]
    public FezUnityWater Water;

    [HideInInspector]
    public Level Level;

    GameObject chunksObj;
    Dictionary<ChunkKey, FezUnityLevelChunk> chunks = new Dictionary<ChunkKey, FezUnityLevelChunk>();

    public FezUnityLevel() {
		Current = this;
	}

    public void Fill(Level level) {
        IEnumerator fillc = FillCoroutine(level);
        while (fillc.MoveNext()) {
        }
    }


    public IEnumerator FillCoroutine(Level level) {
		Level = level;
		
		if (string.IsNullOrEmpty(name)) {
			name = level.Name;
			
			//Offset the triles and everything back
			transform.position -= FezHelper.TrileOffset;
		}
		
		Triles.Clear();
		ArtObjects.Clear();

        if (chunksObj != null) {
            Destroy(chunksObj);
        }
        chunksObj = new GameObject("Chunks");
        chunksObj.transform.parent = transform;
        chunksObj.isStatic = true;
        chunks.Clear();

        TrileSet = FezManager.Instance.GetUnityTrileSet(level.TrileSetName);
		level.TrileSet = TrileSet.TrileSet;
        yield return null;

        if (level.SkyName != null) {
            GameObject skyObj = new GameObject("Sky (" + level.SkyName + ")");
            skyObj.transform.parent = transform;
            Sky = skyObj.AddComponent<FezUnitySky>();
            Sky.Fill(FezManager.Instance.GetUnitySky(level.SkyName));
            yield return null;
        }

        if (SetupLightingOnFill) {
            SetupLighting();
        }

        if (level.WaterType != LiquidType.None) {
            GameObject waterObj = new GameObject("Water");
            waterObj.transform.parent = transform;
            waterObj.transform.localPosition = new Vector3(level.Size.x / 2f, level.WaterHeight, level.Size.z / -2f);
            Water = waterObj.AddComponent<FezUnityWater>();
            Water.Fill(level.WaterType);
        }

        int asset = 0;

        GameObject trilesObj = new GameObject("Triles");
		trilesObj.transform.parent = transform;
		trilesObj.transform.position += FezHelper.TrileOffset;
		foreach (KeyValuePair<TrileEmplacement, TrileInstance> pair in level.Triles) {
			TrileInstance trile = pair.Value;
			if (trile.TrileId == -1) {
				continue;
			}
            SetTrileInInstance(trile);
			GameObject trileObj = trile.GenObject(trilesObj);

            Triles[pair.Key] = trileObj;

            if (IsHidden(trile.Emplacement)) {
                trileObj.SetActive(false);
            } else {
                AddToChunk(trileObj);
                asset++;
            }

            if (asset >= AssetsPerFrame) {
                asset = 0;
                yield return pair;
            } else {
                asset++;
            }
        }

        GameObject aosObj = new GameObject("Art Objects");
		aosObj.transform.parent = transform;
		foreach (KeyValuePair<int, ArtObjectInstance> pair in level.ArtObjects) {
			ArtObjectInstance ao = pair.Value;
			ao.ArtObject = FezManager.Instance.GetArtObject(ao.ArtObjectName);
			ArtObjects[pair.Key] = ao.GenObject(aosObj);

            if (asset >= AssetsPerFrame) {
                asset = 0;
                yield return pair;
            } else {
                asset++;
            }
        }
		
		GameObject planesObj = new GameObject("Background Planes");
		planesObj.transform.parent = transform;
		foreach (KeyValuePair<int, BackgroundPlane> pair in level.BackgroundPlanes) {
			BackgroundPlane plane = pair.Value;
			BackgroundPlanes[pair.Key] = plane.GenObject(planesObj);

            if (asset >= AssetsPerFrame) {
                asset = 0;
                yield return pair;
            } else {
                asset++;
            }
        }

        GameObject npcsObj = new GameObject("NPCs");
        npcsObj.transform.parent = transform;
        foreach (KeyValuePair<int, NpcInstance> pair in level.NonPlayerCharacters) {
            NpcInstance npc = pair.Value;
            NPCs[pair.Key] = npc.GenObject(npcsObj);

            if (asset >= AssetsPerFrame) {
                asset = 0;
                yield return pair;
            } else {
                asset++;
            }
        }

        asset = 0;
        IEnumerator buildc = BuildChunks();
        while (buildc.MoveNext()) {
            if (asset >= ChunksPerFrame) {
                asset = 0;
                yield return buildc.Current;
            } else {
                asset++;
            }
        }
    }

    public void SetupLighting() {
        RenderSettings.ambientIntensity = Level.BaseAmbient;
        FezManager.Instance.Sun.intensity = 1f - Level.BaseAmbient;
        if (Sky != null) {
            RenderSettings.skybox.SetTexture("_SkyGradientTex", Sky.Texture);
            RenderSettings.skybox.SetFloat("_SkyGradientYOffset", 1f / Sky.Texture.height);
        }
    }

    public bool IsHidden(TrileEmplacement te_, int d = 0) {
        if (d == 0) {
            d = DefaultTrileVisibilityDepth;
        }

        for (int x = -d; x <= d; x++) {
            for (int y = -d; y <= d; y++) {
                for (int z = -d; z <= d; z++) {
                    if (x == 0 && y == 0 && z == 0) continue;

                    TrileEmplacement te = new TrileEmplacement(
                        te_.X + x,
                        te_.Y + y,
                        te_.Z + z
                    );
                    TrileInstance trile;
                    if (!Level.Triles.TryGetValue(te, out trile) || trile.TrileId == -1) {
                        return false;
                    }
                    SetTrileInInstance(trile);
                    if (trile.ForceSeeThrough || trile.Trile.SeeThrough || trile.Trile.Thin) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public void SetTrileInInstance(TrileInstance trile) {
        if (trile.Trile == null) {
            trile.Trile = Level.TrileSet.Triles[trile.TrileId];
        }
    }

    public void AddToChunk(GameObject obj) {
        obj.GetComponent<MeshRenderer>().enabled = false;
        Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        GetOrGenChunk(
            Mathf.FloorToInt(obj.transform.position.x / ChunkSize),
            Mathf.FloorToInt(obj.transform.position.y / ChunkSize),
            Mathf.FloorToInt(obj.transform.position.z / ChunkSize),
            mesh.vertices.Length,
            mesh.triangles.Length).Add(obj);
    }

    public IEnumerator BuildChunks() {
        foreach (FezUnityLevelChunk chunk in chunks.Values) {
            chunk.Build();
            yield return null;
        }
    }

    ChunkKey chunkKey = new ChunkKey();
    FezUnityLevelChunk GetOrGenChunk(int cx, int cy, int cz, int vertices = 0, int indices = 0) {
        FezUnityLevelChunk chunk = null;
        chunkKey.X = cx;
        chunkKey.Y = cy;
        chunkKey.Z = cz;
        chunkKey.Sub = 0;

        while (chunks.TryGetValue(chunkKey, out chunk)) {
            if (chunk.Fits(vertices, indices)) {
                return chunk;
            }
            chunkKey.Sub++;
        }

        GameObject chunkObj = new GameObject("Chunk " + cx + ", " + cy + ", " + cz + " (sub " + chunkKey.Sub + ")");
        chunkObj.transform.parent = chunksObj.transform;
        chunkObj.isStatic = true;

        MeshFilter mf = chunkObj.AddComponent<MeshFilter>();
        mf.sharedMesh = new Mesh();
        mf.sharedMesh.name = "Combined Mesh";
        
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = TrileSet.Material;

        chunk = chunkObj.AddComponent<FezUnityLevelChunk>();

        chunks[new ChunkKey() {
            X = cx,
            Y = cy,
            Z = cz,
            Sub = chunkKey.Sub
        }] = chunk;
        return chunk;
    }

}

class ChunkKey {
    public int X;
    public int Y;
    public int Z;
    public int Sub;
    public override int GetHashCode() {
        return X + ((X | (Y * 8)) & Z) - (Sub | Z) * 4;
    }
    public override bool Equals(object o_) {
        ChunkKey o = o_ as ChunkKey;
        if (o == null) {
            return false;
        }
        return
            X == o.X &&
            Y == o.Y &&
            Z == o.Z &&
            Sub == o.Sub;
    }
}

class FezUnityLevelChunk : MonoBehaviour {
    const int MaxVertices = ushort.MaxValue;
    const int MaxIndices = ushort.MaxValue * 3;

    List<CombineInstance> meshes = new List<CombineInstance>(FezUnityLevel.ChunkSize * FezUnityLevel.ChunkSize * FezUnityLevel.ChunkSize);
    int vertices = 0;
    int indices = 0;

    public void Add(GameObject obj) {
        Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        meshes.Add(new CombineInstance() {
            transform = obj.transform.localToWorldMatrix,
            mesh = mesh
        });
        vertices += mesh.vertices.Length;
        indices += mesh.triangles.Length;
    }

    public void Build() {
        GetComponent<MeshFilter>().sharedMesh.CombineMeshes(meshes.ToArray(), true);
    }

    public bool Fits(int v, int i) {
        return vertices + v <= MaxVertices && indices + i <= MaxIndices;
    }
}
