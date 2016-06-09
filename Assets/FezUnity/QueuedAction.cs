using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;

public class QueuedAction {

    public List<QueuedAction> Queue;
    public Action Action;

	public QueuedAction(Action action, List<QueuedAction> queue = null) {
        Action = action;
        Queue = queue;

        if (Queue != null) {
            Queue.Add(this);
        }
	}

    public void Invoke() {
        Action();
        if (Queue != null) {
            Queue.Remove(this);
        }
    }
	
}
