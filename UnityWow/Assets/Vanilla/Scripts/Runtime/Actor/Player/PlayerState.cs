// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    /// <summary>A value snapshot for gameplay, animation, debugging, or external adapters.</summary>
    public struct PlayerState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 characterFaceDirection;
        public Vector2 moveInput;
        public bool isGrounded;
        public bool isBackMove;
        public int jumpCount;
    }
}

