using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;

public interface IReticleListener {

    void OnGazeEnter();
    void OnGazeExit();
    void OnGazeTrigger();

}
