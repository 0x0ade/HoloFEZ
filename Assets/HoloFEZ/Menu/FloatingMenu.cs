using UnityEngine;
using System.Collections;

public class FloatingMenu : MonoBehaviour {
	
	public float Radius = 3f;
	public float ItemWidth = 1f;
	public float Spacing = 0.1f;
	
	void Start() {
		
		int children = transform.childCount;
		float fullSpacing = ItemWidth + Spacing;
		for (int i = 0; i < children; i++) {
			Transform childTransform = transform.GetChild(i);
			
			float x = childTransform.localPosition.x / fullSpacing;
			x = x / Mathf.PI + x * Spacing / Radius;
			
			childTransform.localPosition = new Vector3(
				Mathf.Sin(x) * Radius,
				childTransform.localPosition.y,
				Mathf.Cos(x) * Radius
			);
			
			childTransform.LookAt(Camera.main.transform.position);
			Vector3 euler = childTransform.rotation.eulerAngles;
			childTransform.rotation = Quaternion.Euler(
				0f,
				euler.y - 180f,
				0f
			);
			
			FloatingButton button = childTransform.GetComponent<FloatingButton>();
			if (button == null) {
				continue;
			}
			button.PositionOrig = button.transform.position;
		}
		
	}
	
	void Update() {
	
	}
}
