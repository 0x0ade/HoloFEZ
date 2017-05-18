using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;
using FezEngine.Structure;

public class HoloFEZPlayer : MonoBehaviour {

	public static HoloFEZPlayer Instance;
	
	public HoloFEZPlayer() {
		Instance = this;
	}
	
	public float Speed = 0.05f;
	public bool Frozen = false;
	[HideInInspector]
	public bool Moving = false;
	
	public float TeleportFadeSpeed = 0.05f;
	[HideInInspector]
	public bool Teleporting = false;

    public GameObject VRControls;
	public GameObject MainMenuControls;
	public GameObject InGameControls;
    public Reticle Reticle;

    FloatingButton[] inGameButtons;

    public SimpleSmoothMouseLook mouseLook;

    bool vr;
    bool seated;

    float lastRecalibratedYRot;
    Quaternion nullRotation = Quaternion.Euler(0f, 0f, 0f);

    bool forceNoVR;

    void Start() {
        seated = OpenVR.ChaperoneSetup == null;

        // FIXME Currently no Chaperone / SteamVR support
        seated = true;

        if (VRControls == null) {
			VRControls = GameObject.Find("VR Controls");
		}
		if (MainMenuControls == null) {
			MainMenuControls = GameObject.Find("Main Menu Controls");
		}
		if (InGameControls == null) {
			InGameControls = GameObject.Find("In-Game Controls");
		}
        if (Reticle == null) {
            Reticle = GameObject.Find("Reticle").GetComponent<Reticle>();
        }

        // Fix level select height displacement
        if (vr) {
            MainMenuControls.transform.localPosition = new Vector3(
                0f,
                0.5f,
                0f
            );
        }

        //InGameControls.transform.parent = Camera.main.transform;
        inGameButtons = new FloatingButton[InGameControls.transform.childCount];
		for (int i = 0; i < inGameButtons.Length; i++) {
			Transform childTransform = InGameControls.transform.GetChild(i);
			
			FloatingButton button = childTransform.GetComponent<FloatingButton>();
			if (button == null) {
				continue;
			}
			inGameButtons[i] = button;
			button.ScaleFadeSpeed = 0f;
		}
		InGameControls.SetActive(false);

        FezUnityNpcInstance.DefaultTalk = NPCTalk;
        FezUnityNpcInstance.DefaultStopTalking = NPCStopTalking;

        SwitchLevel(null);
	}
	
	void Update() {
        if (Input.GetButtonDown("ToggleVR"))
            forceNoVR = !forceNoVR;

        vr = OpenVR.IsHmdPresent() && !forceNoVR;
        InGameControls.SetActive(vr && seated);
        if (mouseLook == null)
            mouseLook = Camera.main.GetComponent<SimpleSmoothMouseLook>();
        if (mouseLook != null)
            mouseLook.enabled = !vr;

        if (Input.GetButtonDown("Trigger")) {
            Reticle.Triggered = true;
        }
        if (!Frozen && Input.GetButtonDown("Home")) {
            SelectLevel(null);
        }
        if (vr && seated) {
            // TODO timer-based reticle... in Reticle.cs
        }

        // Google Cardboard Earth demo - styled movement
        /*
        if (Reticle.Triggered && Reticle.Focused == null) {
			Moving = !Moving;
			if (!Frozen) {
				for (int i = 0; i < inGameButtons.Length; i++) {
					FloatingButton button = inGameButtons[i];
					if (button == null) {
						continue;
					}
					button.ScaleFadeSpeed = Moving ? -0.1f : 0.1f;
				}
			}
		}

        if (Moving && !Frozen) {
			transform.position += Camera.main.transform.forward * Speed * HoloFEZHelper.SpeedF;
		}
        */

        // Seated gamepad movement
        if (seated && !Frozen) {
            Vector3 dir = Vector3.zero;

            dir += Camera.main.transform.forward * Input.GetAxis("Vertical");

            float angleY = Camera.main.transform.rotation.eulerAngles.y;
            angleY = (angleY + 90f) / 180f * Mathf.PI;
            dir += new Vector3(Mathf.Sin(angleY), 0f, Mathf.Cos(angleY)) * Input.GetAxis("Horizontal");

            if (dir != Vector3.zero) {
                dir.Normalize();
                transform.position += dir * Speed * HoloFEZHelper.SpeedF;
            }

            transform.position += Vector3.up * Input.GetAxis("Y Movement") * Speed * HoloFEZHelper.SpeedF;
        }

        // Seated recalibration
        if (seated && Input.GetButtonDown("Recalibrate")) {
            transform.rotation = Quaternion.Euler(0f, lastRecalibratedYRot = (Camera.main.transform.rotation.eulerAngles.y - lastRecalibratedYRot) - 90f, 0f);
            VRControls.transform.rotation = nullRotation;
        }
		
        FixedUpdate();
    }
	
	void FixedUpdate() {
		Vector3 camEuler = Camera.main.transform.rotation.eulerAngles;
		InGameControls.transform.rotation = Quaternion.Euler(
			0f,
			camEuler.y,
			0f
		);
	}

	public void BackToMainMenu(System.Action cb = null) {
		SelectLevel(null, cb);
	}
	public void SelectLevel(string levelName, System.Action cb = null) {
        if (string.IsNullOrEmpty(levelName)) {
            StartCoroutine(TeleportCoroutine(delegate() {
                SwitchLevel(levelName);
                if (cb != null) {
                    cb();
                }
            }));
            return;
        }

        Reticle.Loading = true;

        FezUnityLevel.SetupLightingOnFill = false;
        FezManager.Instance.LoadLevelAsync(levelName, delegate(FezUnityLevel level) {
            level.gameObject.SetActive(false);
            StartCoroutine(TeleportCoroutine(delegate() {
                level.gameObject.SetActive(true);
                level.SetupLighting();
                Reticle.Loading = false;

                Frozen = false;
                MainMenuControls.SetActive(false);
                if (cb != null) {
                    cb();
                }
            }));
        });

        //Reticle.Loading = false;

    }

    readonly static Color clearwhite = new Color(1f, 1f, 1f, 0f);
	public IEnumerator TeleportCoroutine(System.Action cb) {
        if (Teleporting) {
            cb();
			yield break;
		}

        Teleporting = true;

        ;
        for (float f = 0f; f <= 1f; f += TeleportFadeSpeed * HoloFEZHelper.SpeedF) {
            SteamVR_Fade.Start(new Color(
                1f,
                1f,
                1f,
                f
            ), 0f, true);
            yield return null;
		}

        cb();
		
		if (Frozen) {
			InGameControls.SetActive(false);
		}
		
		for (float f = 0f; f <= 1f; f += TeleportFadeSpeed * HoloFEZHelper.SpeedF) {
            SteamVR_Fade.Start(new Color(
                1f,
                1f,
                1f,
                1f - f
            ), 0f, true);
            yield return null;
		}
        SteamVR_Fade.Start(clearwhite, 0f, true);

        if (!Frozen) {
			for (int i = 0; i < inGameButtons.Length; i++) {
				FloatingButton button = inGameButtons[i];
				if (button == null) {
					continue;
				}
				button.transform.localScale = Vector3.zero;
				button.ScaleFade = 0f;
				button.ScaleFadeSpeed = 0.1f;
			}
			InGameControls.SetActive(vr && seated);
		}
		
		Teleporting = false;
	}
	
	public void SwitchLevel(string levelName) {
		Moving = false;

		if (string.IsNullOrEmpty(levelName)) {
			Frozen = true;
			MainMenuControls.SetActive(true);
			InGameControls.SetActive(false);

            FezUnityLevel.SetupLightingOnFill = true;
            FezManager.Instance.LoadLevel("FOX");

            // Replace the monolith AO
            GameObject ao = GameObject.Find("zuish_monolith_AAO");
            transform.position = ao.transform.position + new Vector3(
                0f,
                0.5f,
                1f
            );
            Destroy(ao);

            return;
		}

        FezUnityLevel.SetupLightingOnFill = false;
        FezManager.Instance.LoadLevelAsync(levelName, delegate(FezUnityLevel level) {
            Frozen = false;
            MainMenuControls.SetActive(false);
            InGameControls.SetActive(vr && seated);
            level.SetupLighting();
        });
	}

    public void Exit() {
        Application.Quit();
    }

    public void NPCTalk(FezUnityNpcInstance self) {
        if ((transform.position - self.transform.position).sqrMagnitude < 25f) {
            self.CurrentAction = NpcAction.Talk;
        }


    }

    public bool NPCStopTalking(FezUnityNpcInstance self) {
        if (36f < (transform.position - self.transform.position).sqrMagnitude) {
            return true;
        }
        return false;
    }

}
