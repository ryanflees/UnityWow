# Player instant Omni test

Open `PlaygroundKCC.unity` and enter Play Mode. Focus the Game view.

- Press **1** or **4**, or click either bound action bar icon, to play **Instant Omni**. Both slots use the same spell definition.
- Hold WASD while casting. Legs continue locomotion while the upper body plays the Omni release.
- Stop during the release to blend back into the full-body action at the current animation phase.
- Holding either key does not repeat. Both slots share a **1.5-second global cooldown**. Presses during GCD do not restart the animation and are not queued. Press again after it expires. Simultaneous 1/4 presses are coalesced into one release.
- Both icons show the same radial cooldown and remaining time. The test requires no target and has no damage, effects, audio, resource cost or individual spell cooldown.
- Existing movement, camera and R-to-respawn controls remain available.

The definition is `Data/InstantOmniTest.asset`, id **10001**, in `Data/PlaygroundSpellCollection.asset`. It uses the existing `Spell_Arcane_Arcane01_0` Sprite. The scene's `DemoPlayerSpawner` assigns the spell collection and slots 1/4 to the spawned player, then binds the existing HUD presenter. It does not change the shared player or character prefab.

Both keyboard and UI use `PlayerController.TryActivateActionSlot` → `SpellController.TryCast` → `SpellPresentation.Play` → `Character`. The player's reference facing direction also drives the upper-body pose reference during movement and camera turning. UI pointer presses are excluded from camera mouse capture. Empty slots do not cast anything.

The runtime controller currently accepts only free, targetless instant animation spells with optional GCD. Unsupported targeting, costs, individual/shared-group cooldowns, effects and other cast types return a failure result; they are not silently ignored. `SpellCastInstance` remains reserved for future timed casts. This test intentionally exercises the real player/input/UI route without introducing combat simulation.

## Global cooldown configuration

`SpellCooldownData.m_GlobalCooldown` defaults to 1.5 seconds. `SpellController` owns one `GlobalCooldownState` per actor; slots and spell definitions contain no active timers. The timer uses scaled game time, independently from animation, and starts only after a successful cast. It is committed before release callbacks. Cancelling presentation, respawning the same controller, or disabling/re-enabling it does not clear GCD. A rejected cast neither restarts animation nor changes GCD.

`ActorAttributes.m_HastePercent` reduces the next duration using **base GCD / (1 + haste percent / 100)**. Zero haste gives 1.5 seconds, 50 gives 1 second, and 100 gives 0.75 seconds. The duration is captured when cast; changing haste does not rescale an already running GCD. No gameplay minimum duration is imposed yet. The spawned player has this component, which can be adjusted in Play Mode.

| Triggers Global Cooldown | Is Affected By Global Cooldown | Behavior |
| --- | --- | --- |
| On | On | Normal skill: waits for GCD, then starts GCD. |
| Off | On | Waits for an existing GCD but does not start a new one. |
| Off | Off | Off-GCD skill: can be used during GCD and leaves the timer unchanged. |
| On | Off | Can be used during GCD and starts/extends it; never shortens the active timer. |

For the current Omni test both switches are enabled, so 1 and 4 cannot bypass one another. The same controller policy applies to future affected skills, not just duplicate bindings.

## Pose style tuning

The `CharacterLookAtIk` component is saved on `Assets/Art/Characters/WowGirl/WowGirl.prefab`. Select that prefab to edit persistent defaults, or select `WowGirl Player/.../WowGirl` during Play Mode to tune the live character. Its grouped inspector provides **Save Tuning to Prefab** for saving the current pose/LookAt values without copying runtime bone, player or target references. Playback keeps running while tuning; the anchor bone refreshes on the next spell. Playground no longer overwrites the prefab's LookAt style values on spawn.

| Inspector setting | Initial value | Style effect |
| --- | --- | --- |
| Stabilization Weight | 1 | Overall correction strength; lower values retain more locomotion. |
| Facing Lock | 1 | 1 maintains spell facing; 0 lets the reference frame follow the character model. Lower this to reduce strafe counter-twist. |
| Spine / Chest / Upper Chest Share | 1 / 1 / 1 | Relative distribution of torso correction. Zero excludes that local joint. All zero disables torso correction, while head/arm controls remain independent. |
| Joint Twist Limit | 45 degrees | Per-joint axial twist from Avatar rest pose, before overall blending; not a total torso angle. |
| Joint Turn Speed | 360 degrees/second | Lower values soften torso turns but introduce lag. |
| Blend In / Out | 0.12 / 0.15 seconds | Entry and exit rates across a full weight range. |
| Head / Left Arm / Right Arm Preservation | 1 / 1 / 1 | Independent extra orientation preservation after the torso solve. |
| Arm / Head Correction Limit | 180 / 180 degrees | Caps extra orientation correction before its weight is applied. These are not anatomical joint limits. |
| Neck Share | 0.5 | Redistributes head correction between Neck and Head without changing the final head direction. |
| LookAt Suppression | 1 | Automatic LookAt fade during stabilization. Lower values retain more LookAt and can reintroduce torso twist. |

For a looser starting comparison, try Facing Lock **0.65**, arm preservation **0.6**, and Arm Correction Limit **60**. These are exploration values, not a validated final style. Adjust one control at a time and compare left/right movement. Torso shares redistribute the work, so reducing all shares by the same factor does not reduce total correction; use Stabilization Weight for that. The solver preserves bone positions relative to their parents and leaves hips/legs alone, but it does not constrain exact hand positions or solve anatomical shoulder limits.

**CR > Workshop > Validate Character Pose Tuning (Play Mode)** checks the actual resulting poses with zero/uneven torso shares, facing lock, independent arms, angular caps, neck distribution and LookAt blending. The original Character IK validator continues to check the full-preservation baseline.

**CR > Workshop > Validate Global Cooldown** checks timer boundaries, long frames, default duration, haste, duration capture, reentrant requests, actor isolation, exemptions and failed casts. Report: `Temp/GlobalCooldownValidation.txt`.

**CR > Workshop > Validate Player Instant Omni (Play Mode)** runs both shortcuts through a temporary Input System keyboard, checks all 18 standing/eight-direction combinations, GCD rejection/expiry, movement transitions, natural completion, both action slot cooldown overlays/raycasts/clicks, unsupported configurations and disabled-controller cleanup without resetting GCD. Keep the Game view focused and leave movement keys released during the check. Results are written to `Temp/InstantOmniValidation.txt`. The temporary device is removed and the player returns to spawn when the check finishes.
