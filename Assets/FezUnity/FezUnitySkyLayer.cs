using UnityEngine;
using System.Collections.Generic;
using FezEngine.Structure;
using UnityEngine.Rendering;

public class FezUnitySkyLayer : MonoBehaviour, IFillable<FezUnitySky, SkyLayer, int> {

    public static float Scale = 10f;

	[HideInInspector]
	public SkyLayer Layer;
	
	public Material Material;
	
	public void Fill(FezUnitySky sky, SkyLayer layer, int index) {
		Layer = layer;

        Material materialBase = FezManager.Instance.SkyLayerMaterial;
        if (sky.Name == "OUTERSPACE" ||
            sky.Name == "GRAVE" ||
            sky.Name == "Mine" ||
            sky.Name == "ORR_SKY" ||
            sky.Name == "ROOTS" ||
            sky.Name == "Cave" ||
            sky.Name == "CRYPT" ||
            sky.Name == "CMY" ||
            sky.Name == "MEMORY_GRID" ||
            sky.Name == "LOVELINE" ||
            sky.Name == "DRAPES") {
            // Full-bright sky (no fog, lighting) looks better
            materialBase = FezManager.Instance.SkyLayerFullbrightMaterial;
        }

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = FezManager.Instance.SkyLayerMesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Texture2D tex = FezManager.Instance.GetTexture2D("skies/" + sky.Name + "/" + layer.Name);
        meshRenderer.sharedMaterial = Instantiate(tex.GenMaterial(materialBase));

        transform.localScale = new Vector3(
            transform.localScale.x,
            transform.localScale.y * Scale,
            transform.localScale.z
        );
        float ratio = tex.width / tex.height;
        meshRenderer.sharedMaterial.mainTextureScale = new Vector2(1f, Scale * ratio);

        meshRenderer.sharedMaterial.color = new Color(1f, 1f, 1f, layer.Opacity);

        // TODO add further special cases
        if (sky.Name == "Mine" ||
            sky.Name == "WATERWHEEL" ||
            sky.Name == "WATERFRONT" ||
            sky.Name == "ABOVE") {
            // The ceiling / bottom should extend, not repeat
            meshRenderer.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
            meshRenderer.sharedMaterial.mainTextureOffset = new Vector2(0f, -meshRenderer.sharedMaterial.mainTextureScale.y / 2f + 1f);
        } else {
            // The sky layer repeats into infinity
            meshRenderer.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Repeat;
        }

        if (sky.Name == "OUTERSPACE") {
            // Scale the repeating star texture properly
            meshRenderer.sharedMaterial.mainTextureScale = new Vector2(
                (index / 4f) * 16f * meshRenderer.sharedMaterial.mainTextureScale.x,
                (index / 4f) * 16f * meshRenderer.sharedMaterial.mainTextureScale.y
            );
        }

        if (sky.Name == "ABOVE") {
            // Flip so it's correct (everything else looks better with the extension downwards)
            meshRenderer.sharedMaterial.mainTextureOffset = new Vector2(
                meshRenderer.sharedMaterial.mainTextureOffset.x,
                meshRenderer.sharedMaterial.mainTextureScale.y + meshRenderer.sharedMaterial.mainTextureOffset.y - 1f
            );
            meshRenderer.sharedMaterial.mainTextureScale = new Vector2(
                meshRenderer.sharedMaterial.mainTextureScale.x,
                -meshRenderer.sharedMaterial.mainTextureScale.y
            );
        }

        if (sky.Name == "GRAVE") {
            // Offset the various layers to add variance in FPV
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                transform.localPosition.y + index * 8f * Scale,
                transform.localPosition.z
            );
        }

        // Fix shadow artifacting
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

    }

}
