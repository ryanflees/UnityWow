# Character Animation Workshop

This subdirectory tests model animation. Other Workshop experiments should use their own sibling directories.

The interface uses ordinary uGUI Text and InputField components. Do not introduce TextMeshPro unless explicitly needed later.

Open `Scenes/CharacterAnimationWorkshop.unity`, then press Play.

- Browse all 140 HumanFemale clips or search by name.
- The default All category lists the core 14 actions first in their established order, followed by the other 126 actions alphabetically. Category filtering also offers Core 14, Combat, Spells, Movement, Emotes and Other.
- Select a clip to restart it. Pause, Restart and Loop control playback.
- Drag the timeline to pause and inspect an exact pose; change the speed for slow motion.
- View buttons orbit the character; Zoom changes viewing distance.
- Hold the left mouse button in the character area and drag to orbit horizontally or vertically. UI interactions do not start camera dragging. Reset restores the camera.

`HandsClosed` and `Stop` are static poses in the source, so their exported clips are intentionally short. Some source fingers are grouped; inspect grips before using a weapon. The imported set contains skeletal motion, without weapons or spell effects.

The original WowLike character assets are under `Assets/Art/Characters/WowGirl`. The new 140-clip set is under its `HumanFemale` subdirectory. The test uses Generic animation and preserved FBX hierarchy, without another humanoid retargeting pass.

Editor commands:

- **CR > Workshop > Build Character Animation Scene** rebuilds this generated scene and configures imports. An unsaved active scene is backed up before replacement.
- **CR > Workshop > Validate Character Animations** checks all 140 clips, transform binding paths, sampled poses and materials.

The complete Blender project, FBX exports, texture files and source mapping are outside Assets at `References/WowGirl` in the repository root.

Keep screenshots outside Assets, under `References/WowGirl/Preview`, to avoid importing test captures as Unity assets. Future action corrections follow `References/WowGirl/AnimationRepairWorkflow.md`.
