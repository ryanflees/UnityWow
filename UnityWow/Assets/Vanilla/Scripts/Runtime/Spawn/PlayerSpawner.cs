// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using UnityEngine;

namespace CR
{
    /// <summary>Creates a local controller without any scene singleton or Demo dependency.</summary>
    public static class PlayerSpawner
    {
        public static PlayerController SpawnWithCameraPrefab(PlayerController playerPrefab, Transform spawnPoint,
            TPCameraController cameraPrefab, GameObject characterPrefab = null,
            Action<PlayerController> configure = null)
        {
            if (spawnPoint == null) throw new ArgumentNullException(nameof(spawnPoint));
            return SpawnWithCameraPrefab(playerPrefab, spawnPoint.position, spawnPoint.rotation, cameraPrefab,
                characterPrefab, configure);
        }

        /// <summary>
        /// Creates a camera for this player. The camera is owned by the returned player and is
        /// destroyed with it. The prefab itself is not changed.
        /// </summary>
        public static PlayerController SpawnWithCameraPrefab(PlayerController playerPrefab, Vector3 position,
            Quaternion rotation, TPCameraController cameraPrefab, GameObject characterPrefab = null,
            Action<PlayerController> configure = null)
        {
            if (cameraPrefab == null) throw new ArgumentNullException(nameof(cameraPrefab));
            if (cameraPrefab.GetComponent<Camera>() == null)
                throw new ArgumentException("The camera prefab needs a Camera on the same object as TPCameraController.", nameof(cameraPrefab));

            var camera = UnityEngine.Object.Instantiate(cameraPrefab);
            try
            {
                camera.gameObject.SetActive(true);
                var player = Spawn(playerPrefab, position, rotation, characterPrefab, camera, configure);
                // The controller root is a stationary container; the motor and camera use world positions.
                camera.transform.SetParent(player.transform, true);
                return player;
            }
            catch
            {
                camera.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(camera.gameObject);
                throw;
            }
        }

        public static PlayerController Spawn(PlayerController playerPrefab, Transform spawnPoint,
            GameObject characterPrefab = null, TPCameraController camera = null,
            Action<PlayerController> configure = null)
        {
            if (spawnPoint == null) throw new ArgumentNullException(nameof(spawnPoint));
            return Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, characterPrefab, camera, configure);
        }

        /// <summary>
        /// The optional callback runs after visual assembly and before Awake. The motor is
        /// initialized after Awake, before the first Update or physics tick.
        /// The caller owns the returned player and the supplied camera instance.
        /// </summary>
        public static PlayerController Spawn(PlayerController playerPrefab, Vector3 position, Quaternion rotation,
            GameObject characterPrefab = null, TPCameraController camera = null,
            Action<PlayerController> configure = null)
        {
            if (playerPrefab == null) throw new ArgumentNullException(nameof(playerPrefab));
            if (playerPrefab.GetComponentInChildren<PlayerMotor>(true) == null)
                throw new ArgumentException("The player prefab needs a PlayerMotor.", nameof(playerPrefab));

            // An inactive parent prevents Awake/OnEnable from observing partially wired references.
            var staging = new GameObject("Player Spawn");
            staging.SetActive(false);
            PlayerController player = null;
            try
            {
                player = UnityEngine.Object.Instantiate(playerPrefab, staging.transform, false);
                player.gameObject.SetActive(false);
                if (characterPrefab != null)
                {
                    if (player.m_Character != null || player.GetComponentInChildren<Character>(true) != null)
                        throw new ArgumentException("The player prefab already contains a Character; omit characterPrefab.", nameof(characterPrefab));
                    if (player.m_CharacterRoot == null)
                    {
                        player.m_CharacterRoot = player.transform.Find("CharacterRoot");
                        if (player.m_CharacterRoot == null)
                        {
                            player.m_CharacterRoot = new GameObject("CharacterRoot").transform;
                            player.m_CharacterRoot.SetParent(player.transform, false);
                        }
                    }

                    var visual = UnityEngine.Object.Instantiate(characterPrefab, player.m_CharacterRoot, false);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    player.m_Character = visual.GetComponentInChildren<Character>(true);
                    if (player.m_Character == null)
                    {
                        var animator = visual.GetComponentInChildren<Animator>(true);
                        if (animator == null || animator.runtimeAnimatorController == null)
                            throw new ArgumentException("The character prefab needs a Character or an Animator with a controller.", nameof(characterPrefab));
                        player.m_Character = visual.AddComponent<Character>();
                        player.m_Character.m_Animator = animator;
                        animator.applyRootMotion = false;
                    }
                    if (player.m_Character.m_LookAtIk != null)
                        player.m_Character.m_LookAtIk.m_BaseTransform = player.m_CharacterRoot;
                }

                configure?.Invoke(player);
                player.transform.SetParent(null, true);
                player.gameObject.SetActive(true);
                player.InitializeSpawn(position, rotation * Vector3.forward, Vector3.up, camera);
                return player;
            }
            catch
            {
                if (player != null)
                {
                    player.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(player.gameObject);
                }
                throw;
            }
            finally
            {
                UnityEngine.Object.Destroy(staging);
            }
        }
    }
}

