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

### Advanced instructions:
* Hold SHIFT when starting HoloFEZ to change the resolution / controls.
* HoloFEZ works best in VR on some systems with a resolution of 1024 x 768 (default).

----

### Default Controls (both non-VR and VR):

Movement: WASD / Left Stick  

Select: LMB / Shift / A

Back to level select: RMB / Ctrl / B

Change time: Q / E / LT / RT

Move straight up & down: R / F / LB / RB

Recalibrate orientation: Y (keyboard or gamepad)

----

#### TODO:
* Sound (ambient, music)
* Optimize level loading (according to profiler, currently trashing GC)
* Implement HW instancing as soon as available & stable in Unity
* NPCs
* Level scripts

----

#### Disclaimer: Tested on Oculus Rift DK2 (Thanks, Renaud!) and Google Cardboard via Riftcat. Chaperone currently not supported.
