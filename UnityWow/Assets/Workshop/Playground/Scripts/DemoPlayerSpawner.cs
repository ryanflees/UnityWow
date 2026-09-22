// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    /// <summary>Combines the standard controller and a Humanoid visual at a scene spawn point.</summary>
    public sealed class DemoPlayerSpawner : MonoBehaviour
    {
        public PlayerController m_PlayerPrefab;
        public GameObject m_CharacterPrefab;
        public SpawnPoint m_SpawnPoint;
        [Tooltip("Assign a camera prefab here. It is created with the player when the scene starts.")]
        public TPCameraController m_CameraPrefab;
        [Min(0.01f)] public float m_CharacterScale = 1.2f;

        public PlayerController Player { get; private set; }

        private void Start()
        {
            if (m_PlayerPrefab == null || m_CharacterPrefab == null || m_SpawnPoint == null || m_CameraPrefab == null)
            {
                Debug.LogError("The WowGirl demo needs a player prefab, character prefab, camera prefab and spawn point.", this);
                return;
            }

            Player = PlayerSpawner.SpawnWithCameraPrefab(m_PlayerPrefab, m_SpawnPoint.transform, m_CameraPrefab,
                m_CharacterPrefab, ConfigureCharacter);
        }

        private void OnDestroy()
        {
            if (Player != null) Destroy(Player.gameObject);
        }

        private void ConfigureCharacter(PlayerController player)
        {
            player.name = "WowGirl Player";
            Character character = player.m_Character;
            GameObject visual = character.gameObject;
            visual.name = "WowGirl";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * m_CharacterScale;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman || animator.runtimeAnimatorController == null)
            {
                throw new System.InvalidOperationException("The demo visual needs a Humanoid Animator with an Animator Controller.");
            }

            animator.applyRootMotion = false;
            character.m_Animator = animator;
            CharacterLookAtIk lookAt = character.m_LookAtIk != null ? character.m_LookAtIk : visual.GetComponent<CharacterLookAtIk>();
            if (lookAt == null) lookAt = visual.AddComponent<CharacterLookAtIk>();
            lookAt.m_Animator = animator;
            lookAt.m_BaseTransform = player.m_CharacterRoot;
            lookAt.m_Head = animator.GetBoneTransform(HumanBodyBones.Head);
            lookAt.m_BodyBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            lookAt.m_EnableBody = true;
            lookAt.m_BodyWeight = 0.771f;
            lookAt.m_Weight = 0.829f;
            lookAt.m_HeadWeight = 0.663f;
            lookAt.m_MaxAngle = 85f;
            lookAt.m_TargetDistance = 8f;
            lookAt.m_TargetHeight = 1.45f;
            lookAt.m_UseTargetTransform = false;
            character.m_LookAtIk = lookAt;
        }
    }
}

