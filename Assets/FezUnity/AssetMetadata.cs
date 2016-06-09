//Taken from FEZMod

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class AssetMetadata {
	
	public static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();
	
	public string File;
	public long Offset;
	public int Length;
	
	public AssetMetadata() {
	}
	
	public AssetMetadata(string file, long offset, int length)
		: this() {
		File = file;
		Offset = offset;
		Length = length;
	}
	
}
