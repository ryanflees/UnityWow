// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CR
{
	public static class InstantOmniValidator
	{
		private static bool m_IsRunning;
		public static bool IsRunning => m_IsRunning;

		[MenuItem("CR/Workshop/Validate Player Instant Omni (Play Mode)")]
		public static void Validate()
		{
			DemoPlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<DemoPlayerSpawner>();
			if (!Application.isPlaying || spawner == null || spawner.Player == null)
				throw new InvalidOperationException("Enter Play Mode in PlaygroundKCC before validating the player spell.");
			if (m_IsRunning) throw new InvalidOperationException("Instant Omni validation is already running.");
			spawner.StartCoroutine(ValidateSequence(spawner));
		}

		private static IEnumerator ValidateSequence(DemoPlayerSpawner spawner)
		{
			m_IsRunning = true;
			PlayerController player = spawner.Player;
			Character character = player.m_Character;
			Animator animator = character.m_Animator;
			SpellController controller = player.m_SpellController;
			Keyboard previousKeyboard = Keyboard.current;
			Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
			int releases = 0;
			Action<SpellDefinition> onRelease = spell => releases++;
			controller.m_SpellReleased += onRelease;
			bool passed = false;
			try
			{
				Require(Application.isFocused, "Focus the Game view so the normal player input gate accepts test input.");
				SpellDefinition definition = controller.m_SpellCollection.GetSpell(spawner.m_TestSpellId);
				Require(definition != null && definition.m_Icon != null && definition.m_Cast.m_Type == SpellCastType.Instant &&
					definition.m_Cast.m_CanMoveWhileCasting && definition.m_Target.m_Type == SpellTargetType.None &&
					definition.m_EffectList.Count == 0 && definition.m_CostList.Count == 0 && definition.m_Cooldown.m_GlobalCooldown == 1.5f &&
					definition.m_Cooldown.m_TriggersGlobalCooldown && definition.m_Cooldown.m_IsAffectedByGlobalCooldown,
					"The test spell must be instant, movable, targetless, free of effects/costs and use a 1.5-second GCD.");
				ValidateRejections(controller, definition);
				Key[][] directions =
				{
					Array.Empty<Key>(), new[] { Key.W }, new[] { Key.S }, new[] { Key.A }, new[] { Key.D },
					new[] { Key.W, Key.A }, new[] { Key.W, Key.D }, new[] { Key.S, Key.A }, new[] { Key.S, Key.D }
				};
				foreach (Key actionKey in new[] { Key.Digit1, Key.Digit4 })
				foreach (Key[] direction in directions)
				{
					SetKeys(keyboard);
					controller.CancelPresentation();
					ReturnToSpawn(spawner);
					yield return WaitForStanding(player);
					yield return WaitForGlobalCooldown(controller);
					Vector3 start = player.GetPlayerPosition();
					int before = releases;
					SetKeys(keyboard, direction.Concat(new[] { actionKey }).ToArray());
					yield return new WaitForSeconds(0.25f);
					Require(releases == before + 1, "One key press must release exactly one spell.");
					Require(animator.GetCurrentAnimatorStateInfo(character.SpellLayerIndex).IsName("SpellLayer.CastSpellOmniFinish"), $"{actionKey} did not play the Omni release state.");
					Require(animator.GetLayerWeight(character.UpperBodyLayerIndex) > 0.99f, "The upper-body release was lost.");
					foreach (ActionBarSlotView view in spawner.m_ActionBarPresenter.m_Slots)
						Require(!view.m_Button.interactable && view.m_CooldownFill.enabled && view.m_CooldownFill.fillAmount > 0f,
							"Both action slots must display the shared GCD and reject clicks.");
					if (direction.Length == 0)
						Require(animator.GetLayerWeight(character.SpellLayerIndex) > 0.99f, "Standing release must retain the full-body layer.");
					else
					{
						Require(animator.GetLayerWeight(character.SpellLayerIndex) < 0.01f, "Moving release must reveal locomotion.");
						Require(Vector3.ProjectOnPlane(player.GetPlayerPosition() - start, Vector3.up).magnitude > 0.05f, "Casting stopped player displacement.");
					}
					float phase = character.SpellNormalizedTime;
					yield return new WaitForSeconds(0.12f);
					Require(releases == before + 1 && character.SpellNormalizedTime > phase, $"Holding {actionKey} retriggered or froze the release.");
				}

				SetKeys(keyboard);
				ReturnToSpawn(spawner);
				yield return WaitForStanding(player);
				yield return WaitForGlobalCooldown(controller);
				SetKeys(keyboard, Key.W, Key.Digit1);
				yield return new WaitForSeconds(0.25f);
				float phaseBeforeStop = character.SpellNormalizedTime;
				SetKeys(keyboard);
				yield return new WaitForSeconds(0.2f);
				Require(character.SpellNormalizedTime > phaseBeforeStop && animator.GetLayerWeight(character.SpellLayerIndex) > 0.99f,
					"Stopping movement restarted the spell or did not restore full-body playback.");
				int beforeRepeat = releases;
				SetKeys(keyboard, Key.D, Key.Digit4);
				yield return new WaitForSeconds(0.25f);
				Require(releases == beforeRepeat && character.SpellNormalizedTime > phaseBeforeStop && animator.GetLayerWeight(character.SpellLayerIndex) < 0.01f,
					"Pressing slot 4 during slot 1's GCD must preserve the ongoing action and allow movement.");
				SetKeys(keyboard);
				float deadline = Time.time + 5f;
				while (character.IsPlayingSpellAnimation && Time.time < deadline) yield return null;
				Require(!character.IsPlayingSpellAnimation && animator.GetLayerWeight(character.UpperBodyLayerIndex) == 0f,
					"Release did not finish naturally and clear its layers.");
				yield return WaitForGlobalCooldown(controller);
				SetKeys(keyboard, Key.Digit4);
				yield return new WaitForSeconds(0.15f);
				Require(releases == beforeRepeat + 1, "Slot 4 must become usable after the shared GCD expires.");
				SetKeys(keyboard);

				Canvas.ForceUpdateCanvases();
				EventSystem eventSystem = EventSystem.current;
				foreach (ActionBarSlotView slot in spawner.m_ActionBarPresenter.m_Slots)
				{
				yield return WaitForGlobalCooldown(controller);
				Require(eventSystem != null && slot.m_Button.interactable && slot.m_Icon.sprite == definition.m_Icon, "The action slot is not bound to the spell icon.");
				RectTransform frame = slot.m_Button.targetGraphic.rectTransform;
				Vector2 point = RectTransformUtility.WorldToScreenPoint(null, frame.TransformPoint(frame.rect.center));
				PointerEventData pointer = new PointerEventData(eventSystem) { position = point, button = PointerEventData.InputButton.Left };
				List<RaycastResult> hits = new List<RaycastResult>();
				eventSystem.RaycastAll(pointer, hits);
				Require(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(slot.m_Button.transform), "The spell slot cannot receive pointer clicks.");
				int beforeClick = releases;
				ExecuteEvents.Execute(slot.m_Button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
				Require(releases == beforeClick + 1, $"Clicking slot {slot.m_SlotIndex + 1} did not use the same cast pipeline exactly once.");
				}
				controller.enabled = false;
				Require(!character.IsPlayingSpellAnimation && animator.GetLayerWeight(character.UpperBodyLayerIndex) == 0f &&
					player.TryActivateActionSlot(0) == SpellCastResult.Disabled, "Disabled casting must clear presentation and reject input.");
				controller.enabled = true;
				Require(player.TryActivateActionSlot(3) == SpellCastResult.GlobalCooldown, "Re-enabling the controller must not reset its GCD.");
				passed = true;
			}
			finally
			{
				controller.enabled = true;
				controller.CancelPresentation();
				controller.m_SpellReleased -= onRelease;
				InputSystem.RemoveDevice(keyboard);
				if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
				ReturnToSpawn(spawner);
				m_IsRunning = false;
				Directory.CreateDirectory("Temp");
				string report = passed ? "Passed Key 1/4 in all 18 standing/eight-direction combinations, shared GCD rejection and expiry, movement transitions, natural completion, both cooldown overlays and slot clicks, unsupported spell rejection, and disable cleanup without resetting GCD." : "Instant Omni validation did not complete. Inspect the Console for the failed assertion.";
				File.WriteAllText("Temp/InstantOmniValidation.txt", report + "\n");
				if (passed) Debug.Log(report);
			}
		}

		private static void ValidateRejections(SpellController controller, SpellDefinition definition)
		{
			Require(controller.TryCast(new SpellCastRequest(-1, Quaternion.identity)) == SpellCastResult.UnknownSpell, "An unknown spell must be rejected.");
			SpellConfigCollection original = controller.m_SpellCollection;
			SpellConfigCollection collection = ScriptableObject.CreateInstance<SpellConfigCollection>();
			SpellDefinition unsupported = UnityEngine.Object.Instantiate(definition);
			try
			{
				unsupported.m_Cooldown.m_Cooldown = 5f;
				collection.m_SpellList.Add(unsupported);
				collection.RebuildLookup();
				controller.m_SpellCollection = collection;
				Require(controller.TryCast(new SpellCastRequest(unsupported.m_Id, Quaternion.identity)) == SpellCastResult.UnsupportedSpell,
					"Unsupported cooldown rules must not silently become free casts.");
			}
			finally
			{
				controller.m_SpellCollection = original;
				UnityEngine.Object.Destroy(unsupported);
				UnityEngine.Object.Destroy(collection);
			}
		}

		private static void SetKeys(Keyboard keyboard, params Key[] keys)
		{
			InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
		}

		private static IEnumerator WaitForStanding(PlayerController player)
		{
			yield return new WaitForSeconds(0.15f);
			float deadline = Time.time + 5f;
			while ((!player.GetState().isGrounded || !player.m_Character.m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Stand")) && Time.time < deadline)
				yield return null;
			Require(player.GetState().isGrounded && player.m_Character.m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Stand"), "The test player did not settle into standing after respawn.");
		}

		private static IEnumerator WaitForGlobalCooldown(SpellController controller)
		{
			float deadline = Time.time + 5f;
			while (controller.GlobalCooldownRemaining > 0f && Time.time < deadline) yield return null;
			Require(controller.GlobalCooldownRemaining == 0f, "Global cooldown did not expire.");
			yield return null;
		}

		private static void ReturnToSpawn(DemoPlayerSpawner spawner)
		{
			Transform spawn = spawner.m_SpawnPoint.transform;
			spawner.Player.InitializeSpawn(spawn.position, spawn.forward, Vector3.up, spawner.Player.m_TPCameraController);
		}

		private static void Require(bool condition, string message)
		{
			if (!condition) throw new InvalidOperationException(message);
		}
	}
}
