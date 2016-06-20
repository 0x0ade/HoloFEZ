using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;
using UnityEngine.Rendering;

public class FezUnityWater : MonoBehaviour, IFillable<LiquidType> {

    [HideInInspector]
    public LiquidType LiquidType;
    [HideInInspector]
    public FezUnityLiquidColorScheme ColorScheme;

    public void Fill(LiquidType type) {
        LiquidType = type;
        ColorScheme = type.GetColorScheme();

        Material materialBase = FezManager.Instance.WaterMaterial;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = FezManager.Instance.WaterMesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = Instantiate(materialBase);

        transform.localScale = new Vector3(
            1000f,
            1f,
            1000f
        );

        meshRenderer.sharedMaterial.SetColor("_LiquidBody", ColorScheme.LiquidBody);
        meshRenderer.sharedMaterial.SetColor("_SolidOverlay", ColorScheme.SolidOverlay);
        meshRenderer.sharedMaterial.SetTextureScale("_NoiseTex", new Vector2(1000f, 1000f));
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    }

}   
