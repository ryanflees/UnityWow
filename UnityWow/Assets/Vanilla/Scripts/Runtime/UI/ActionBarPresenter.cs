// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace CR
{
	[Serializable]
	public class ActionBarSlotView
	{
		public int m_SlotIndex;
		public UnityEngine.UI.Button m_Button;
		public UnityEngine.UI.Image m_Icon;
		public UnityEngine.UI.Image m_CooldownFill;
		public UnityEngine.UI.Text m_CooldownLabel;
		[NonSerialized] public UnityAction m_ClickAction;
	}

	public class ActionBarPresenter : MonoBehaviour
	{
		public ActionBarSlotView[] m_Slots = Array.Empty<ActionBarSlotView>();
		private PlayerController m_Player;

		public void Bind(PlayerController player)
		{
			m_Player = player;
			Refresh();
		}

		public void Refresh()
		{
			foreach (ActionBarSlotView slot in m_Slots)
			{
				if (slot == null) continue;
				SpellDefinition spell = null;
				if (m_Player != null && m_Player.m_SpellController != null && m_Player.m_SpellController.m_SpellCollection != null)
					spell = m_Player.m_SpellController.m_SpellCollection.GetSpell(m_Player.m_ActionBar.GetSpellId(slot.m_SlotIndex));
				RefreshCooldown(slot, spell);
				if (slot.m_CooldownFill != null) slot.m_CooldownFill.sprite = spell != null ? spell.m_Icon : null;
				if (slot.m_Icon != null)
				{
					slot.m_Icon.sprite = spell != null ? spell.m_Icon : null;
					slot.m_Icon.enabled = slot.m_Icon.sprite != null;
				}
			}
		}

		private void LateUpdate()
		{
			foreach (ActionBarSlotView slot in m_Slots)
			{
				if (slot == null) continue;
				SpellDefinition spell = null;
				if (m_Player != null && m_Player.m_SpellController != null && m_Player.m_SpellController.m_SpellCollection != null)
					spell = m_Player.m_SpellController.m_SpellCollection.GetSpell(m_Player.m_ActionBar.GetSpellId(slot.m_SlotIndex));
				RefreshCooldown(slot, spell);
			}
		}

		private void RefreshCooldown(ActionBarSlotView slot, SpellDefinition spell)
		{
			SpellController controller = m_Player != null ? m_Player.m_SpellController : null;
			bool blocked = controller != null && controller.IsGlobalCooldownBlocking(spell);
			float remaining = blocked ? controller.GlobalCooldownRemaining : 0f;
			if (slot.m_Button != null)
				slot.m_Button.interactable = spell != null && m_Player.isActiveAndEnabled && controller.isActiveAndEnabled && !blocked;
			if (slot.m_CooldownFill != null)
			{
				slot.m_CooldownFill.enabled = blocked;
				slot.m_CooldownFill.fillAmount = blocked && controller.GlobalCooldownDuration > 0f ? Mathf.Clamp01(remaining / controller.GlobalCooldownDuration) : 0f;
			}
			if (slot.m_CooldownLabel != null)
			{
				slot.m_CooldownLabel.enabled = blocked;
				slot.m_CooldownLabel.text = blocked ? (Mathf.Ceil(remaining * 10f) / 10f).ToString("0.0") : string.Empty;
			}
		}

		private void OnEnable()
		{
			foreach (ActionBarSlotView slot in m_Slots)
			{
				if (slot == null || slot.m_Button == null) continue;
				slot.m_ClickAction = () => ActivateSlot(slot.m_SlotIndex);
				slot.m_Button.onClick.AddListener(slot.m_ClickAction);
			}
			Refresh();
		}

		private void OnDisable()
		{
			foreach (ActionBarSlotView slot in m_Slots)
				if (slot != null && slot.m_Button != null && slot.m_ClickAction != null)
					slot.m_Button.onClick.RemoveListener(slot.m_ClickAction);
		}

		private void ActivateSlot(int slotIndex)
		{
			if (isActiveAndEnabled && m_Player != null) m_Player.TryActivateActionSlot(slotIndex);
		}
	}
}
