using UnityEngine;
using System;
using System.Collections.Generic;
using FmbLib;
using FezEngine;
using FezEngine.Structure;

/*
 * FezUnity helper - static values, helpers and extension methods.
 */
public static class FezHelper {
	
	// Tiles are offset by 0.5f in the Trixel Engine (see FEZMod.Editor custom mesh rendering, i.e. triles)
	// As the triles are the real units, we're moving everything else back by -0.5f
	public readonly static Vector3 TrileOffset = new Vector3(0.5f, 0.5f, 0.5f);

    public static Vector3 ToVector(this TrileEmplacement e) {
		return new Vector3(
			e.X,
			e.Y,
			e.Z
		);
	}
	
	public static Vector3 FezZRotation = new Vector3(
		0f,
		180f,
		0f
	);
	public static void FezZ(this GameObject obj) {
		// Invert Z
		obj.transform.position = new Vector3(
			obj.transform.position.x,
			obj.transform.position.y,
			-obj.transform.position.z
		);
		
		Vector3 euler = obj.transform.rotation.eulerAngles;
		if (euler.y.AlmostEqual(90f) || euler.y.AlmostEqual(270f)) {
			obj.transform.Rotate(FezZRotation);
		}
	}
	
	public static bool AlmostEqual(this float a, float b, float e = 0.01f) {
		return Mathf.Abs(a - b) <= e;
	}

	public static Mesh GetUnityMesh(this TrileInstance trile) {
		return trile.Trile.GetUnityMesh();
	}
	public static Mesh GetUnityMesh(this Trile trile) {
		FezUnityTrileSet trileSet;
		FezUnityLevel level = FezUnityLevel.Current;
		// Microoptimization: TrileSet.TrileSet - direct access
		if (level != null && level.TrileSet != null && level.TrileSet.TrileSet.Name == trile.TrileSet.Name) {
			trileSet = level.TrileSet;
		} else {
			// Automatically generates if required
			trileSet = FezManager.Instance.GetUnityTrileSet(trile.TrileSet.Name);
		}
		return trileSet.Triles[trile.Id];
	}
	
	public static Mesh GetUnityMesh(this ArtObjectInstance ao) {
		return ao.ArtObject.GetUnityMesh();
	}
	public static Mesh GetUnityMesh(this ArtObject ao) {
		Mesh mesh;
		if (!FezManager.Instance.ArtObjectMeshes.TryGetValue(ao.Name, out mesh)) {
			FezManager.Instance.ArtObjectMeshes[ao.Name] = mesh = FezManager.Instance.GenMesh(ao);
		}
		return mesh;
	}
	
	public static Material GenMaterial(this Texture2D texture, Shader shader = null) {
		return FezManager.Instance.GenMaterial(texture, shader);
	}
    public static Material GenMaterial(this Texture2D texture, Material materialBase) {
        return FezManager.Instance.GenMaterial(texture, materialBase);
    }

    public static GameObject GenObject(this TrileInstance trile, GameObject parent = null) {
		GameObject obj = new GameObject(trile.Trile.Name);
		if (parent != null) {
			obj.transform.parent = parent.transform;
		}
		obj.transform.localPosition = trile.Position;
		// Muh precision
        obj.transform.rotation = new Quaternion(
			0f,
			(float) System.Math.Sin(trile.Phi / 2D),
			0f,
			(float) System.Math.Cos(trile.Phi / 2D)
		);

        // Holder not required - nothing happens in Update
        // FezUnityTrileInstance fezHolder = obj.AddComponent<FezUnityTrileInstance>();
        // fezHolder.Fill(trile);
        FezUnityTrileInstance.Fill(obj, trile);
        obj.isStatic = true;
		
		// Fix Unity / Trixel Engine Z direction conflict
		obj.FezZ();
		
		return obj;
	}
	
	public static GameObject GenObject(this ArtObjectInstance ao, GameObject parent = null) {
		GameObject obj = new GameObject(ao.ArtObject.Name);
		if (parent != null) {
			obj.transform.parent = parent.transform;
		}
		obj.transform.localPosition = ao.Position;
        obj.transform.rotation = ao.Rotation;
		obj.transform.localScale = ao.Scale;

        // Holder not required - nothing happens in Update
        // FezUnityArtObjectInstance fezHolder = obj.AddComponent<FezUnityArtObjectInstance>();
        // fezHolder.Fill(ao);
        FezUnityArtObjectInstance.Fill(obj, ao);
        obj.isStatic = true;

        // Fix Unity / Trixel Engine Z direction conflict
        obj.FezZ();
		
		return obj;
	}
	
	public static GameObject GenObject(this BackgroundPlane plane, GameObject parent = null) {
        GameObject obj = new GameObject(plane.TextureName);
		if (parent != null) {
			obj.transform.parent = parent.transform;
		}
		obj.transform.localPosition = plane.Position;
        obj.transform.rotation = plane.Rotation;
		obj.transform.localScale = new Vector3(
			plane.Size.x * plane.Scale.x,
			plane.Size.y * plane.Scale.y,
			plane.Size.z * plane.Scale.z
		);
		
		FezUnityBackgroundPlane fezHolder = obj.AddComponent<FezUnityBackgroundPlane>();
		fezHolder.Fill(plane);
        obj.isStatic = true;

        // Fix Unity / Trixel Engine Z direction conflict
        obj.FezZ();
		
		return obj;
	}

    public static GameObject GenObject(this NpcInstance npc, GameObject parent = null) {
        GameObject obj = new GameObject(npc.Name);
        if (parent != null) {
            obj.transform.parent = parent.transform;
        }
        obj.transform.localPosition = npc.Position;

        FezUnityNpcInstance fezHolder = obj.AddComponent<FezUnityNpcInstance>();
        fezHolder.Fill(npc);
        obj.isStatic = false;

        // Fix Unity / Trixel Engine Z direction conflict
        obj.FezZ();

        return obj;
    }

    public static GameObject GenObject(this SkyLayer layer, FezUnitySky parent, int index, float offsetScale = 1f) {
        float maxSize = Mathf.Max(FezUnityLevel.Current.Level.Size.x, FezUnityLevel.Current.Level.Size.y, FezUnityLevel.Current.Level.Size.z);
        float sizeFactor = Mathf.Max(1f, maxSize / 32f);
        float offset = sizeFactor * (index * 32f + FezManager.Instance.SkySpace) * offsetScale;

        GameObject obj = new GameObject(layer.Name);
        obj.transform.parent = parent.transform;
        obj.transform.localPosition = new Vector3(
            FezUnityLevel.Current.Level.Size.x / 2f,
            FezUnityLevel.Current.Level.Size.y / 2f + offset / 2f,
            FezUnityLevel.Current.Level.Size.z / 2f
        );
        obj.transform.localScale = new Vector3(
            maxSize + offset,
            maxSize + offset,
            maxSize + offset
        );

        FezUnitySkyLayer fezHolder = obj.AddComponent<FezUnitySkyLayer>();
        fezHolder.Fill(parent, layer, index);
        obj.isStatic = true;

        // Fix Unity / Trixel Engine Z direction conflict
        obj.FezZ();

        return obj;
    }

    public static bool IsSkyFullbright(string name) {
        return
            name == "WATERWHEEL" ||
            name == "BLACK" ||
            name == "OUTERSPACE" ||
            name == "GRAVE" ||
            name == "Mine" ||
            name == "ORR_SKY" ||
            name == "ROOTS" ||
            name == "Cave" ||
            name == "CRYPT" ||
            name == "CMY" ||
            name == "MEMORY_GRID" ||
            name == "LOVELINE" ||
            name == "DRAPES";
    }
    public static bool IsSkyExtending(string name) {
        return
            name == "Mine" ||
            name == "WATERWHEEL" ||
            name == "WATERFRONT" ||
            name == "ABOVE";
    }
    public static bool IsSkyClouded(string name) {
        return
            name == "WATERFRONT" ||
            name == "ABOVE";
    }

    // Raw values stolen from FEZ
    public readonly static Dictionary<LiquidType, FezUnityLiquidColorScheme> LiquidColorSchemes = new Dictionary<LiquidType, FezUnityLiquidColorScheme>() {
        { LiquidType.Water, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(61f / 255f, 117f / 255f, 254f / 255f),
            SolidOverlay = new Color(40f / 255f, 76f / 255f, 162f / 255f),
            SubmergedFoam = new Color(91f / 255f, 159f / 255f, 254f / 255f),
            EmergedFoam = new Color(175f / 255f, 205f / 255f, 1f)
        } },
        { LiquidType.Blood, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(174f / 255f, 26f / 255f, 0f),
            SolidOverlay = new Color(84f / 255f, 0f, 21f / 255f),
            SubmergedFoam = new Color(230f / 255f, 81f / 255f, 55f / 255f),
            EmergedFoam = new Color(1f, 1f, 1f)
        } },
        { LiquidType.Sewer, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(82f / 255f, 127f / 255f, 57f / 255f),
            SolidOverlay = new Color(32f / 255f, 70f / 255f, 49f / 255f),
            SubmergedFoam = new Color(174f / 255f, 196f / 255f, 64f / 255f),
            EmergedFoam = new Color(174f / 255f, 196f / 255f, 64f / 255f)
        } },
        { LiquidType.Lava, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(209f / 255f, 0f, 0f),
            SolidOverlay = new Color(150f / 255f, 0f, 0f),
            SubmergedFoam = new Color(1f, 0f, 0f),
            EmergedFoam = new Color(1f, 0f, 0f)
        } },
        { LiquidType.Purple, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(194f / 255f, 1f / 255f, 171f / 255f),
            SolidOverlay = new Color(76f / 255f, 9f / 255f, 103f / 255f),
            SubmergedFoam = new Color(247f / 255f, 52f / 255f, 223f / 255f),
            EmergedFoam = new Color(254f / 255f, 254f / 255f, 254f / 255f)
        } },
        { LiquidType.Green, new FezUnityLiquidColorScheme() {
            LiquidBody = new Color(47f / 255f, 1f, 139f / 255f),
            SolidOverlay = new Color(0f, 167f / 255f, 134f / 255f),
            SubmergedFoam = new Color(0f, 218f / 255f, 175f / 255f),
            EmergedFoam = new Color(184f / 255f, 249f / 255f, 207f / 255f)
        } }
    };

    public static FezUnityLiquidColorScheme GetColorScheme(this LiquidType type) {
        FezUnityLiquidColorScheme scheme;
        if (LiquidColorSchemes.TryGetValue(type, out scheme)) {
            return scheme;
        }
        return null;
    }

}
