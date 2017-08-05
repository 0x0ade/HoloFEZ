using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Tools;
using FmbLib;
using System.IO;
using UnityEngine.UI;
using System.Collections;

public class FezUnityNpcInstance : MonoBehaviour, IFillable<NpcInstance> {

    public static Func<FezUnityNpcInstance, bool> DefaultShouldStartTalking = _ShouldStartTalking;
    public static Action<FezUnityNpcInstance> DefaultTalking = _Talking;
    public static Func<FezUnityNpcInstance, bool> DefaultShouldStopTalking = _ShouldStopTalking;

    [HideInInspector]
    public NpcInstance NPC;

    public GameObject SpeechBubble;
    public CanvasGroup SpeechBubbleCanvasGroup;

    public Dictionary<NpcAction, FezUnityAnimatedTexture> Animations = new Dictionary<NpcAction, FezUnityAnimatedTexture>();

    public bool LookingRight = true;
    public float WalkStep = 0f;

    MeshRenderer meshRenderer;

    public float TimeSinceActionChange;
    public float TimeUntilActionChange;
    public float WalkedDistance;

    public bool CanIdle;
    public bool CanIdle2;
    public bool CanIdle3;
    public bool CanWalk;
    public bool CanTalk;
    public bool CanTurn;

    public int CurrentTextLine = -1;
    private float _CurrentTextOpacity = 0f;
    private float CurrentTextOpacity {
        get {
            return _CurrentTextOpacity;
        }
        set {
            _CurrentTextOpacity = value;
            SpeechBubbleCanvasGroup.alpha = value;
        }
    }

    NpcAction _currentAction;
    public NpcAction CurrentAction {
        get {
            return _currentAction;
        }
        set {
            if (_currentAction != value && Animations.Count != 0) {
                FezUnityAnimatedTexture animationOld;
                if (Animations.TryGetValue(_currentAction, out animationOld)) {
                    animationOld.enabled = false;
                }

                if (_currentAction != NpcAction.None) {
                    transform.localPosition -= new Vector3(0f, transform.localScale.y / 2f, 0f);
                }

                if (value != NpcAction.None) {
                    FezUnityAnimatedTexture animation = Animations[value];
                    animation.Animation.Timing.Restart();
                    CurrentAnimation = animation;
                    CurrentTiming = animation.Animation.Timing;
                    animation.enabled = true;
                    meshRenderer.sharedMaterial = animation.Material;
                    transform.localScale = new Vector3(
                        animation.Animation.FrameWidth / 16f,
                        animation.Animation.FrameHeight / 16f,
                        1f
                    );
                    transform.localPosition += new Vector3(0f, transform.localScale.y / 2f, 0f);
                }
            }

            _currentAction = value;
        }
    }

    public FezUnityAnimatedTexture CurrentAnimation { get; protected set; }
    public AnimationTiming CurrentTiming { get; protected set; }

    public void Fill(NpcInstance npc) {
        Animations.Clear();
        CurrentAction = NpcAction.None;

        NPC = npc;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = FezManager.Instance.BackgroundPlaneMesh;

        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        foreach (NpcAction action in Enum.GetValues(typeof(NpcAction))) {
            if (action == NpcAction.None ||
                action == NpcAction.Walk ||
                action == NpcAction.Idle ||
                action == NpcAction.Talk) {
                continue;
            }
            if (!NPC.Actions.ContainsKey(action) && FezManager.Instance.AssetExists("character animations/" + NPC.Name + "/" + action)) {
                NPC.Actions.Add(action, new NpcActionContent() {
                    AnimationName = action.ToString()
                });
            }
        }

        foreach (KeyValuePair<NpcAction, NpcActionContent> pair in NPC.Actions) {
            NpcAction action = pair.Key;
            NpcActionContent actionContent = pair.Value;

            AnimatedTexture texAnim = FezManager.Instance.GetTextureOrOther("character animations/" + NPC.Name + "/" + actionContent.AnimationName) as AnimatedTexture;
            texAnim.Timing.Loop = true;
            texAnim.Timing.Loop =
                action != NpcAction.Idle2 &&
                action != NpcAction.Turn &&
                action != NpcAction.Burrow &&
                action != NpcAction.Hide &&
                action != NpcAction.ComeOut &&
                action != NpcAction.TakeOff &&
                action != NpcAction.Land;
            actionContent.Animation = texAnim;

            Texture2D tex2D = texAnim.Texture;
            meshRenderer.material = Instantiate(tex2D.GenMaterial(FezManager.Instance.BackgroundPlaneMaterial));
            meshRenderer.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;

            FezUnityAnimatedTexture animation = gameObject.AddComponent<FezUnityAnimatedTexture>();
            animation.Fill(texAnim);
            animation.enabled = false;

            Animations[action] = animation;
        }

        using (BinaryReader metadataReader = FezManager.Instance.ReadFromPack("character animations/" + NPC.Name + "/metadata")) {
            if (metadataReader != null) {
                npc.FillMetadata(FmbUtil.ReadObject(metadataReader) as NpcMetadata);
            }
        }

        CanIdle = Animations.ContainsKey(NpcAction.Idle);
        CanIdle2 = Animations.ContainsKey(NpcAction.Idle2);
        CanIdle3 = Animations.ContainsKey(NpcAction.Idle3);
        CanWalk = Animations.ContainsKey(NpcAction.Walk);
        CanTalk = Animations.ContainsKey(NpcAction.Talk);
        CanTurn = Animations.ContainsKey(NpcAction.Turn);

        CurrentAction = CanIdle ? NpcAction.Idle : NpcAction.Walk;

        if (!CanTalk) {
            return;
        }

        SpeechBubble = new GameObject("Speech Bubble");
        SpeechBubble.transform.parent = transform;
        SpeechBubble.transform.localPosition = Vector3.up * transform.localScale.y * 0.5f;
        SpeechBubble.transform.localScale = new Vector3(
            0.01f / transform.localScale.x,
            0.01f / transform.localScale.y,
            1f
        );

        Canvas bubbleCanvas = SpeechBubble.AddComponent<Canvas>();
        RectTransform bubbleTransform = SpeechBubble.GetComponent<RectTransform>();
        bubbleTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        bubbleTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

        SpeechBubbleCanvasGroup = SpeechBubble.AddComponent<CanvasGroup>();

        AddSpeechCorner(0f, 0f, "speechbubblese");
        AddSpeechCorner(0f, 1f, "speechbubblese");
        AddSpeechCorner(1f, 0f, "speechbubblese");
        AddSpeechCorner(1f, 1f, "speechbubblese");

        GameObject textObj = new GameObject("Text");
        textObj.transform.parent = SpeechBubble.transform;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        Text text = textObj.AddComponent<Text>();
        text.font = FezManager.Instance.SpeechFont;
        text.fontSize = 50;
        text.alignment = TextAnchor.MiddleCenter;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = "";

        RectTransform textTransform = textObj.GetComponent<RectTransform>();
        textTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        textTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 243f);

        CurrentTextOpacity = 0f;
    }

    protected void AddSpeechCorner(float x, float y, string imgName) {
        Texture2D tex = FezManager.Instance.GetTexture2D("other textures/speech_bubble/" + imgName);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        GameObject imgObj = new GameObject(imgName);
        imgObj.transform.parent = SpeechBubble.transform;
        imgObj.transform.localPosition = Vector3.zero;
        imgObj.transform.localScale = Vector3.one;

        Image img = imgObj.AddComponent<Image>();
        // Cache sprites?
        img.sprite = Sprite.Create(
            tex,
            new Rect((1f - x) * tex.width, y * tex.height, (2f * (x - 0.5f)) * tex.width, (2f * (y - 0.5f)) * -tex.height),
            new Vector2(tex.width * 0.5f, tex.height * 0.5f),
            16f
        );
        img.sprite.name = imgName;

        RectTransform imgTransform = imgObj.GetComponent<RectTransform>();
        imgTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 20f);
        imgTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20f);
        imgTransform.pivot = new Vector2(1f - x, y);
    }

    public void Update() {
        Vector3 ssPositionOld = Camera.main.WorldToScreenPoint(transform.position);

        if (CurrentAction == NpcAction.Idle ||
            CurrentAction == NpcAction.Idle3 ||
            CurrentAction == NpcAction.Walk) {
            TimeSinceActionChange += Time.deltaTime;
            if (TimeUntilActionChange <= TimeSinceActionChange) {
                ToggleAction();
            }
        } else if (!CurrentTiming.Loop && CurrentTiming.Ended && CurrentAction != NpcAction.Hide) {
            ToggleAction();
        }

        bool shouldStopTalking = ShouldStopTalking != null && ShouldStopTalking(this);
        if (shouldStopTalking)
            CurrentTextLine = -1;

        if (CurrentAction != NpcAction.Talk) {
            if (ShouldStartTalking != null && CanTalk && (NPC.Speech.Count > 0 || NPC.CustomSpeechLine != null) && CurrentTextLine == -1 && ShouldStartTalking(this)) {
                CurrentAction = NpcAction.Talk;
                UpdateText(0);
            }
            if (CurrentAction == NpcAction.Walk) {
                Walk();
            }
        } else if ((shouldStopTalking || CurrentTextLine < 0 || CurrentTextLine >= NPC.Speech.Count) && CurrentAction != NpcAction.TakeOff) {
            StartCoroutine(FadeTextOut());
            ToggleAction();
        } else {
            Talking(this);
        }

        transform.LookAt(transform.position + (transform.position - Camera.main.transform.position));
        Vector3 ssPosition = Camera.main.WorldToScreenPoint(transform.position);
        bool lookingRight;
        if (FezHelper.AlmostEqual(ssPositionOld.x, ssPosition.x)) {
            lookingRight = LookingRight;
        } else {
            lookingRight = ssPositionOld.x < ssPosition.x;
        }
        if ((!lookingRight && 0f < meshRenderer.sharedMaterial.mainTextureScale.x) ||
            (lookingRight && meshRenderer.sharedMaterial.mainTextureScale.x < 0f)) {
            CurrentAnimation.FlipH = !lookingRight;
        }
    }

    private void Walk() {
        WalkStep += ((LookingRight ? 1f : -1f) / (WalkedDistance == 0f ? 1f : WalkedDistance) * Time.deltaTime) * NPC.WalkSpeed / (NPC.DestinationOffset.magnitude * 0.5f);
        if (WalkStep < 0f || 1f < WalkStep) {
            WalkStep = Mathf.Clamp01(WalkStep);
            ToggleAction();
            return;
        }

        WalkStep = Mathf.Clamp01(WalkStep);
        gameObject.FezZ();
        transform.localPosition = Vector3.Lerp(
            NPC.Position,
            NPC.Position + NPC.DestinationOffset,
            WalkStep) + new Vector3(0f, transform.localScale.y / 2f, 0f);
        gameObject.FezZ();
    }

    // Talking needs to be overriden by HoloFEZ
    public Func<FezUnityNpcInstance, bool> ShouldStartTalking = DefaultShouldStartTalking;
    public Action<FezUnityNpcInstance> Talking = DefaultTalking;
    public Func<FezUnityNpcInstance, bool> ShouldStopTalking = DefaultShouldStopTalking;

    private static bool _ShouldStartTalking(FezUnityNpcInstance self) {
        return false;
    }
    private static void _Talking(FezUnityNpcInstance self) {
    }
    private static bool _ShouldStopTalking(FezUnityNpcInstance self) {
        return false;
    }

    private Coroutine _CurrentTextUpdater;
    public void UpdateText(int line = -1) {
        if (line == -1)
            line = ++CurrentTextLine;
        CurrentTextLine = line;
        if (CurrentTextLine < 0 || CurrentTextLine >= NPC.Speech.Count)
            return;

        string text = FezText.Game[NPC.Speech[line].Text];
        if (_CurrentTextUpdater != null)
            StopCoroutine(_CurrentTextUpdater);
        _CurrentTextUpdater = StartCoroutine(UpdateText(text));
    }

    private IEnumerator UpdateText(string text) {
        yield return FadeTextOut();
        transform.GetComponentInChildren<Text>().text = text;
        yield return FadeTextIn();
    }

    private IEnumerator FadeTextOut() {
        return FadeText(0f);
    }

    private IEnumerator FadeTextIn() {
        return FadeText(1f);
    }

    private IEnumerator FadeText(float to) {
        float from = CurrentTextOpacity;

        for (float f = 0f; f <= 0.1f; f += Time.deltaTime) {
            CurrentTextOpacity = Mathf.Lerp(from, to, f / 0.1f);
            yield return null;
        }

        CurrentTextOpacity = to;
    }

    private void ToggleAction() {
        NpcAction prevAction = CurrentAction;
        RandomizeAction();
        TimeUntilActionChange = UnityEngine.Random.Range(2f, 5f);
        TimeSinceActionChange = 0f;
    }

    private void RandomizeAction() {
        switch (CurrentAction) {
            case NpcAction.Turn:
                Turn();
                break;
            case NpcAction.Burrow:
                Destroy(gameObject);
                break;
            case NpcAction.TakeOff:
                CurrentAction = NpcAction.Fly;
                break;
            case NpcAction.Land:
                CurrentAction = NpcAction.Idle;
                break;
            default:
                if ((UnityEngine.Random.value <= 0.5f || !CanWalk) && CanIdle) {
                    if (CanWalk || UnityEngine.Random.value <= 0.5f) {
                        ChooseIdle();
                        break;
                    } else if (CanTurn) {
                        CurrentAction = NpcAction.Turn;
                        break;
                    }
                    Turn();
                    break;
                }

                if (!CanWalk) {
                    CurrentAction = CanIdle ? NpcAction.Idle : NpcAction.None;
                    return;
                }

                if (WalkStep == 0f || WalkStep == 1f) {
                    if (CanIdle && UnityEngine.Random.value <= 0.5f) {
                        ChooseIdle();
                        break;
                    } else if (CanTurn) {
                        CurrentAction = NpcAction.Turn;
                        break;
                    } else {
                        Turn();
                        break;
                    }
                } else if (CanTurn && UnityEngine.Random.value <= 0.5f) {
                    CurrentAction = NpcAction.Turn;
                    break;
                }

                CurrentAction = NpcAction.Walk;
                break;
        }
    }

    private void ChooseIdle() {
        if (CurrentAction == NpcAction.Idle2 || CurrentAction == NpcAction.Idle3) {
            CurrentAction = NpcAction.Idle;
            return;
        }
        float random = UnityEngine.Random.value;
        float special = 1f + (CanIdle2 ? 1f : 0f) + (CanIdle3 ? 1f : 0f);

        if (random < 1f / special) {
            CurrentAction = NpcAction.Idle;
            return;
        }
        if (special > 1.0 && random < 2f / special) {
            CurrentAction = CanIdle2 ? NpcAction.Idle2 : NpcAction.Idle3;
            return;
        }
        if (special <= 2f || random >= 3f / special) {
            return;
        }
        CurrentAction = NpcAction.Idle3;
    }

    private void Turn() {
        LookingRight = !LookingRight;
        CurrentAction = CanWalk ? NpcAction.Walk : NpcAction.Idle;
    }

}
