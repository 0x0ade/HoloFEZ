using UnityEngine;
using System.Collections;
using System.IO;
using FmbLib;

public class LzxDecompressor : ILzxDecompressor {
	
	public static void Init() {
		FmbUtil.Setup.CreateLzxDecompressor = gen;
	}
	private static ILzxDecompressor gen(int window) {
		return new LzxDecompressor(window);
	}
	
	protected LzxDecoder lzx;
	public LzxDecompressor()
		: this(16) {
	}
	
	public LzxDecompressor(int window) {
		lzx = new LzxDecoder(window);
	}

	public int Decompress(Stream inData, int inLen, Stream outData, int outLen) {
		return lzx.Decompress(inData, inLen, outData, outLen);
	}
	
}
