//Taken from FEZMod.Installer
using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;

public static class FezFinder {
	
	public static string GetSteamPath() {
		Process[] processes = Process.GetProcesses(".");
		string path = null;
		
		for (int i = 0; i < processes.Length; i++) {
			Process p = processes[i];
			
			try {
				if (!p.ProcessName.Contains("steam") || path != null) {
					p.Dispose();
					continue;
				}
				
				if (p.MainModule.ModuleName.ToLower().Contains("steam")) {
					path = p.MainModule.FileName;
					Console.WriteLine("Steam found at " + path);
					p.Dispose();
				}
			} catch (Exception) {
				//probably the service acting up or a process quitting
				p.Dispose();
			}
		}
		
		//string os = Environment.OSVersion.Platform.ToString().ToLower();
		//https://github.com/mono/mono/blob/master/mcs/class/corlib/System/Environment.cs
		//if MacOSX, OSVersion.Platform returns Unix.
		string os = GetPlatform().ToString().ToLower();
		
		if (path == null) {
			Console.WriteLine("Found no Steam executable");
			
			if (os.Contains("lin") || os.Contains("unix")) {
				path = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/share/Steam");
				if (!Directory.Exists(path)) {
					return null;
				} else {
					Console.WriteLine("At least Steam seems to be installed somewhere reasonable...");
					path = Path.Combine(path, "distributionX_Y/steam");
				}
			} else {
				return null;
			}
		}
		
		
		if (os.Contains("win")) {
			//I think we're running in Windows right now...
			path = Directory.GetParent(path).Parent.FullName; //PF/Steam[/bin/steam.exe]
			Console.WriteLine("Windows Steam main dir " + path);
			
		} else if (os.Contains("mac") || os.Contains("osx")) {
			//Guyse, we need a test case here!
			return null;
			
		} else if (os.Contains("lin") || os.Contains("unix")) {
			//Are you sure you want to forcibly remove everything from your home directory?
			path = Directory.GetParent(path).Parent.FullName; //~/.local/share/Steam[/ubuntuX_Y/steam]
			Console.WriteLine("Linux Steam main dir " + path);
			
		} else {
			Console.WriteLine("Unknown platform: " + os);
			return null;
		}
		
		//PF/Steam/SteamApps //~/.local/share/Steam/SteamApps
		if (Directory.Exists(Path.Combine(path, "SteamApps"))) {
			path = Path.Combine(path, "SteamApps");
		} else {
			path = Path.Combine(path, "steamapps");
		}
		path = Path.Combine(path, "common"); //SA/common
		
		path = Path.Combine(path, "FEZ");
		path = Path.Combine(path, "FEZ.exe");
		
		if (!File.Exists(path)) {
			Console.WriteLine("FEZ not found at " + path + " (at least Steam found)");
			return null;
		}
		
		Console.WriteLine("FEZ found at " + path);
		
		return path;
	}
	
	public static string FindFEZ() {
		string path;
		
		if ((path = FezFinder.GetSteamPath()) != null) {
			return path;
		} else if (false) {
			//TODO check other paths
			//How does GOG handle FEZ?
		} else {
			//Nothing found
		}
		
		return null;
	}
	
	public static PlatformID GetPlatform() {
		//for mono, get from
		//static extern PlatformID Platform
		PropertyInfo property_platform = typeof(Environment).GetProperty("Platform", BindingFlags.NonPublic | BindingFlags.Static);
		if (property_platform != null) {
			return (PlatformID) property_platform.GetValue(null, null);
		} else {

			//for .net, use default value
			return Environment.OSVersion.Platform;
		}
	}
	
}
	