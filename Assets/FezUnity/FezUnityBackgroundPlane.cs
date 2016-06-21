using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public class FezUnityBackgroundPlane : MonoBehaviour, IFillable<BackgroundPlane> {
	
	[HideInInspector]
	public BackgroundPlane Plane;
	
	public void Fill(BackgroundPlane plane) {
		Plane = plane;
		
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = FezManager.Instance.BackgroundPlaneMesh;
		
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		object tex = FezManager.Instance.GetTextureOrOther("background planes/" + plane.TextureName);
		Texture2D tex2D = tex as Texture2D;
		Material materialBase;
		if (plane.Doublesided || plane.Billboard) {
			if (plane.Fullbright) {
                materialBase = FezManager.Instance.BackgroundPlaneFullbrightMaterial;
			} else {
                materialBase = FezManager.Instance.BackgroundPlaneMaterial;
			}
		} else {
			if (plane.Fullbright) {
                materialBase = FezManager.Instance.BackgroundPlaneFullbrightOnesidedMaterial;
			} else {
                materialBase = FezManager.Instance.BackgroundPlaneOnesidedMaterial;
			}
		}
		
		if (tex is AnimatedTexture) {
			AnimatedTexture texAnim = (AnimatedTexture) tex;
			texAnim.Timing.Loop = plane.Loop;
			
			plane.Texture = tex2D = texAnim.Texture;
			meshRenderer.material = Instantiate(tex2D.GenMaterial(materialBase));
			
			FezUnityAnimatedTexture animation = gameObject.AddComponent<FezUnityAnimatedTexture>();
			animation.Fill(texAnim);
			
		} else {
			plane.Texture = tex2D;
			meshRenderer.material = Instantiate(tex2D.GenMaterial(materialBase));
		}

        meshRenderer.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp; // Fixes bleeding edges in some background planes

        meshRenderer.sharedMaterial.SetVector("_PlaneScale", new Vector4(plane.Scale.x, plane.Scale.y, 0f, 0f));
        if (plane.TextureName == "dent_square") {
            meshRenderer.sharedMaterial.EnableKeyword("_PlaneClamp");
        }
    }
	
	public void Update() {
		if (Plane.Billboard) {
			transform.LookAt(Camera.main.transform.position);
        }
		
	}
	
}
