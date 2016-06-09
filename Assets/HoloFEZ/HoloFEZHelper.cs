using UnityEngine;
using System;
using System.Collections.Generic;
using FmbLib;
using FezEngine;
using FezEngine.Structure;

/*
 * HoloFEZ helper - stuff that doesn't fit into FezHelper
 */
public static class HoloFEZHelper {

    public static Mesh Invert(this Mesh mesh) {
        int n0, n1, n2;
        for (int i = 0; i < mesh.triangles.Length; i += 3) {
            n0 = mesh.triangles[i + 0];
            n1 = mesh.triangles[i + 1];
            n2 = mesh.triangles[i + 2];
            mesh.triangles[i + 0] = n2;
            mesh.triangles[i + 1] = n1;
            mesh.triangles[i + 2] = n0;
        }
        return mesh;
    }

    public static float TimeSpeed = 1f / 120f;
    public static float ToSpeedF(this float time) {
        return time / TimeSpeed;
    }
    public static float SpeedF {
        get {
            return Time.deltaTime.ToSpeedF();
        }
    }

}
