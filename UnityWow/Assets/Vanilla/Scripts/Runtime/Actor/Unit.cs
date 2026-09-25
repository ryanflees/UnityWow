using System;
using System.Collections.Generic;
using UnityEngine;

namespace CR
{
    [DisallowMultipleComponent]
    public sealed class Unit : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float m_InitialMaxHealth = 100f;
        [SerializeField, Min(0f)] private float m_InitialMaxMana = 100f;
        [SerializeField, HideInInspector] private UnitRuntimeState m_State;
        [SerializeField, HideInInspector] private bool m_IsInitialized;
        private bool m_IsNotifying;
        private readonly Queue<(UnitRuntimeState Previous, UnitRuntimeState Next)> m_Notifications =
            new Queue<(UnitRuntimeState Previous, UnitRuntimeState Next)>();
        private event Action<Unit> m_Unavailable;
        public event Action<UnitRuntimeState, UnitRuntimeState> m_StateChanged;

        public UnitRuntimeState State
        {
            get
            {
                EnsureInitialized();
                return m_State;
            }
        }

        public bool SetHealth(float value)
        {
            ValidateValue(value, 0f, nameof(value));
            UnitRuntimeState state = State;
            if (!state.IsAlive && value > 0f) return false;
            float health = Mathf.Min(value, state.MaxHealth);
            return Commit(new UnitRuntimeState(health, state.MaxHealth, state.CurrentMana, state.MaxMana,
                health > 0f ? UnitLifeState.Alive : UnitLifeState.Dead, state.SelectedTarget,
                health > 0f && state.IsInCombat));
        }

        public bool SetMaxHealth(float value)
        {
            ValidateValue(value, 1f, nameof(value));
            UnitRuntimeState state = State;
            return Commit(new UnitRuntimeState(Mathf.Min(state.CurrentHealth, value), value,
                state.CurrentMana, state.MaxMana, state.LifeState, state.SelectedTarget, state.IsInCombat));
        }

        public bool SetMana(float value)
        {
            ValidateValue(value, 0f, nameof(value));
            UnitRuntimeState state = State;
            return Commit(new UnitRuntimeState(state.CurrentHealth, state.MaxHealth, Mathf.Min(value, state.MaxMana),
                state.MaxMana, state.LifeState, state.SelectedTarget, state.IsInCombat));
        }

        public bool SetMaxMana(float value)
        {
            ValidateValue(value, 0f, nameof(value));
            UnitRuntimeState state = State;
            return Commit(new UnitRuntimeState(state.CurrentHealth, state.MaxHealth, Mathf.Min(state.CurrentMana, value),
                value, state.LifeState, state.SelectedTarget, state.IsInCombat));
        }

        public bool SetTarget(Unit target)
        {
            if (!ReferenceEquals(target, null) && (target == null || !target.isActiveAndEnabled || !isActiveAndEnabled))
                return false;
            UnitRuntimeState state = State;
            return Commit(new UnitRuntimeState(state.CurrentHealth, state.MaxHealth, state.CurrentMana,
                state.MaxMana, state.LifeState, target, state.IsInCombat));
        }

        public bool ClearTarget() => SetTarget(null);

        public bool SetCombatState(bool isInCombat)
        {
            UnitRuntimeState state = State;
            if (isInCombat && (!state.IsAlive || !isActiveAndEnabled)) return false;
            return Commit(new UnitRuntimeState(state.CurrentHealth, state.MaxHealth, state.CurrentMana,
                state.MaxMana, state.LifeState, state.SelectedTarget, isInCombat));
        }

        public bool Revive(float health)
        {
            ValidateValue(health, 0f, nameof(health));
            if (health == 0f) throw new ArgumentOutOfRangeException(nameof(health));
            UnitRuntimeState state = State;
            if (state.IsAlive) return false;
            return Commit(new UnitRuntimeState(Mathf.Min(health, state.MaxHealth), state.MaxHealth,
                state.CurrentMana, state.MaxMana, UnitLifeState.Alive, state.SelectedTarget, false));
        }

        public bool ResetForSpawn()
        {
            UnitRuntimeState state = State;
            return Commit(new UnitRuntimeState(state.MaxHealth, state.MaxHealth, state.MaxMana, state.MaxMana,
                UnitLifeState.Alive, null, false));
        }

        private void Awake() => EnsureInitialized();

        private void OnEnable()
        {
            EnsureInitialized();
            Unit target = m_State.SelectedTarget;
            if (!ReferenceEquals(target, null))
            {
                if (target == null || !target.isActiveAndEnabled) ClearTarget();
                else
                {
                    target.m_Unavailable -= OnTargetUnavailable;
                    target.m_Unavailable += OnTargetUnavailable;
                }
            }
        }

        private void OnDisable() => ReleaseRelationships();
        private void OnDestroy() => ReleaseRelationships();

        private void ReleaseRelationships()
        {
            if (!m_IsInitialized) return;
            UnitRuntimeState state = m_State;
            Commit(new UnitRuntimeState(state.CurrentHealth, state.MaxHealth, state.CurrentMana,
                state.MaxMana, state.LifeState, null, false));
            m_Unavailable?.Invoke(this);
        }

        private void OnTargetUnavailable(Unit target)
        {
            if (ReferenceEquals(m_State.SelectedTarget, target)) ClearTarget();
        }

        private void EnsureInitialized()
        {
            if (m_IsInitialized) return;
            ValidateValue(m_InitialMaxHealth, 1f, nameof(m_InitialMaxHealth));
            ValidateValue(m_InitialMaxMana, 0f, nameof(m_InitialMaxMana));
            m_State = new UnitRuntimeState(m_InitialMaxHealth, m_InitialMaxHealth, m_InitialMaxMana,
                m_InitialMaxMana, UnitLifeState.Alive, null, false);
            m_IsInitialized = true;
        }

        private bool Commit(UnitRuntimeState next)
        {
            if (m_State.Matches(next)) return false;
            UnitRuntimeState previous = m_State;
            if (!ReferenceEquals(previous.SelectedTarget, next.SelectedTarget))
            {
                if (!ReferenceEquals(previous.SelectedTarget, null)) previous.SelectedTarget.m_Unavailable -= OnTargetUnavailable;
                if (!ReferenceEquals(next.SelectedTarget, null)) next.SelectedTarget.m_Unavailable += OnTargetUnavailable;
            }
            m_State = next;
            // Nested changes commit immediately, but their snapshots are delivered after the current notification.
            m_Notifications.Enqueue((previous, next));
            if (m_IsNotifying) return true;
            m_IsNotifying = true;
            try
            {
                while (m_Notifications.Count > 0)
                {
                    var notification = m_Notifications.Dequeue();
                    m_StateChanged?.Invoke(notification.Previous, notification.Next);
                }
            }
            finally
            {
                m_IsNotifying = false;
                m_Notifications.Clear();
            }
            return true;
        }

        private static void ValidateValue(float value, float minimum, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum)
                throw new ArgumentOutOfRangeException(parameter, "Value must be finite and within the allowed range.");
        }
    }
}
