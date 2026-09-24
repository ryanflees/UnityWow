# Spell and action development baseline

Reviewed on 2026-09-24 against the project source and the connected Unity 6000.3.23f1 Editor.

## Decision

Keep the current separation between `SpellDefinition`, actor runtime state, and character animation. The existing three-layer spell animation system is a suitable foundation for directed/omnidirectional instant, cast-time, and channeled spells. It does not need a replacement animation framework before the first combat slice.

The project is ready to extend, but is not yet a working combat system. The six definitions under this workshop contain no gameplay effects or assigned icons. Animation preview success is not evidence that resource costs, cooldowns, targeting, or damage work. The subsequent [Player instant Omni test](../Playground/README.md) adds a seventh definition and connects a free, targetless animation spell to the real player's action slots 1 and 4.

## Current responsibilities

| Part | Current state | Responsibility |
| --- | --- | --- |
| `SpellDefinition` and effect data | Implemented configuration | Stable id, text/icon, cast policy, targeting, costs, cooldowns, typed effect data, presentation intent. Shared assets contain no actor timers or current targets. |
| `SpellConfigCollection` | Implemented lookup | Resolve stable ids. Ambiguous duplicate ids are rejected. Call `RebuildLookup` after intentionally changing the public list at runtime. |
| `SpellDefinitionValidator` | Implemented structural validation | Report missing blocks, invalid numbers, timing/animation mismatches, target ranges, effect triggers and duplicate effect indices. Optional content validation requires an icon and an effect. |
| `SpellPresentation` | Implemented animation adapter | Shared entry point for preparation/release, overlay selection, facing and release pose stabilization. Currently contains no VFX, projectile or audio execution. |
| `Character.Spells` | Implemented playback | Own Animator states, layer weights, movement blending, cancellation and automatic visual completion. `None` and unknown types do not start an animation. |
| `CharacterLookAtIk` | Implemented pose correction | Release pose stabilization and LookAt; uses the same SpellLayer phase as animation. |
| `SpellAnimationWorkshop` | Animation-only harness | Supply preview timings, movement controls and presentation requests. Release completion follows Animator, including changed playback speed. |
| `SpellController`, `SpellCastRequest` | Instant animation and GCD slice implemented | Validate and dispatch free targetless instant animations with actor-owned, haste-adjusted GCD and per-spell trigger/affected switches. Unsupported gameplay features return failure results. |
| `SpellCastInstance` | Empty placeholder | Timed cast state remains to be implemented; individual/shared-group cooldowns and gameplay interruptions also remain pending. |
| `ActorAttributes` | Haste for GCD implemented | Health, resources and other combat attributes remain to be implemented. |
| `CombatActor`, `ActorControlState` | Empty placeholders | Identity, faction and combined action restrictions remain to be implemented. |
| Player spell processor and action bar | Slots 1 and 4 implemented | Both keys and bound icons forward through the same player/controller request path. Target selection remains an empty placeholder. |
| `CharacterAnimationDriver` | Empty placeholder | Do not introduce a second owner of spell layer weights. Add arbitration only when competing death, hit, melee or other actions require it. |

## Interfaces to retain

- Bind action bar slots to `SpellDefinition.m_Id`. Read icon, display name and description from the same definition. UI forwards requests; it must not apply damage or start Animator states directly.
- Use one `SpellPresentation` per character. Set `m_Character`, then call `Play(definition, false, facingRotation)` for preparation/channeling and `Play(definition, true, facingRotation)` for release. Check its boolean result. Call `SetFacing` when the logical casting direction changes, and `Stop` on cancellation. Disabling presentation clears the layers immediately.
- `IsPlaying` describes visual playback only. `None` is a successful no-op when a character is assigned. A missing or invalid Animator binding returns false. A missing stabilization clip warns while leaving valid animation playback available.
- Current animation state names and the two release clip names belong to the WowGirl presentation binding. New character rigs or renamed/overridden release clips need an explicit per-character binding/profile before reuse; do not add those model-specific names to spell ids or effect executors.
- Preserve the unmasked SpellLayer and its synchronized UpperBody layer with explicit motion overrides. SpellLayer remains the only animation clock and continues at zero weight. Base locomotion stays independent.
- The current movement overlay supports release animations. Moving cast-time/channel preparation, melee windup/recovery, hit reactions, death priority and weapon attachments require their own implementation and tests. Merely setting `m_CanMoveWhileCasting` does not add that support.

## Contract for the next gameplay implementation

The following defines the intended runtime behavior; it is not implemented by the animation workshop.

1. `SpellCastRequest` carries spell id plus an explicit unit, ground point or direction. Player input and AI submit the same request type. On acceptance, capture the target and direction into a per-cast instance; changing the selected target later must not retarget that cast accidentally.
2. `SpellController` is the sole authority for accepting casts. Validate definition, actor life/control state, target relation, range, line of sight, facing, all costs, GCD, shared cooldown and charges before accepting. Return a structured failure reason for UI. Unsupported target/effect features fail explicitly rather than silently succeeding.
3. Keep the gameplay clock independent of Animator. Instant spells release when accepted; cast-time spells release once their gameplay duration ends; channels activate immediately and tick at interval, twice interval, and so on, including a tick exactly at the duration. A slow frame must not skip or duplicate ticks. Channel completion stops its loop without a release animation by default. Passive spells cannot be manually cast.
4. Define `CastComplete` as successful release/activation: immediate for instant and channel activation, after the cast timer for cast-time spells. Charge `CastStart` costs on acceptance, `CastComplete` costs at release/activation, and `ChannelTick` costs before each tick. Aggregate costs for the same resource/timing and check/deduct atomically. Never spend half a multi-resource cost.
5. Revalidate relevant actor/target restrictions at release and channel ticks. Movement cancels stationary preparation/channeling; jumping and forced displacement use explicit policy. `m_IsInterruptible` controls hostile interrupt effects, not owner cancellation, death or despawn cleanup. Define refund and cooldown policies deliberately; do not let visual cancellation decide them.
6. Use actor-owned GCD, spell cooldown, shared-group and charge state. A baseline policy is GCD on acceptance, ordinary/shared cooldown on release/activation, one charge consumed on release, and serial charge recovery. The existing shared-group field has no separate duration; use the triggering spell's cooldown until a group definition is introduced. These policies must have boundary tests before enabling the fields in gameplay.
7. Resolve targets independently from the effect implementation. `Self` selects the caster; `Unit` selects a captured actor; `Ground` and `Direction` need point/cone/area selection. `Dead` is a life-state modifier alongside faction flags, not an independent faction. `m_MaxTargetCount` is positive; define deterministic target ordering. Range and radius remain world units.
8. Dispatch typed effects through handlers using caster, accepted cast, resolved target, effect index and trigger context. Damage and healing are distinct handlers; resource modification may be signed. Apply channel effects once per tick. Delayed effects/projectiles own their lifetime after successful release and cannot fire twice on duplicate collision callbacks. Add recursion depth/cycle protection before supporting triggered spells.
9. Publish accepted, released, ticked, completed, cancelled and rejected outcomes for presentation and UI. Presentation errors must not create extra gameplay effects. Release animation completion must not be used as a damage event, cooldown timer or input lock.

The schema already reserves auras, critical hits, scaling, multiple charges, triggered spells, delayed effects and projectiles. These fields are data only. Implement and advertise each capability together with its executor and tests. Structural validation does not certify those capabilities or cross-asset trigger cycles.

## Icons and authoring

The asset audit found 1,118 textures under `Assets/Art/UI/Icon/spells`; all were imported as default textures rather than Sprites. No arbitrary icon-to-spell mapping was assigned during this review.

Select the required textures or a folder in the Project window, then use **Assets > CR > Prepare Selected Spell Icons**. The command only handles assets under `Assets/Art/UI/Icon/`, imports single Sprites, disables mipmaps and clamps wrapping. It preserves source images and existing compression/filter settings. Drag the resulting Sprite into `SpellDefinition.m_Icon`. Selecting the icon folder intentionally processes all its textures.

The Spell Definition inspector now displays structural errors and content warnings. **CR > Spells > Validate Spell Definitions** audits definitions, collections, missing triggered-spell references and icon readiness, writing `Temp/SpellDefinitionAudit.txt`. Duplicate ids are considered global authoring errors. This report is a configuration audit, not a combat readiness pass.

Create gameplay definitions separately from the six workshop fixtures. The workshop builder resets preview ids, cast timings and presentation fields when rebuilding the scene. Keeping dedicated combat assets prevents that utility from overwriting gameplay tuning.

## Validation and next combat slice

- **CR > Workshop > Validate Spell Foundation** checks invalid configurations, preview/content distinction, duplicate lookup rejection/rebuild, absent animation behavior, shared presentation, slower release completion and immediate cancellation. Report: `Temp/SpellFoundationValidation.txt`.
- **CR > Workshop > Validate Spell Animations** covers existing layering, continuous phase, standing/moving weights, restart, cancellation and automatic exit.
- **CR > Workshop > Validate Spell Foot Stability** covers the six spell clips and both cast-to-release transitions.
- **CR > Workshop > Validate Moving Instant Spells (Play Mode)** covers all 18 instant/direction combinations through the workshop and shared presentation, including component disable cleanup and rejecting playback while disabled.
- **CR > Workshop > Validate Character IK (Play Mode)** covers pose stabilization, limits, repeated releases and locomotion blending.

The first combat slice should contain one player, one target dummy, health/mana, a damage handler, and three real definitions: one instant, one cast-time and one channel. Connect their icons and three action bar slots through the same cast request path. Cover missing/dead/out-of-range targets, insufficient resources, GCD/cooldown rejection, moving/cancelling a cast, target loss, large-frame channel ticks, independent state on two actors, and UI/animation/effect agreement. This validates the architecture before adding projectiles, auras, charge spells or melee action arbitration.

### Results from this review

Unity compilation, foundation validation, the existing animation suite, foot stability, moving instant playback and IK validation passed. The foot/toe drift bound remained 0.452 mm. Play Mode covered 18 instant/direction combinations and presentation disable cleanup. IK validation covered 72 poses, four continuous sequences, 16 repeated-cast sequences and 180 pre-IK arm poses. The configuration audit reported six definitions, zero structural errors, and 12 expected warnings for missing icons/effects. These results apply to the animation foundation; combat acceptance tests await the runtime implementation above.
