using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;

public class Reticle : MonoBehaviour {

    readonly static System.Type t_object = typeof(object);
    readonly static System.Type t_IReticleListener = typeof(IReticleListener);

    public static Reticle Instance;
    public Reticle() {
        Instance = this;
    }

    protected bool loading_;
    public bool Loading {
        get {
            return loading_;
        }
        set {
            if (loading_ != value) {
                if (value) {
                    material.EnableKeyword("loading_jump");
                } else {
                    material.DisableKeyword("loading_jump");
                }
            }

            loading_ = value;
        }
    }

    public GameObject Focused {
        get;
        private set;
    }

    protected bool triggered_;
    public bool Triggered {
        get {
            return triggered_;
        }
        set {
            if (triggered_ != value && value) {
                Trigger();
            }
        }
    }

    public Vector3 DefaultPosition = new Vector3(0f, 0f, 5f);
    public float SizeOnScreen = 128f;

    public float ScaleFade = 0f;
    public float ScaleFadeSpeed = 0.05f;

    public float OuterRadius = 0.05f;
    public float InnerRadius = 0.05f;
    public Color Color = new Color(1f, 1f, 1f, 0.1f);

    public float OuterRadiusLarge = 0.1f;
    public float InnerRadiusLarge = 0.03f;
    public Color ColorLarge = new Color(1f, 1f, 1f, 1f);

    Material material;

    void Start() {
        SizeOnScreen = SizeOnScreen * (Camera.main.ViewportToScreenPoint(Vector3.one).y / 512f);

        GetComponent<MeshRenderer>().material = Instantiate(GetComponent<MeshRenderer>().sharedMaterial);
        material = GetComponent<MeshRenderer>().sharedMaterial;
    }
	
	void Update() {
        triggered_ = false;
        bool large = Loading;

        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo)) {
            transform.position = hitInfo.point;
            large = true;
            
            Focus(hitInfo.collider.gameObject);
        } else {
            transform.localPosition = DefaultPosition;

            Unfocus();
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        transform.localScale = Vector3.one * (
            Camera.main.ScreenToWorldPoint(screenPos) -
            Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y + SizeOnScreen, screenPos.z))
        ).magnitude;

        ScaleFade = Mathf.Clamp(ScaleFade + (large ? ScaleFadeSpeed : -ScaleFadeSpeed) * HoloFEZHelper.SpeedF, 0f, 1f);
        float scaleFadeS = Mathf.Sin(ScaleFade * Mathf.PI / 2f);

        material.color = Color.Lerp(Color, ColorLarge, scaleFadeS);
        material.SetFloat("_RadiusOuter", Mathf.Lerp(OuterRadius, OuterRadiusLarge, scaleFadeS));
        material.SetFloat("_RadiusInner", Mathf.Lerp(InnerRadius, InnerRadiusLarge, scaleFadeS));
    }

    public void Unfocus() {
        if (Focused == null) {
            return;
        }

        Component[] components = Focused.GetComponents(t_IReticleListener);
        bool hasListener = false;
        for (int i = 0; i < components.Length; i++) {
            if (!(components[i] is IReticleListener)) {
                continue;
            }
            hasListener = true;
            ((IReticleListener) components[i]).OnGazeExit();
        }

        if (!hasListener) {
            EventTrigger trigger = Focused.GetComponent<EventTrigger>();
            if (trigger != null) {
                trigger.OnPointerExit(null);
            }
        }

        Focused = null;
    }

    public void Focus(GameObject focused) {
        if (Focused == focused) {
            return;
        }

        if (Focused != null) {
            Unfocus();
        }

        Component[] components = focused.GetComponents(t_IReticleListener);
        bool hasListener = false;
        for (int i = 0; i < components.Length; i++) {
            if (!(components[i] is IReticleListener)) {
                continue;
            }
            hasListener = true;
            ((IReticleListener) components[i]).OnGazeEnter();
        }

        if (!hasListener) {
            EventTrigger trigger = focused.GetComponent<EventTrigger>();
            if (trigger != null) {
                trigger.OnPointerEnter(null);
            }
        }

        Focused = focused;
    }

    public void Trigger() {
        triggered_ = true;
        if (Focused == null) {
            return;
        }

        Component[] components = Focused.GetComponents(t_IReticleListener);
        bool hasListener = false;
        for (int i = 0; i < components.Length; i++) {
            if (!(components[i] is IReticleListener)) {
                continue;
            }
            hasListener = true;
            ((IReticleListener) components[i]).OnGazeTrigger();
        }

        if (!hasListener) {
            EventTrigger trigger = Focused.GetComponent<EventTrigger>();
            if (trigger != null) {
                trigger.OnPointerClick(null);
            }
        }
    }

}
