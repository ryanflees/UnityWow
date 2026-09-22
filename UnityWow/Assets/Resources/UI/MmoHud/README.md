# Minimal MMO HUD

This is a visual HUD foundation, not a combat or targeting implementation.

- Art: Assets/Art/UI/MinimalMmo/Textures and Sprites.
- Prefabs: Assets/UI/MmoHud/Prefabs (MmoHud, ActionBar, AbilitySlot, PlayerFrame, TargetFrame).
- Preview scene: Assets/Workshop/Playground/PlaygroundKCC.unity.
- Bottom-center action bar contains twelve empty slots with editable key labels.
- Player and target frames use circular portrait placeholders, thin health/resource strips and editable labels. Values are visual examples, not bound to gameplay.
- Uses standard UnityEngine.UI.Text and LegacyRuntime.ttf. No TextMeshPro.
- Canvas scales with a 1920 x 1080 reference resolution. Frames are anchored to upper corners; action bar is bottom-center.
- Decorative graphics do not receive raycasts and do not block camera controls.
- Replace children of PortraitContent to add portrait content; the existing circular Mask clips the result.
- Original generated PNGs retain alpha. Native Sprite assets trim transparent padding without changing the source images. ActionBar supports sliced rendering.
- Generated using the built-in imagegen tool. The earlier ornamental direction was rejected and is not included in Assets.

Screenshots and generation records are stored outside Assets in References/UI/MinimalMmo.


Design revision: ability slots and action-bar background now use flat 3x3 native textures with a one-pixel sliced border. All corners are square. No gradient, surface texture, bevel, ornament or metallic detail. The earlier generated slot/bar PNGs were removed. Portrait outlines remain simple circles.


Border rendering fix: skill slots now use two solid uGUI rectangles, an outer border and an inner fill inset by 2 reference pixels on all four sides. No texture sampling is used for slot edges. Canvas pixel-perfect rendering is enabled.

