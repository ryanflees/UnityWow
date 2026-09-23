# Character Animation Workshop

This subdirectory tests model animation. Other Workshop experiments should use their own sibling directories.

The interface uses ordinary uGUI Text and InputField components. Do not introduce TextMeshPro unless explicitly needed later.

Open `Scenes/CharacterAnimationWorkshop.unity`, then press Play.

- Browse the current WowGirl action library or search by name. The list currently contains 140 actions from 142 shared animation assets: TurnLeft/TurnRight are compatibility copies of ShuffleLeft/ShuffleRight and are hidden when their FBX contents match. The controller keeps its existing compatibility references.
- The default All category groups actions in this order: **Movement, Spells, Melee, Ranged, Death, Swimming, Emotes, Other**. Category tags appear beside action names. Movement starts with the established core actions (idle, walk, run, jump and related transitions); remaining actions sort by name within their category. The Category button cycles through All and each group in the same order.
- Classification follows action names. Bow/Rifle/Thrown attacks and weapon readiness belong to Ranged; spell readiness/releases/channels belong to Spells; Death/Drown/Drowned belong to Death; Swim actions have their own group. Fishing casts remain in Other.
- Select a clip to restart it. Pause, Restart and Loop control playback.
- Drag the timeline to pause and inspect an exact pose; change the speed for slow motion.
- Foot IK is enabled by default for Humanoid clips, matching the spell controller. Toggle **Foot IK: ON/OFF** to compare imported poses with corrected foot placement; the setting is retained while switching clips.
- View buttons orbit the character; Zoom changes viewing distance.
- Hold the left mouse button in the character area and drag to orbit horizontally or vertically. UI interactions do not start camera dragging. Reset restores the camera.

`HandsClosed` and `Stop` are static poses in the source, so their exported clips are intentionally short. Some source fingers are grouped; inspect grips before using a weapon. The imported set contains skeletal motion, without weapons or spell effects.

The shared model and Humanoid Avatar are under `Assets/Art/Characters/WowGirl/Model`, and all clips are under `Assets/Art/Characters/WowGirl/Animations`. The old `HumanFemale` asset copy has been removed. The browser and spell scene use the same imported animations, including the corrected spell feet.

Editor commands:

- **CR > Workshop > Refresh Character Animation Clips** updates only the saved scene's action list after assets are added, removed or consolidated. It preserves the character, cameras, scene layout and import settings.
- **CR > Workshop > Build Character Animation Scene** rebuilds this generated scene from the existing shared model and clips. An unsaved active scene is backed up before replacement. It preserves existing animation import settings.
- **CR > Workshop > Validate Character Animations** checks current assets, sampled poses, materials and the saved scene's exact clip references; stale scene lists fail validation. Clip counts are discovered from the assets instead of hard-coded.

The complete Blender project, FBX exports, texture files and source mapping are outside Assets at `References/WowGirl` in the repository root.

Keep screenshots outside Assets, under `References/WowGirl/Preview`, to avoid importing test captures as Unity assets. Future action corrections follow `References/WowGirl/AnimationRepairWorkflow.md`.
