using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Tools;
using FmbLib;
using System.IO;

public class FezUnityNpcInstance : MonoBehaviour, IFillable<NpcInstance> {

    [HideInInspector]
    public NpcInstance NPC;

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

        BinaryReader metadataReader = FezManager.Instance.ReadFromPack("character animations/" + NPC.Name + "/metadata");
        if (metadataReader != null) {
            npc.FillMetadata(FmbUtil.ReadObject(metadataReader) as NpcMetadata);
        }

        CanIdle = Animations.ContainsKey(NpcAction.Idle);
        CanIdle2 = Animations.ContainsKey(NpcAction.Idle2);
        CanIdle3 = Animations.ContainsKey(NpcAction.Idle3);
        CanWalk = Animations.ContainsKey(NpcAction.Walk);
        CanTalk = Animations.ContainsKey(NpcAction.Talk);
        CanTurn = Animations.ContainsKey(NpcAction.Turn);

        CurrentAction = CanIdle ? NpcAction.Idle : NpcAction.Walk;
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

        if (CurrentAction != NpcAction.Talk) {
            if (CanTalk && (NPC.Speech.Count > 0 || NPC.CustomSpeechLine != null)) {
                Talk();
            }
            if (CurrentAction == NpcAction.Walk) {
                Walk();
            }
        } else if (StopTalking() && CurrentAction != NpcAction.TakeOff) {
            ToggleAction();
        }

        Vector3 ssPosition = Camera.main.WorldToScreenPoint(transform.position);
        bool lookingRight;
        if (FezHelper.AlmostEqual(ssPositionOld.x, ssPosition.x)) {
            lookingRight = LookingRight;
        } else {
            lookingRight = ssPositionOld.x < ssPosition.x;
        }
        if (lookingRight) {
            transform.LookAt(transform.position + (transform.position - Camera.main.transform.position));
        } else {
            transform.LookAt(Camera.main.transform.position);
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

    private void Talk() {
        // TODO
    }

    private bool StopTalking() {
        // TODO
        return false;
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
