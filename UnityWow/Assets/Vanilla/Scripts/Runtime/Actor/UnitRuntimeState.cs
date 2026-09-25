using System;
using UnityEngine;

namespace CR
{
    public enum UnitLifeState
    {
        Alive,
        Dead
    }

    [Serializable]
    public struct UnitRuntimeState
    {
        [SerializeField] private float m_CurrentHealth;
        [SerializeField] private float m_MaxHealth;
        [SerializeField] private float m_CurrentMana;
        [SerializeField] private float m_MaxMana;
        [SerializeField] private UnitLifeState m_LifeState;
        [SerializeField] private Unit m_SelectedTarget;
        [SerializeField] private bool m_IsInCombat;

        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_MaxHealth;
        public float CurrentMana => m_CurrentMana;
        public float MaxMana => m_MaxMana;
        public UnitLifeState LifeState => m_LifeState;
        public Unit SelectedTarget => m_SelectedTarget;
        public bool IsInCombat => m_IsInCombat;
        public bool IsAlive => m_LifeState == UnitLifeState.Alive;
        public float HealthNormalized => m_MaxHealth > 0f ? m_CurrentHealth / m_MaxHealth : 0f;
        public float ManaNormalized => m_MaxMana > 0f ? m_CurrentMana / m_MaxMana : 0f;

        internal UnitRuntimeState(float currentHealth, float maxHealth, float currentMana, float maxMana,
            UnitLifeState lifeState, Unit selectedTarget, bool isInCombat)
        {
            m_CurrentHealth = currentHealth;
            m_MaxHealth = maxHealth;
            m_CurrentMana = currentMana;
            m_MaxMana = maxMana;
            m_LifeState = lifeState;
            m_SelectedTarget = selectedTarget;
            m_IsInCombat = isInCombat;
        }

        internal bool Matches(UnitRuntimeState other)
        {
            return m_CurrentHealth == other.m_CurrentHealth && m_MaxHealth == other.m_MaxHealth &&
                m_CurrentMana == other.m_CurrentMana && m_MaxMana == other.m_MaxMana &&
                m_LifeState == other.m_LifeState && ReferenceEquals(m_SelectedTarget, other.m_SelectedTarget) &&
                m_IsInCombat == other.m_IsInCombat;
        }
    }
}

