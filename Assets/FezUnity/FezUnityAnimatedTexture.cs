using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public class FezUnityAnimatedTexture : MonoBehaviour, IFillable<AnimatedTexture> {
	
	[HideInInspector]
	public AnimatedTexture Animation;
	
	public Material Material;
	
	public void Fill(AnimatedTexture animation) {
		Animation = animation;
	
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		Material = meshRenderer.material;
		
		Material.mainTextureScale = new Vector2(
			Animation.FrameWidth / (float) Material.mainTexture.width,
			Animation.FrameHeight / (float) Material.mainTexture.height
		);
		
		Update();
	}
	
	public void Update() {
		Animation.Timing.Update(TimeSpan.FromSeconds(Time.deltaTime));
		
		Rectangle offset = Animation.Offsets[Animation.Timing.Frame];
		Material.mainTextureOffset = new Vector2(
			offset.X / (float) Material.mainTexture.width,
			offset.Y / (float) Material.mainTexture.height
		);
		
	}
	
}
