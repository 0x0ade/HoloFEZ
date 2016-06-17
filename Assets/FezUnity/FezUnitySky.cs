using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public class FezUnitySky : MonoBehaviour, IFillable<Sky>, IFillable<FezUnitySky> {

    public readonly bool IsInstance;

	public string Name {
		get {
			return Sky.Name;
		}
		set {
            Sky.Name = value;
		}
	}

    public bool IsFullbright { get; protected set; }

    public Texture2D Texture;
    public Color[] FogColors;

    // TODO clouds
    // TODO layers

    [HideInInspector]
    public GameObject[] Layers;

    public Sky Sky;
	
	public FezUnitySky() {
        IsInstance = true;
    }

    public FezUnitySky(Sky sky) {
        IsInstance = false;

        Fill(sky);
    }

    public void Fill(Sky sky) {
        Sky = sky;
        IsFullbright = FezHelper.IsSkyFullbright(Name);

        Texture = FezManager.Instance.GetTexture("skies/" + sky.Name + "/" + sky.Background) as Texture2D;
        FogColors = Texture.GetPixels(0, (int) (Texture.height / 2f), Texture.width, 1);

        //Horizontal repeat, vertical clamp
        Texture2D fix = new Texture2D(Texture.width, Texture.height + 2, Texture.format, false);
        fix.SetPixels(0, 0, Texture.width, 1, Texture.GetPixels(0, 0, Texture.width, 1)); // Upper clamp
        fix.SetPixels(0, 1, Texture.width, Texture.height, Texture.GetPixels(0, 0, Texture.width, Texture.height)); // Main gradient
        fix.SetPixels(0, fix.height - 1, Texture.width, 1, Texture.GetPixels(0, Texture.height - 1, Texture.width, 1)); // Lower clamp
        fix.Apply(false, false);
        DestroyImmediate(Texture);
        Texture = fix;

        Texture.wrapMode = TextureWrapMode.Repeat;

        Fill();
    }

    public void Fill(FezUnitySky sky) {
        Sky = sky.Sky;
        Texture = sky.Texture;
        FogColors = sky.FogColors;

        Fill();
    }

    static bool skyInstantiated = false;
    public void Fill() {
        if (!IsInstance) {
            return;
        }

        Layers = new GameObject[Sky.Layers.Count];
        int indexOffset = 0;
        bool isOuterSpace = Name == "OUTERSPACE";
        float offsetScale = FezHelper.IsSkyClouded(Name) ? 2f : 1f;
        for (int i = 0; i < Layers.Length; i++) {
            SkyLayer layer = Sky.Layers[i];

            if (!isOuterSpace && layer.Name.StartsWith("OUTERSPACE")) {
                // TODO handle space layer separately
                //indexOffset++;
                //continue;
            }

            GameObject layerObj = layer.GenObject(this, Layers.Length - (i - indexOffset), offsetScale);
            layerObj.transform.parent = transform;
            Layers[i] = layerObj;
        }

        if (!skyInstantiated) {
            skyInstantiated = true;
            RenderSettings.skybox = Instantiate(RenderSettings.skybox);
        }
    }

    private readonly static Vector3 onethirdV3 = new Vector3(1f/3f, 1f/3f, 1f/3f);
    public void Update() {
        // TODO add further special cases
        if (IsFullbright) {
            // This "sky" is "inside"
            FezManager.Instance.Sun.gameObject.SetActive(false);
            RenderSettings.fogColor = FogColors[0] + new Color(0.27f, 0.26f, 0.25f);
            RenderSettings.ambientLight = RenderSettings.fogColor * 3f;
            return;
        }
        FezManager.Instance.Sun.gameObject.SetActive(true);

        Color diffuse = FezManager.Instance.Sun.color;
        Color ambient = RenderSettings.ambientLight;

        // Stolen from FEZ
        float fogIndex = (FezManager.Instance.TimeRelative * FogColors.Length) % FogColors.Length;
        Color fog = Color.Lerp(FogColors[Math.Max((int) Mathf.Floor(fogIndex), 0)], FogColors[Math.Min((int) Mathf.Ceil(fogIndex), FogColors.Length - 1)], fogIndex % 1f);
        float ambientFactor = Math.Max(Vector3.Dot(new Vector3(fog.r, fog.g, fog.b), onethirdV3), 0.1f);
        diffuse = Color.Lerp(diffuse, fog, FezManager.Instance.NightContribution * 0.4f);
        ambient = Sky.FoliageShadows ? Color.Lerp(ambient, Color.white, FezManager.Instance.NightContribution * 0.5f) : Color.Lerp(ambient, Color.Lerp(fog, Color.white, 0.5f), FezManager.Instance.NightContribution * 0.5f);
        ambient = Color.Lerp(ambient, fog, 0.14375f);

        // Further color manipulation
        float brightness = (diffuse.r + diffuse.g + diffuse.b * 2f + ambient.r + ambient.g + ambient.b * 2f) / 6f;
        float brightnessTime = 0.8f - FezManager.Instance.NightContribution * 0.6f - FezManager.Instance.DawnContribution * 0.2f - FezManager.Instance.DuskContribution * 0.2f;

        // Blend diffuse and ambient (more "natural")
        FezManager.Instance.Sun.color = diffuse;
        RenderSettings.ambientLight = ambient;
        diffuse = Color.Lerp(FezManager.Instance.Sun.color, RenderSettings.ambientLight, 0.2f + FezManager.Instance.NightContribution * 0.4f);
        ambient = Color.Lerp(FezManager.Instance.Sun.color, RenderSettings.ambientLight, 0.8f - FezManager.Instance.NightContribution * 0.4f);
        if (FezUnityLevel.Current != null) {
            float baseIntensity = 1f - RenderSettings.ambientIntensity;
            FezManager.Instance.Sun.intensity = baseIntensity - FezManager.Instance.NightContribution * baseIntensity * 0.9f;
        }

        // Partially desaturate color
        diffuse = Color.Lerp(diffuse, new Color(
                diffuse.r * 0.7f + diffuse.g * 0.05f + diffuse.b * 0.25f,
                diffuse.r * 0.1f + diffuse.g * 0.6f + diffuse.b * 0.3f,
                diffuse.r * 0.3f + diffuse.g * 0.2f + diffuse.b * 0.5f
            ), 0.3f);
        ambient = Color.Lerp(ambient, new Color(
                ambient.r * 0.8f + ambient.g * 0.15f + ambient.b * 0.05f,
                ambient.r * 0.1f + ambient.g * 0.7f + ambient.b * 0.2f,
                ambient.r * 0.1f + ambient.g * 0.1f + ambient.b * 0.8f
            ), 0.3f);
        
        // Blend to brightness / darkness, further desaturating and fakely highening the range
        diffuse = Color.Lerp(diffuse, Color.white, brightnessTime * 0.6f);
        diffuse = Color.Lerp(diffuse, Color.black, FezManager.Instance.NightContribution * 0.4f);
        ambient = Color.Lerp(ambient, Color.black, brightness * 0.4f);
        ambient = Color.Lerp(ambient, Color.white, brightnessTime + brightness * 0.5f);

        // Blend with original
        diffuse = Color.Lerp(diffuse, FezManager.Instance.Sun.color, 0.3f);
        ambient = Color.Lerp(ambient, RenderSettings.ambientLight, 0.3f);

        // Unity 5.4 got rid of ambient intensity... and changed half of the lighting (at least feels like that)
        FezManager.Instance.Sun.color = diffuse * 1.1f;
        RenderSettings.ambientLight = ambient * (RenderSettings.ambientIntensity + 0.15f);
        RenderSettings.fogColor = fog;

        // Update sun rotation
        FezManager.Instance.Sun.transform.rotation = Quaternion.Euler(180f * FezManager.Instance.TimeRelative, -360f * FezManager.Instance.TimeRelative, 0f);
    }

}
