//Taken from FEZMod
using System;
using System.Collections.Generic;
using FmbLib;
using System.IO;

public class TextType {
    public string Name;
    public Dictionary<string, Dictionary<string, string>> Map;
    public Dictionary<string, string> Fallback;

    public string this[string key] {
        get {
            return Get(key);
        }
    }

    public TextType()
        : this("UNKNOWN") {
    }

    public TextType(string name) {
        Name = name;
    }

    public void Load() {
        try {
            using (BinaryReader reader = FezManager.Instance.ReadFromPack("Texts/" + Name) ?? FezManager.Instance.ReadFromPack("Resources/" + Name + "text")) {
                Map = FmbUtil.ReadObject(reader) as Dictionary<string, Dictionary<string, string>>;
            }
            Fallback = Map[string.Empty];
        } catch (Exception e) {
            UnityEngine.Debug.Log(e);
            //loading failed - fall back to empty maps
            Map = new Dictionary<string, Dictionary<string, string>>();
            Fallback = Map[string.Empty] = new Dictionary<string, string>();
        }
    }

    public string Get(string tag) {
        if (tag == null) {
            return "[error:nulltag]";
        }

        Dictionary<string, string> map;
        if (!Map.TryGetValue(System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out map)) {
            map = Fallback;
        }

        string str;
        if ((!map.TryGetValue(tag, out str)) && (!Fallback.TryGetValue(tag, out str))) {
            return "[" + Name + ":" + tag + "]";
        }
        return str;
    }
}

public static class FezText {

    public readonly static TextType Game = new TextType("game");
    public readonly static TextType Static = new TextType("static");

    static FezText() {
        Game.Load();
        Static.Load();
    }

}
