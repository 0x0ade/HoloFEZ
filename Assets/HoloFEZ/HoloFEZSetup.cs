using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HoloFEZSetup : MonoBehaviour {

	public InputField PathField;
	public Button ConfirmButton;
	
	// Use this for initialization
	void Start () {
		if (FezManager.FezPath != null) {
			// Something went wrong.
		}
		
		FezManager.FezPath = PlayerPrefs.GetString("FezContentPath");
		
		if (!string.IsNullOrEmpty(FezManager.FezPath)) {
			Continue();
			return;
		}
		
		if (Application.platform == RuntimePlatform.Android) {
			// Default path - FNADroid
			FezManager.FezPath = "/sdcard/Android/data/com.angelde.fnadroid/game/Content";
			if (!Directory.Exists(FezManager.FezPath)) {
				// FNADroid with FEZDroid not found - falling back to "copy to phone"
				FezManager.FezPath = "/sdcard/Content";
			}
		} else {
			FezManager.FezPath = FezFinder.FindFEZ() ?? @"C:\Program Files (x86)\Steam\steamapps\common\FEZ\FEZ.exe";
			FezManager.FezPath = Path.Combine(Directory.GetParent(FezManager.FezPath).FullName, "Content");
		}
		
		PathField.text = FezManager.FezPath = CorrectPath(FezManager.FezPath);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	public void Continue() {
		SceneManager.LoadScene("MainScene");
	}
	
	public void Confirm() {
		FezManager.FezPath = CorrectPath(PathField.text);
		PlayerPrefs.SetString("FezContentPath", FezManager.FezPath);
		Continue();
	}
	
	public static string CorrectPath(string path) {
		if (string.IsNullOrEmpty(path)) {
			return null;
		}
		
		path = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		if (path[path.Length - 1] == Path.DirectorySeparatorChar) {
			path = path.Substring(0, path.Length - 1);
		}
		
		if (path.ToLowerInvariant().EndsWith("fez.exe")) {
			return Path.Combine(Directory.GetParent(path).FullName, "Content");
		}
		if (path.ToLowerInvariant().EndsWith("fez")) {
			return Path.Combine(path, "Content");
		}
		
		return path;
	}
	
}
