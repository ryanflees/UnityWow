// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    /// <summary>Demo-only recovery for exploring the valley and jumping beyond its boundary.</summary>
    public sealed class PlaygroundRespawn : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("Spawner")]
        public DemoPlayerSpawner m_Spawner;
        [UnityEngine.Serialization.FormerlySerializedAs("FallHeight")]
        public float m_FallHeight = -12f;

        private void Update()
        {
            if (!m_Spawner || !m_Spawner.Player) return;
            bool reset = m_Spawner.Player.m_PlayerMotor.Position.y < m_FallHeight;
#if ENABLE_INPUT_SYSTEM
            reset |= UnityEngine.InputSystem.Keyboard.current?.rKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            reset |= Input.GetKeyDown(KeyCode.R);
#endif
            if (reset) ReturnToSpawn();
        }

        public void ReturnToSpawn()
        {
            if (!m_Spawner || !m_Spawner.Player || !m_Spawner.m_SpawnPoint) return;
            Transform spawn = m_Spawner.m_SpawnPoint.transform;
            m_Spawner.Player.InitializeSpawn(spawn.position, spawn.forward, Vector3.up, m_Spawner.Player.m_TPCameraController);
        }
    }
}

