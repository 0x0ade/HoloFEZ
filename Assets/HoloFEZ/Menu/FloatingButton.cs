using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class FloatingButton : MonoBehaviour, IReticleListener {

	public float ScaleFade = 0f;
	public float ScaleFadeSpeed = 0.03f;
	
	public float MoveTime = 0.5f;
	public float MoveOnFocus = 0.3f;
	
	public UnityEvent OnClick = new UnityEvent();
	
	public Vector3 PositionOrig;
	public Vector3 ScaleOrig;
	public Vector2 TextureOffset;
	public Vector2 TextureScale;
	
	public bool Focused;
	
	Vector3 delta;
	float focusedT;
	float focusedTS;
	
	MeshRenderer meshRenderer;
	
	void Start() {
		PositionOrig = transform.position;
		ScaleOrig = transform.localScale;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = Instantiate(meshRenderer.sharedMaterial);

        TextureOffset = meshRenderer.sharedMaterial.mainTextureOffset;
		TextureScale = meshRenderer.sharedMaterial.mainTextureScale;
	}
	
	void Update() {
		transform.position += transform.forward * MoveOnFocus * focusedTS;
		PositionOrig = transform.position;
		
		float focusedTD = Focused ? Time.deltaTime : -Time.deltaTime;
		focusedT = Mathf.Clamp(focusedT * MoveTime + focusedTD, 0f, MoveTime) / MoveTime;
		focusedTS = Mathf.Sin(focusedT * Mathf.PI / 2f);
		
		transform.position = PositionOrig - transform.forward * MoveOnFocus * focusedTS;
		float zoom = (focusedTS - 1f) * MoveOnFocus;
        meshRenderer.sharedMaterial.mainTextureScale = new Vector2(
			TextureScale.x + Mathf.Sign(TextureScale.x) * zoom,
			TextureScale.y + Mathf.Sign(TextureScale.y) * zoom
		);
        zoom = zoom * 0.5f;
        meshRenderer.sharedMaterial.mainTextureOffset = new Vector2(
			TextureOffset.x - Mathf.Sign(TextureScale.x) * zoom,
			TextureOffset.y - Mathf.Sign(TextureScale.y) * zoom
		);
		
		ScaleFade = Mathf.Clamp(ScaleFade + ScaleFadeSpeed * HoloFEZHelper.SpeedF, 0f, 1f);
		float scaleFadeS = Mathf.Sin(ScaleFade * Mathf.PI / 2f);

        meshRenderer.sharedMaterial.color = new Color(
            meshRenderer.sharedMaterial.color.r,
            meshRenderer.sharedMaterial.color.g,
            meshRenderer.sharedMaterial.color.b,
			scaleFadeS
		);
		transform.localScale = new Vector3(
			ScaleOrig.x * scaleFadeS,
			ScaleOrig.y * scaleFadeS,
			ScaleOrig.z * scaleFadeS
		);
	}
	
	public void SetGazedAt(bool gazedAt) {
		Focused = gazedAt;
	}
	
	public void OnGazeEnter() {
		SetGazedAt(true);
	}

	public void OnGazeExit() {
		SetGazedAt(false);
	}

	public void OnGazeTrigger() {
		SetGazedAt(false);
		OnClick.Invoke();
	}
	
}
