using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;
using FezEngine.Structure.Geometry;
using UnityEngine.SceneManagement;
using System.Threading;

/*
 * FezUnity management - everything that isn't static and / or needs setup.
 */
public class FezManager : MonoBehaviour {
	
	public static FezManager Instance;
    public static Thread MainThread;
	
	public FezManager() {
		Instance = this;
	}
	
	public static string FezPath;
	
	public Shader DefaultShader;

	public Material BackgroundPlaneMaterial;
	public Material BackgroundPlaneFullbrightMaterial;
	public Material BackgroundPlaneFullbrightOnesidedMaterial;
	public Material BackgroundPlaneOnesidedMaterial;

    public Material SkyLayerMaterial;
    public Material SkyLayerFullbrightMaterial;

    public Material WaterMaterial;
	
	public Mesh QuadMesh;
    public Mesh WaterMesh;
	public Mesh BackgroundPlaneMesh;
    public Mesh SkyLayerMesh;
	
	public Transform Player;

    public Light Sun;
    public float Time = 12f * 3600f;
    public const float SecondsPerDay = 24f * 3600f;
    public float TimeFactor = 260f;
    public float TimeRelative { get { return (Time / SecondsPerDay) % 1f; } }
    [HideInInspector] public float NightContribution;
    [HideInInspector] public float DawnContribution;
    [HideInInspector] public float DuskContribution;

    public float SkySpace = 48f;

    [HideInInspector] public Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

    [HideInInspector] public Dictionary<string, Sky> FezSkies = new Dictionary<string, Sky>();
    [HideInInspector] public Dictionary<string, FezUnitySky> UnitySkies = new Dictionary<string, FezUnitySky>();

    [HideInInspector] public Dictionary<string, TrileSet> FezTrileSets = new Dictionary<string, TrileSet>();
	[HideInInspector] public Dictionary<string, FezUnityTrileSet> UnityTrileSets = new Dictionary<string, FezUnityTrileSet>();
	
	[HideInInspector] public Dictionary<string, ArtObject> ArtObjects = new Dictionary<string, ArtObject>();
	[HideInInspector] public Dictionary<string, Mesh> ArtObjectMeshes = new Dictionary<string, Mesh>();

    List<QueuedAction> MainQueue = new List<QueuedAction>();

    string ChosenJoystickTriggerAxis;

    void Start() {
        MainThread = Thread.CurrentThread;
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        if (string.IsNullOrEmpty(FezPath)) {
			FezPath = PlayerPrefs.GetString("FezContentPath");
		}
		
		if (DefaultShader == null) {
			DefaultShader = Shader.Find("Diffuse");
		}
		
		if (BackgroundPlaneMesh == null) {
			BackgroundPlaneMesh = new Mesh() {
				name = "Background Plane",
				vertices = QuadMesh.vertices,
				// Completely flip UV
				uv = new Vector2[] {
					QuadMesh.uv[3],
					QuadMesh.uv[2],
					QuadMesh.uv[1],
					QuadMesh.uv[0]
				},
				triangles = QuadMesh.triangles,
				normals = QuadMesh.normals
			};
		}
        if (SkyLayerMesh == null) {
            // TODO fix redundant vertices
            SkyLayerMesh = new Mesh() {
                name = "Sky Layer",
                vertices = new Vector3[] {
                    // A
                    new Vector3(-1f, -1f, -1f),
                    new Vector3(-1f,  1f, -1f),
                    new Vector3( 1f, -1f, -1f),
                    new Vector3( 1f,  1f, -1f),
                    // B
                    new Vector3( 1f, -1f, -1f),
                    new Vector3( 1f,  1f, -1f),
                    new Vector3( 1f, -1f,  1f),
                    new Vector3( 1f,  1f,  1f),
                    // C
                    new Vector3(-1f, -1f,  1f),
                    new Vector3(-1f,  1f,  1f),
                    new Vector3( 1f, -1f,  1f),
                    new Vector3( 1f,  1f,  1f),
                    // D
                    new Vector3(-1f, -1f, -1f),
                    new Vector3(-1f,  1f, -1f),
                    new Vector3(-1f, -1f,  1f),
                    new Vector3(-1f,  1f,  1f)
                },
                uv = new Vector2[] {
                    // A
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    // B
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    // C
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    // D
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f)
                },
                triangles = new int[] {
                    // A
                    2, 1, 0,
                    1, 2, 3,
                    // B
                    6, 5, 4,
                    5, 6, 7,
                    // C
                    8, 9, 10,
                    11, 10, 9,
                    // D
                    12, 13, 14,
                    15, 14, 13
                }
            };
            SkyLayerMesh.Optimize();
            SkyLayerMesh.RecalculateNormals();
        }

        if (Sun == null) {
            Sun = GameObject.Find("Sun").GetComponent<Light>();
        }
		
		FmbUtil.Setup.Log = null;
		
		// FmbLib LzxDecoder extension to read directly from .pak
		LzxDecompressor.Init();
		
		ScanPackMetadata("Essentials.pak");
		ScanPackMetadata("Updates.pak");
		ScanPackMetadata("Other.pak");
		
		if (AssetMetadata.Map.Count == 0) {
			// Something went horribly wrong - ABORT!
			PlayerPrefs.SetString("FezContentPath", null);
			SceneManager.LoadScene("SetupScene");
			return;
		}

        var joystickNames = Input.GetJoystickNames();
        for (int i = 0; i < Mathf.Min(8, joystickNames.Length); i++)
            if (joystickNames[i].IndexOf("xbox", System.StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                ChosenJoystickTriggerAxis = string.Format("Time_J{0}", i + 1);
                Debug.Log(string.Format("Chose joystick #{0} for the time axis", i + 1));
                break;
            }
    }

    void Awake() {
		
	}
	
	void Update() {
        Time += UnityEngine.Time.deltaTime * TimeFactor + UnityEngine.Time.deltaTime * TimeFactor * Input.GetAxis("Time") * 100.0f;

        if (ChosenJoystickTriggerAxis != null)
            Time += UnityEngine.Time.deltaTime * TimeFactor * Input.GetAxis(ChosenJoystickTriggerAxis) * 100.0f;

        if (Time < 0f) {
            Time += SecondsPerDay;
        }

        // Time contribution values blatantly stolen from FEZ
        const float onesixth = 1f / 6f;
        const float onethird = 1f / 3f;
        DawnContribution = EaseTime(TimeRelative, 0.08333334f, onesixth);
        DuskContribution = EaseTime(TimeRelative, 0.75f, onesixth);
        NightContribution = EaseTime(TimeRelative, 0.8333333f, onethird);
        NightContribution = Mathf.Max(NightContribution, EaseTime(TimeRelative, 0.8333333f - 1f, onethird));

        RenderSettings.skybox.SetFloat("_SkyGradientX", TimeRelative);

        while (0 < MainQueue.Count) {
            QueuedAction qa = MainQueue[0];
            MainQueue.RemoveAt(0);
            qa.Invoke();
        }
    }

    private float EaseTime(float value, float start, float duration) {
        float time = value - start;
        float durationThird = duration / 3f;
        if (time < durationThird) {
            return Mathf.Clamp01(time / durationThird);
        }
        if (time > 2f * durationThird) {
            return 1f - Mathf.Clamp01((time - 2f * durationThird) / durationThird);
        }
        return time < 0f || time > duration ? 0f : 1f;
    }

    public bool AssetExists(string assetName) {
        return AssetMetadata.Map.ContainsKey(assetName.ToLowerInvariant().Replace('/', '\\'));
    }
    public Stream StreamFromPack(string assetName) {
		assetName = assetName.ToLowerInvariant().Replace('/', '\\');
		AssetMetadata metadata;
		if (AssetMetadata.Map.TryGetValue(assetName, out metadata)) {
			Stream assetStream = File.OpenRead(metadata.File);
			if (metadata.Length == 0) {
				return assetStream;
			}
			return new LimitedStream(assetStream, metadata.Offset, metadata.Length);
		}
		return null;
	}
	public BinaryReader ReadFromPack(string assetName) {
		Stream stream = StreamFromPack(assetName);
		if (stream == null) {
			return null;
		}
		return new BinaryReader(stream);
	}
	
	public void ScanPackMetadata(string name) {
		string filePath = Path.Combine(FezPath, name);
		if (!File.Exists(filePath)) {
			Debug.Log("Pack " + filePath + " not found.");
			return;
		}
		Debug.Log("Loading file map data from pack file " + filePath);
		using (FileStream packStream = File.OpenRead(filePath)) {
			using (BinaryReader packReader = new BinaryReader(packStream)) {
				int count = packReader.ReadInt32();
				for (int i = 0; i < count; i++) {
					string file = packReader.ReadString();
					int length = packReader.ReadInt32();
					if (!AssetMetadata.Map.ContainsKey(file)) {
						AssetMetadata.Map[file] = new AssetMetadata(filePath, packStream.Position, length);
					}
					packStream.Seek(length, SeekOrigin.Current);
				}
			}
		}
	}
	
	public FezUnityLevel LoadLevel(string name, bool unload = true) {
        if (unload) {
            UnloadLevel(FezUnityLevel.Current);
        }

        FezUnityLevel level = null;
        IEnumerator loadc = LoadLevelCoroutine(name, false, LoadLevelCoroutinePost);
        while (loadc.MoveNext()) {
            object item = loadc.Current;
            if (item is FezUnityLevel) {
                level = (FezUnityLevel) item;
            }
        }

        return level;
	}

    public Coroutine LoadLevelAsync(string name, System.Action<FezUnityLevel> cb, bool unload = true) {
        FezUnityLevel previous = FezUnityLevel.Current;
        return StartCoroutine(LoadLevelCoroutine(name, true, delegate(FezUnityLevel level_) {
            if (unload) {
                UnloadLevel(previous);
            }
            LoadLevelCoroutinePost(level_);
            cb(level_);
        }));
    }

    public IEnumerator LoadLevelCoroutine(string name, bool multithreaded, System.Action<FezUnityLevel> cb) {
        GameObject levelObj = new GameObject("");
        levelObj.SetActive(false);
        FezUnityLevel level = levelObj.AddComponent<FezUnityLevel>();
        using (BinaryReader reader = ReadFromPack("levels/" + name)) {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            Level levelRaw = null;
            if (!multithreaded) {
                sw.Start();
                levelRaw = FmbUtil.ReadObject(reader) as Level;
                sw.Stop();
                yield return null;
            } else {
                Thread thread = new Thread(delegate() {
                    sw.Start();
                    levelRaw = FmbUtil.ReadObject(reader) as Level;
                    sw.Stop();
                });
                thread.Name = "FezUnity load raw level " + name;
                thread.IsBackground = true;
                thread.Start();
                while (thread.IsAlive) {
                    yield return null;
                }
            }
            Debug.Log("Level " + levelRaw.Name + " loaded in " + sw.ElapsedMilliseconds + " ms");

            long timeLoad = sw.ElapsedMilliseconds;
            sw.Start();
            IEnumerator fillc = level.FillCoroutine(levelRaw);
            while (fillc.MoveNext()) {
                yield return fillc.Current;
            }
            sw.Stop();
            long timeFill = sw.ElapsedMilliseconds;
            Debug.Log("Level " + levelRaw.Name + " filled in " + sw.ElapsedMilliseconds + " ms");

            Debug.Log("Level " + levelRaw.Name + " loaded and filled in " + (timeLoad + timeFill) + " ms");
        }

        levelObj.SetActive(true);
        yield return level;
        cb(level);
    }

    void LoadLevelCoroutinePost(FezUnityLevel level) {
        if (Player == null) {
            Player = transform;
        }
        if (level.Level.StartingPosition != null) {
            Player.position = level.Level.StartingPosition.Id.ToVector() + Vector3.up * 2f + FezHelper.TrileOffset;
            Player.position = new Vector3(
                Player.position.x,
                Player.position.y,
                -Player.position.z
            );
        }
    }


    public void UnloadLevel(FezUnityLevel level = null) {
        bool nullifyCurrent = false;
        if (level == null) {
            level = FezUnityLevel.Current;
            nullifyCurrent = true;
        }
		if (level == null) {
			return;
		}
		
		// TODO
		
		Destroy(level.gameObject);
        if (nullifyCurrent) {
            FezUnityLevel.Current = null;
        }
	}
	
	public Mesh GenMesh(Trile trile) {
		Mesh mesh = GenMesh(trile.Geometry);
		mesh.name = trile.Name;
		return mesh;
	}
	
	public Mesh GenMesh(ArtObject ao) {
		Mesh mesh = GenMesh(ao.Geometry);
		mesh.name = ao.Name;
		return mesh;
	}
	
	public Mesh GenMesh<T>(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, T> siip, float verticesScale = 1f) {
		Mesh mesh = new Mesh();
		
		Vector3[] vertices = new Vector3[siip.Vertices.Length];
		Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++) {
            //vertices[i] = siip.Vertices[i].Position;
			// Trixel Engine Z != Unity Z
			vertices[i] = new Vector3(
				siip.Vertices[i].Position.x,
				siip.Vertices[i].Position.y,
				-siip.Vertices[i].Position.z
			);
            uv[i] = siip.Vertices[i].TextureCoordinate;
        }

		mesh.vertices = vertices;
		mesh.triangles = siip.Indices;
		mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.Optimize();
		
		return mesh;
	}
	
	Dictionary<CacheKey_Texture2D_Name, Material> cachedMaterialsPerShader = new Dictionary<CacheKey_Texture2D_Name, Material>();
	CacheKey_Texture2D_Name cachedMaterialsPerShaderKey = new CacheKey_Texture2D_Name();
	public Material GenMaterial(Texture2D texture, Shader shader = null) {
		if (texture == null) {
			return null;
		}

        cachedMaterialsPerShaderKey.A = texture;
        cachedMaterialsPerShaderKey.B = shader == null ? null : shader.name;
		Material material;
		if (cachedMaterialsPerShader.TryGetValue(cachedMaterialsPerShaderKey, out material)) {
			return material;
		}
		
		if (shader == null) {
			shader = DefaultShader;
		}
		
        material = new Material(shader);
        material.mainTexture = texture;
        material.mainTexture.filterMode = FilterMode.Point;

        cachedMaterialsPerShader[new CacheKey_Texture2D_Name() {
			A = texture,
			B = cachedMaterialsPerShaderKey.B
		}] = material;
        return material;
    }

    Dictionary<CacheKey_Texture2D_Name, Material> cachedMaterialsPerMaterial = new Dictionary<CacheKey_Texture2D_Name, Material>();
    CacheKey_Texture2D_Name cachedMaterialsPerMaterialKey = new CacheKey_Texture2D_Name();
    public Material GenMaterial(Texture2D texture, Material materialBase) {
        if (texture == null || materialBase == null) {
            return null;
        }

        cachedMaterialsPerMaterialKey.A = texture;
        cachedMaterialsPerMaterialKey.B = materialBase.name;
        Material material;
        if (cachedMaterialsPerMaterial.TryGetValue(cachedMaterialsPerMaterialKey, out material)) {
            return material;
        }

        material = new Material(materialBase);
        material.mainTexture = texture;
        material.mainTexture.filterMode = FilterMode.Point;

        cachedMaterialsPerMaterial[new CacheKey_Texture2D_Name() {
            A = texture,
            B = cachedMaterialsPerMaterialKey.B
        }] = material;
        return material;
    }



    public Texture GetTexture(string name) {
		Texture t;
		if (Textures.TryGetValue(name, out t)) {
			return t;
		}
		
		using (BinaryReader reader = ReadFromPack(name)) {
			return Textures[name] = FmbUtil.ReadObject(reader) as Texture;
		}
	}
    public Texture2D GetTexture2D(string name) {
        return GetTexture(name) as Texture2D;
    }

    public object GetTextureOrOther(string name) {
		Texture t;
		if (Textures.TryGetValue(name, out t)) {
			return t;
		}
		
		using (BinaryReader reader = ReadFromPack(name)) {
			object obj = FmbUtil.ReadObject(reader);
			if (obj is Texture) {
				Textures[name] = (Texture) obj;
			}
			return obj;
		}
	}

    public FezUnitySky GetUnitySky(string name) {
        FezUnityLevel level = FezUnityLevel.Current;
        // Microoptimization: Sky.Sky - direct access
        if (level != null && level.Sky != null && level.Sky.Sky != null && level.Sky.Sky.Name == name) {
            return level.Sky;
        }

        FezUnitySky uSky;
        if (UnitySkies.TryGetValue(name, out uSky)) {
            return uSky;
        }

        return UnitySkies[name] = new FezUnitySky(GetFezSky(name));
    }

    public Sky GetFezSky(string name) {
        Sky sky;
        if (FezSkies.TryGetValue(name, out sky)) {
            return sky;
        }

        using (BinaryReader reader = ReadFromPack("skies/" + name)) {
            return FezSkies[name] = FmbUtil.ReadObject(reader) as Sky;
        }
    }

    public FezUnityTrileSet GetUnityTrileSet(string name) {
		FezUnityLevel level = FezUnityLevel.Current;
		// Microoptimization: TrileSet.TrileSet - direct access
		if (level != null && level.TrileSet != null && level.TrileSet.TrileSet.Name == name) {
			return level.TrileSet;
		}
		
		FezUnityTrileSet uts;
        if (UnityTrileSets.TryGetValue(name, out uts)) {
			return uts;
		}
		
		return UnityTrileSets[name] = new FezUnityTrileSet(GetFezTrileSet(name));
    }
	
	public TrileSet GetFezTrileSet(string name) {
		TrileSet ts;
		if (FezTrileSets.TryGetValue(name, out ts)) {
			return ts;
		}
		
		using (BinaryReader reader = ReadFromPack("trile sets/" + name)) {
			return FezTrileSets[name] = FmbUtil.ReadObject(reader) as TrileSet;
		}
	}
	
	public ArtObject GetArtObject(string name) {
		ArtObject ao;
		if (ArtObjects.TryGetValue(name, out ao)) {
			return ao;
		}
		
		using (BinaryReader reader = ReadFromPack("art objects/" + name)) {
			return ArtObjects[name] = FmbUtil.ReadObject(reader) as ArtObject;
		}
	}


    public QueuedAction Queue(System.Action action, List<QueuedAction> queue = null, bool skipQueue = false) {
        Debug.Log("QUEUE: " + action.Method.Name + ", " + (queue == null ? "null" : queue.Count.ToString()) + ", " + skipQueue);
        if (skipQueue) {
            action();
            return null;
        }

        QueuedAction qa = new QueuedAction(action, queue);
        MainQueue.Add(qa);
        return qa;
    }

}

class CacheKey_Texture2D_Name {
	public Texture2D A;
	public string B;
	public override int GetHashCode() {
		int a = A == null ? 0 : A.GetHashCode();
		int b = B == null ? 0 : B.GetHashCode();
		return a + a | b;
	}
    public override bool Equals(object o_) {
        CacheKey_Texture2D_Name o = o_ as CacheKey_Texture2D_Name;
        if (o == null) {
            return false;
        }
        if (
            (o.A == null && o.A != null) ||
            (o.A != null && o.A == null) ||
            (o.B == null && o.B != null) ||
            (o.B != null && o.B == null)
        ) {
            return false;
        }
        if (o.A == null && o.B == null) {
            return true;
        }
        if (o.A != null) {
            return o.A.Equals(A);
        }
        if (o.B != null) {
            return o.B.Equals(B);
        }
        return o.A.Equals(A) && o.B.Equals(B);
    }
}
