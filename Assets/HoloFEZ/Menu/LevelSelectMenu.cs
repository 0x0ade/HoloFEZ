using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FmbLib;

public class LevelSelectMenu : MonoBehaviour {
	
	public Shader ButtonShader;
    public Texture2D MaskTexture;
	
	FloatingButton[] buttons;
	
	float timeMin = -0.5f;
	float sinceStart = -0.5f;
	float scaleFadeSpeed = 3f;
	
	Switch switching = Switch.None;
	
	int levelIndex = 0;
	List<string> levels = new List<string>();
	List<Material> levelMaterials = new List<Material>();
	
	void Start() {
		foreach (string assetName in AssetMetadata.Map.Keys) {
			if (!assetName.StartsWith("levels\\")) {
				continue;
			}

            string level = assetName.Substring(7);
			Material material;
			
			try {
				material =
					Instantiate((FmbUtil.ReadObject(
						FezManager.Instance.ReadFromPack("other textures\\map_screens\\" + level)
					) as Texture2D).GenMaterial(ButtonShader));
                material.SetTexture("_MaskTex", MaskTexture);
			} catch {
                // Normally hidden / inaccessible level
                continue;
			}
			
			material.mainTextureScale = new Vector2(
				1f,
				-1f
			);
			
			levels.Add(level);
			levelMaterials.Add(material);
		}
		
		buttons = new FloatingButton[transform.childCount];
		for (int i = 0; i < buttons.Length; i++) {
			Transform childTransform = transform.GetChild(i);
			
			FloatingButton button = childTransform.GetComponent<FloatingButton>();
			if (button == null) {
				continue;
			}
			buttons[i] = button;
			button.ScaleFadeSpeed = 0f;
			
			if (i <= 0 || buttons.Length - 1 <= i) {
				continue;
			}
			
			UpdateButton(button, i - 1 + levelIndex);
		}
		
	}
	
	void Update() {
		if (switching == Switch.Level && !HoloFEZPlayer.Instance.Teleporting) {
			switching = Switch.None;
		}
		
		float scaleFadeSpeedAbs = scaleFadeSpeed;
		if (scaleFadeSpeed < 0f) {
			scaleFadeSpeedAbs = -scaleFadeSpeed;
			if (sinceStart <= timeMin) {
				int levelButtons = buttons.Length - 2;
				levelIndex +=
					switching == Switch.Previous ? -levelButtons :
					switching == Switch.Next ? levelButtons :
					0;
                if (levelIndex < 0) {
                    levelIndex = levels.Count + levelIndex;
                }
				switching = Switch.None;
				UpdateButtons();
				scaleFadeSpeed = -scaleFadeSpeed;
                SetButtonPhysics(true);
            }
		}
		
		sinceStart = Mathf.Clamp(sinceStart + Time.deltaTime * Mathf.Sign(scaleFadeSpeed), timeMin, buttons.Length * 0.5f / scaleFadeSpeedAbs + 0.5f);
		
		for (int i = 0; i < buttons.Length; i++) {
			FloatingButton button = buttons[i];
			if (button == null) {
				continue;
			}
			button.ScaleFade = sinceStart * scaleFadeSpeedAbs - i * 0.5f;
		}
		
	}
	
	public void UpdateButtons() {
		for (int i = 1; i < buttons.Length - 1; i++) {
			FloatingButton button = buttons[i];
			if (button == null) {
				continue;
			}
			UpdateButton(button, i - 1 + levelIndex);
		}
	}

    public void SetButtonPhysics(bool enabled) {
        for (int i = 0; i < buttons.Length; i++) {
            FloatingButton button = buttons[i];
            if (button == null) {
                continue;
            }
            button.GetComponent<Collider>().enabled = enabled;
        }
    }
	
	public void UpdateButton(FloatingButton button, int levelID) {
		if (levelID < 0) {
			levelID = levels.Count - levelID;
		}
		levelID %= levels.Count;

        Material material = levelMaterials[levelID];
        button.TextureScale = new Vector2(
            Mathf.Sign(material.mainTextureScale.x),
            Mathf.Sign(material.mainTextureScale.y)
        );
        button.GetComponent<MeshRenderer>().sharedMaterial = material;
		button.OnClick.RemoveAllListeners();
		button.OnClick.AddListener(delegate() {
			if (switching != Switch.None) {
				return;
			}
			
			switching = Switch.Level;
            SetButtonPhysics(false);

            HoloFEZPlayer.Instance.SelectLevel(levels[levelID], delegate() {
                SetButtonPhysics(true);
            });
		});
	}
	
	public void Next() {
		if (switching != Switch.None) {
			return;
		}
		switching = Switch.Next;
		scaleFadeSpeed = -scaleFadeSpeed;
        SetButtonPhysics(false);
    }
	
	public void Previous() {
		if (switching != Switch.None) {
			return;
		}
		switching = Switch.Previous;
		scaleFadeSpeed = -scaleFadeSpeed;
        SetButtonPhysics(false);
    }
	
	enum Switch {
		None,
		Previous,
		Next,
		Level
	}
	
}
