# Spell Animation Workshop

Open `Scenes/SpellAnimationWorkshop.unity` and enter Play mode. Keys 1-6 play the six test spells; 0 or Escape cancels. The layer toggle compares full-body release with the upper-body release layer.

## Moving instant spells

- Hold WASD for eight directions relative to the character's initial forward direction. Release the keys to stand. The camera observes the character from the front, so character-right appears on screen-left.
- The nine direction buttons select continuous movement or standing. Space stops movement; R stops and resets position. F1 hides or shows both control panels.
- Press 1 (Directed) or 2 (Omni) while standing or moving. Moving releases automatically use the UpperBody layer while the legs keep running or walking backward. Starting, stopping, or changing direction during a release keeps the upper-body action playing.
- LookAt keeps the head looking along the initial forward direction during side/diagonal movement. The LOOK AT button enables comparison without it.
- Travel switches between actual displacement with camera follow and in-place animation preview. Actual displacement is limited to the preview floor (seven units from the starting position on each axis); use R to recenter.
- Cast-time and channeled tests remain stationary. Starting one stops movement; starting movement interrupts it.

The workshop uses the existing forward/backward clips with character yaw for side and diagonal travel, matching the player locomotion approach. These are not separate authored strafe clips.

## Upper-body stabilization

The character's existing `CharacterLookAtIk` component owns both upper-body stabilization and LookAt. Select FORWARD and press 2 to compare the Omni release, then press T or click UPPER BODY to switch stabilization on/off independently of LOOK AT. REPEAT INSTANT loops the last selected instant spell (1 or 2) with a short pause. Stop/0/Escape cancels the loop; cast-time and channel tests also leave repeat mode. TRAVEL / IN PLACE is useful for watching multiple cycles.

The component samples the release clip at the Animator state's current phase on a transform-only reference skeleton using the same Humanoid Avatar. It reconstructs the reference UpperChest rotation in the supplied spell-facing frame and distributes the correction over Spine, Chest, and UpperChest after animation evaluation. Each joint's axial twist is limited relative to the Avatar's rest pose (45 degrees by default), with a rotation speed limit to smooth changes near opposite-facing poses. Pelvis/leg transforms and local bone offsets are preserved. Rotating the spine naturally changes the upper body's position; running's vertical motion remains.

When a joint reaches its limit, the torso may deviate from the reference pose: the UI shows LIMIT and the remaining pose offset. After the torso solve, the head and upper arms blend directly toward their independently sampled reference rotations. The head correction is shared over Neck/Head. No head/arm target is derived from the already blended chest: that residual-based approach caused Directed's exit blend to wind the head through a full turn. This preserves directions, not exact hand positions; it is not a hand-target IK solver. LookAt fades out while stabilization takes over and returns as stabilization fades out. Its head correction measures the remaining yaw from the current head orientation after the body solve, rather than adding the character-root yaw again. Joint limits are configurable preview defaults, not universal anatomical limits.

`CharacterLookAtIk.UpperBody.cs` is another part of the same component, not a separate behaviour. Its single LateUpdate calls `Evaluate`: first the upper-body pose solve, then LookAt using the remaining weight. There is no cross-component pose-weight setter or dependency on the execution order of two scripts.

Inspector controls under Upper Body Pose include Enable Upper Body Stabilization, Upper Body Weight, blend durations, anchor bone, joint twist/speed limits, and head/arm orientation preservation. UpperChest falls back to Chest when the Avatar lacks it. A reference skeleton is cached per component and released on disable/destruction. Outside the workshop, call `Initialize`, `PlayUpperBodyPose` with the clip/state/layer and world-space spell-facing rotation, `SetUpperBodyReferenceRotation` when facing changes, and `StopUpperBodyPose` when interrupted. Toggle `m_EnableUpperBodyStabilization` for an in-progress A/B comparison; the existing `m_Enable` controls LookAt independently. Disabling the entire component releases the reference and requires another `PlayUpperBodyPose` call. `Evaluate` is also available for manual animation evaluation; do not call it again after LateUpdate for the same pose.

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
- **CR > Workshop > Validate Moving Instant Spells (Play Mode)** runs all 18 direction/instant combinations, displacement, IK target direction, cancellation, changes during release, and automatic completion in the running workshop. The report is written to `Temp/MovingInstantValidation.txt`. Leave movement keys released during the check.
- **CR > Workshop > Validate Character IK (Play Mode)** checks 72 poses across both releases and nine directions through the unified evaluation, including a rotated facing frame and active LookAt. It checks reference alignment when unconstrained, per-joint twist limits, independently sampled head/arm directions, local bone offsets, lower-body transforms, toggling, and blending. Four continuous left/right sequences check spine rotation jumps at 60 Hz. Another 16 sequences cover three complete casts each, both sides and spells, LookAt on/off, and 30/60 Hz. These measure unwrapped head heading and quaternion steps across entry, exit, and repeated casts to catch full-turn winding that mid-cast snapshots miss. Results go to `Temp/CharacterIkValidation.txt`.
- **CR > Workshop > Validate Spell Foot Stability** samples both feet and toes at 120 Hz, then checks both 0.12-second cast-to-release blends in the actual AnimatorController. It fails above 1.5 mm of axis-aligned drift and writes `Reports/FootStability.json`.
- **CR > Workshop > Validate Character Animations** checks all 142 Unity clips and materials.

The corrected six clips and two cast-to-release blends measured a maximum foot/toe axis span of 0.452 mm. This is a trajectory bound, not a perceptual score. Blender subframe shoe tests and all-bone FBX round trips are recorded separately in the revision folder. The idle-to-spell stance change and locomotion are intentional and are outside the stationary-foot assertion.

