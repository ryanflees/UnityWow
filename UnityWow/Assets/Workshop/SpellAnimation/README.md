# Spell Animation Workshop

Open `Scenes/SpellAnimationWorkshop.unity` and enter Play mode. Keys 1-6 play the six test spells; 0 or Escape cancels. The layer toggle compares full-body release with the upper-body release layer.

| Test | Casting / channel clip | Release clip |
| --- | --- | --- |
| Instant Directed / Cast Directed | ReadySpellDirected | SpellCastDirected |
| Instant Omni / Cast Omni | ReadySpellOmni | SpellCastOmni |
| Channel Directed | ChannelCastDirected | None |
| Channel Omni | ChannelCastOmni | None |

All clips come from `Assets/Art/Characters/WowGirl/Animations` and use the WowGirl Humanoid Avatar. Keep Foot IK enabled on the six Base Layer states. The character browser also enables Foot IK by default and provides a toggle for comparison.

## Foot stability correction (2026-09-23)

These six stationary spells originally used rotation-only retargeting. Measured foot drift reached about 22 mm, and the Blender shoe soles penetrated the floor by up to 6.7 mm. Enabling Unity Foot IK alone did not remove that motion.

The Blender correction pins both support feet to shared spell-stance anchors, preserves their stance yaw, levels the soles, and solves the upper/lower legs while retaining the original knee bend direction. Only the eight leg/foot/toe rotation channels change. Hips, upper body, hands, action durations, rig, skinning, and the other 134 Blender actions remain unchanged. The action timeline stays at 30 FPS; the corrected leg channels and FBX export use half-frame samples (60 samples/second) to limit interpolation drift.

Source: `References/WowGirl/Blender/WowGirl_AllHumanFemale.blend` at repository root. The revision, rollback files, scripts, before/after captures, and Blender/FBX reports are in `References/WowGirl/Revisions/Spells/20260923-FootStability`. `References` is excluded from Git by the repository's existing ignore rule; preserve these local authoring files separately.

The local FBX exporters honor each action's `export_sample_step` property; these six actions require `0.5`. Re-export the saved actions without rebuilding the retargeted library. Preserve the existing Unity `.meta` files and clip names.

## Validation

- **CR > Workshop > Validate Spell Animations** checks casting, channeling, release layers, movement during release, and cancellation.
- **CR > Workshop > Validate Spell Foot Stability** samples both feet and toes at 120 Hz, then checks both 0.12-second cast-to-release blends in the actual AnimatorController. It fails above 1.5 mm of axis-aligned drift and writes `Reports/FootStability.json`.
- **CR > Workshop > Validate Character Animations** checks all 142 Unity clips and materials.

The corrected six clips and two cast-to-release blends measured a maximum foot/toe axis span of 0.452 mm. This is a trajectory bound, not a perceptual score. Blender subframe shoe tests and all-bone FBX round trips are recorded separately in the revision folder. The idle-to-spell stance change and locomotion are intentional and are outside the stationary-foot assertion.
