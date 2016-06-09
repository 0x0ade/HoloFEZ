# HoloFEZ - FEZ in (Steam)VR POC
### License: MIT, excluding Unity plugins
----
### Dependencies:
* SteamVR Unity plugin (blame this for lack of non-Windows support)
* FEZ (preferably via Steam)
----
### Usage instructions:
1. Run HoloFEZ
2. Set FEZ content folder (auto-detected if Steam running)
3. ..?
----
### Default Controls (both non-VR and VR):
Movement: WASD / Left Stick  
Select: LMB / Shift / A
Back to level select: RMB / Ctrl / B
----
#### TOOD:
* Sound (ambient, music)
* Better background handling (some left untested)
* Optimize level loading (according to profiler, currently trashing GC)
* Implement HW instancing as soon as available & stable in Unity
* NPCs
* Level scripts
----
#### Disclaimer: Only tested on Google Cardboard via Riftcat. Chaperone currently not supported.