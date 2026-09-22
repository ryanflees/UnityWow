// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    public class PlayerAvatar : MonoBehaviour
    {
        [Header("Visual")]
        public Character m_Character;
        public Transform m_CharacterRoot;

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (m_Character == null)
                m_Character = GetComponentInChildren<Character>();
            if (m_CharacterRoot == null)
            {
                m_CharacterRoot = transform.Find("CharacterRoot")
                    ?? (m_Character != null ? m_Character.transform.parent ?? m_Character.transform : null);
            }
        }
    }
}

