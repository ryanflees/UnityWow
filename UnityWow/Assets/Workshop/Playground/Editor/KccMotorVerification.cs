// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Linq;
using System.Reflection;
using KinematicCharacterController;
using UnityEngine;

namespace CR
{
    public static class KccMotorVerification
    {
        public static void RunFromMenu() => Debug.Log(JsonUtility.ToJson(Run(), true));

        public static MotorVerification.Report Run()
        {
            return MotorVerification.Run("KCC", go =>
            {
                var motor = go.AddComponent<KinematicCharacterMotor>();
                typeof(KinematicCharacterMotor).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motor, null);
                motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                motor.MaxStepHeight = 0.3f;
                motor.MaxStableSlopeAngle = 60f;
                motor.CollidableLayers = 1 << 31;
                return go.AddComponent<KinematicActor>();
            }, (actor, dt) =>
            {
                var motor = actor.GetComponent<KinematicCharacterMotor>();
                motor.UpdatePhase1(dt);
                motor.UpdatePhase2(dt);
                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            });
        }
    }
}


