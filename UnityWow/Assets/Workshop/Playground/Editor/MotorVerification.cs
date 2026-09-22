// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
    public static class MotorVerification
    {
        [Serializable]
        public class Report
        {
            public string backend;
            public List<string> passed = new List<string>();
            public List<string> failures = new List<string>();
        }

        public static Report Run(string backend, Func<GameObject, PlayerMotor> create, Action<PlayerMotor, float> simulate)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run motor verification in Edit Mode.");
            var report = new Report { backend = backend };
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                foreach (float dt in new[] { 0.02f, 1f / 60f, 1f / 120f })
                {
                    Check(report, "ground_speed_and_stop", dt, create, simulate, f =>
                    {
                        f.Settle();
                        Require(f.m_Motor.IsOnGround(), "Did not settle on the ground");
                        Vector3 start = f.m_Motor.Position;
                        f.m_Board.m_MoveVelocity = Vector3.forward * 6f;
                        f.Steps(Mathf.RoundToInt(0.5f / dt));
                        Require(Mathf.Abs((f.m_Motor.Position - start).z - 3f) < 0.08f, "Ground speed changed");
                        f.m_Board.m_MoveVelocity = Vector3.zero;
                        start = f.m_Motor.Position;
                        f.Steps(1);
                        Require(Vector3.ProjectOnPlane(f.m_Motor.Position - start, Vector3.up).magnitude < 0.005f, "Ground movement has inertia");
                    });
                    Check(report, "standing_jump_and_landing", dt, create, simulate, f =>
                    {
                        f.Settle();
                        f.m_Board.m_WasStandingJumpRequested = true;
                        f.m_Motor.RequestJump();
                        f.Steps(1);
                        Require(f.m_Board.m_JumpCount == 1 && !f.m_Motor.IsOnGround(), "Jump was not accepted");
                        Require(f.m_Board.m_CanStartStandingJumpAirMove, "Standing jump lost its air-input window");
                        float peak = f.LocalPosition.y;
                        for (int i = 0; i < Mathf.CeilToInt(1.2f / dt); i++)
                        {
                            f.Steps(1);
                            peak = Mathf.Max(peak, f.LocalPosition.y);
                        }
                        Require(peak > 1.45f && peak < 1.85f, "Unexpected jump apex: " + peak);
                        Require(f.m_Motor.IsOnGround() && f.m_Board.m_WasGroundedThisFrame, "Landing was not reported");
                        Require(!f.m_Board.m_HasAirMoveIntent && !f.m_Board.m_CanStartStandingJumpAirMove, "Air intent did not reset");
                    });
                    Check(report, "moving_jump_keeps_intent", dt, create, simulate, f =>
                    {
                        f.Settle();
                        f.m_Board.m_MoveVelocity = Vector3.forward * 6f;
                        f.m_Motor.RequestJump();
                        f.Steps(1);
                        f.m_Board.m_MoveVelocity = Vector3.left * 6f;
                        f.Steps(Mathf.RoundToInt(0.2f / dt));
                        Require(f.LocalPosition.z > 1f && Mathf.Abs(f.LocalPosition.x) < 0.02f, "Midair input changed the takeoff direction");
                    });
                    Check(report, "wall_and_ceiling", dt, create, simulate, f =>
                    {
                        f.Block(new Vector3(0f, 2f, 2f), new Vector3(10f, 4f, 0.5f));
                        f.Block(new Vector3(0f, 2.7f, 0f), new Vector3(10f, 0.5f, 10f));
                        f.Settle();
                        f.m_Board.m_MoveVelocity = Vector3.forward * 6f;
                        f.m_Motor.RequestJump();
                        float peak = 0f;
                        for (int i = 0; i < Mathf.CeilToInt(1f / dt); i++)
                        {
                            f.Steps(1);
                            peak = Mathf.Max(peak, f.LocalPosition.y);
                        }
                        Require(f.LocalPosition.z < 1.35f, "Passed through the wall");
                        Require(peak < 0.6f, "Passed through the ceiling");
                        Require(f.m_Motor.IsOnGround(), "Did not fall after ceiling contact");
                    });
                    Check(report, "step_up_and_down", dt, create, simulate, f =>
                    {
                        f.Block(new Vector3(0f, 0.1f, 2f), new Vector3(4f, 0.2f, 2f));
                        f.Settle();
                        f.m_Board.m_MoveVelocity = Vector3.forward * 3f;
                        f.Steps(Mathf.RoundToInt(0.7f / dt));
                        Require(f.LocalPosition.z > 1.8f && f.LocalPosition.y > 0.15f, "Could not climb a 0.2m step");
                        f.Steps(Mathf.RoundToInt(1f / dt));
                        Require(f.LocalPosition.y < 0.08f && f.m_Motor.IsOnGround(), "Did not return to the floor");
                    });
                    Check(report, "slope", dt, create, simulate, f =>
                    {
                        GameObject slope = f.Block(new Vector3(0f, 0.65f, 3f), new Vector3(4f, 0.25f, 4f));
                        slope.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
                        Physics.SyncTransforms();
                        f.Settle();
                        f.m_Board.m_MoveVelocity = Vector3.forward * 3f;
                        f.Steps(Mathf.RoundToInt(1f / dt));
                        Require(f.LocalPosition.z > 2.4f && f.LocalPosition.y > 0.4f && f.m_Motor.IsOnGround(), "Could not climb the slope");
                    });
                    Check(report, "fixed_world_up", dt, create, simulate, f =>
                    {
                        f.m_Motor.SetPositionAndRotation(f.m_Origin + Vector3.up * 0.1f, Quaternion.Euler(45f, 30f, 90f), true);
                        f.Steps(1);
                        Require(Vector3.Dot(f.m_Motor.transform.up, Vector3.up) > 0.999f, "m_Motor accepted tilted gravity");
                        Require(f.m_Board.m_GravityUp == Vector3.up, "Blackboard has arbitrary gravity");
                    });
                    Check(report, "gravity_cannot_reverse", dt, create, simulate, f =>
                    {
                        f.m_Motor.SetPositionAndRotation(f.m_Origin + Vector3.up * 3f, Quaternion.identity, true);
                        f.m_Motor.m_GravityStrength = -25f;
                        f.Steps(Mathf.RoundToInt(0.2f / dt));
                        Require(f.LocalPosition.y <= 3.01f && f.m_Motor.Velocity.y <= 0.001f, "Negative gravity strength caused upward gravity");
                    });
                }
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            Directory.CreateDirectory("../References/Vanilla/Reports");
            File.WriteAllText("../References/Vanilla/Reports/" + backend + ".json", JsonUtility.ToJson(report, true) + "\n");
            return report;
        }

        private static void Check(Report report, string name, float dt, Func<GameObject, PlayerMotor> create, Action<PlayerMotor, float> simulate, Action<Fixture> test)
        {
            string id = name + " dt=" + dt;
            using (var fixture = new Fixture(create, simulate, dt))
            {
                try { test(fixture); report.passed.Add(id); }
                catch (Exception exception) { report.failures.Add(id + ": " + exception.Message + " position=" + fixture.LocalPosition); }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class Fixture : IDisposable
        {
            public readonly Vector3 m_Origin = new Vector3(1000f, 1000f, 1000f);
            public readonly PlayerBlackboard m_Board = new PlayerBlackboard();
            public readonly PlayerMotor m_Motor;
            private readonly List<GameObject> m_Objects = new List<GameObject>();
            private readonly Action<PlayerMotor, float> m_Simulate;
            private readonly float m_Dt;
            public Vector3 LocalPosition => m_Motor.Position - m_Origin;

            public Fixture(Func<GameObject, PlayerMotor> create, Action<PlayerMotor, float> simulate, float dt)
            {
                m_Dt = dt;
                m_Simulate = simulate;
                Block(new Vector3(0f, -0.25f, 0f), new Vector3(100f, 0.5f, 100f));
                GameObject go = new GameObject("VerificationMotor");
                m_Objects.Add(go);
                go.transform.position = m_Origin + Vector3.up * 0.02f;
                m_Motor = create(go);
                Physics.SyncTransforms();
                m_Motor.Initialize(m_Board);
                m_Motor.SetPositionAndRotation(go.transform.position, Quaternion.identity, true);
            }

            public GameObject Block(Vector3 position, Vector3 size)
            {
                GameObject go = new GameObject("VerificationBlock");
                go.layer = 31;
                go.transform.position = m_Origin + position;
                go.AddComponent<BoxCollider>().size = size;
                m_Objects.Add(go);
                Physics.SyncTransforms();
                return go;
            }

            public void Settle() => Steps(Mathf.CeilToInt(0.2f / m_Dt));

            public void Steps(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Physics.SyncTransforms();
                    m_Simulate(m_Motor, m_Dt);
                    Physics.SyncTransforms();
                }
            }

            public void Dispose()
            {
                foreach (var go in m_Objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}


