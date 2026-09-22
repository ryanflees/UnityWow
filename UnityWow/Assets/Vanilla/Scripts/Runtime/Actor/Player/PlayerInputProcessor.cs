// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerInputProcessor : IPlayerProcessor
	{
		private bool m_HasCapturedCursor;
		private bool m_WaitForMouseRelease;
		private int m_SuppressLookFrames;
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
		private bool m_HasSavedCursorPosition;
		private CursorPoint m_SavedCursorPosition;

		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		private struct CursorPoint
		{
			public int x;
			public int y;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out CursorPoint point);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool SetCursorPos(int x, int y);
#endif

		public PlayerInputProcessor(PlayerController controller)
		{
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			RuntimeInputState inputState = RuntimeInputManager.CurrentInput;

			if (!blackboard.m_InputEnabled || !Application.isFocused)
			{
				ResetInput(blackboard);
				ReleaseCursor(Application.isFocused);
				return;
			}

			// Escape / the Editor can release the lock independently of our mouse buttons.
			if (m_HasCapturedCursor && Cursor.lockState != CursorLockMode.Locked)
				ReleaseCursor(false);
			if (!inputState.m_PrimaryMouse.m_IsPressed && !inputState.m_SecondaryMouse.m_IsPressed)
				m_WaitForMouseRelease = false;

			blackboard.m_IsPrimaryMouseRotating = !m_WaitForMouseRelease && inputState.m_PrimaryMouse.m_IsPressed;
			blackboard.m_IsSecondaryMouseRotating = !m_WaitForMouseRelease && inputState.m_SecondaryMouse.m_IsPressed;
			blackboard.m_WasSecondaryMousePressed = !m_WaitForMouseRelease && inputState.m_SecondaryMouse.m_WasPressed;
			blackboard.m_WasSecondaryMouseReleased = !m_WaitForMouseRelease && inputState.m_SecondaryMouse.m_WasReleased;
			blackboard.m_WasMoveForwardPressed = inputState.m_MoveForward.m_WasPressed;
			blackboard.m_WasMoveBackwardPressed = inputState.m_MoveBackward.m_WasPressed;
			blackboard.m_WasMoveLeftPressed = inputState.m_MoveLeft.m_WasPressed;
			blackboard.m_WasMoveRightPressed = inputState.m_MoveRight.m_WasPressed;
			blackboard.m_WasJumpPressed = inputState.m_Jump.m_WasPressed;
			blackboard.m_IsCameraRotating = blackboard.m_IsPrimaryMouseRotating || blackboard.m_IsSecondaryMouseRotating;
			blackboard.m_MoveInput = GetMoveInput(inputState, blackboard.m_IsPrimaryMouseRotating && blackboard.m_IsSecondaryMouseRotating);
			blackboard.m_ZoomInput = inputState.m_Zoom;

			if (blackboard.m_IsCameraRotating)
			{
				HideCursor();
				// Locking recenters the OS pointer; don't interpret that warp as camera movement.
				blackboard.m_LookInput = m_SuppressLookFrames > 0 ? Vector2.zero : inputState.m_Look;
				if (m_SuppressLookFrames > 0) m_SuppressLookFrames--;
			}
			else
			{
				ShowCursor();
				blackboard.m_LookInput = Vector2.zero;
			}
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void ReleaseCursor(bool restorePosition = true)
		{
			ShowCursor(restorePosition);
			m_WaitForMouseRelease = true;
		}

		private void ResetInput(PlayerBlackboard blackboard)
		{
			blackboard.m_MoveInput = Vector2.zero;
			blackboard.m_LookInput = Vector2.zero;
			blackboard.m_ZoomInput = 0f;
			blackboard.m_IsCameraRotating = false;
			blackboard.m_IsPrimaryMouseRotating = false;
			blackboard.m_IsSecondaryMouseRotating = false;
			blackboard.m_WasSecondaryMousePressed = false;
			blackboard.m_WasSecondaryMouseReleased = false;
			blackboard.m_WasMoveForwardPressed = false;
			blackboard.m_WasMoveBackwardPressed = false;
			blackboard.m_WasMoveLeftPressed = false;
			blackboard.m_WasMoveRightPressed = false;
			blackboard.m_WasJumpPressed = false;
			blackboard.m_WasStandingJumpRequested = false;
			blackboard.m_IsFaceControlledByCamera = false;
			blackboard.m_ShouldSnapFaceAngle = false;
		}

		private void HideCursor()
		{
			if (m_HasCapturedCursor) return;
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
			// Desktop coordinates also work in a docked Game view and on multiple monitors.
			m_HasSavedCursorPosition = GetCursorPos(out m_SavedCursorPosition);
#endif
			m_HasCapturedCursor = true;
			m_SuppressLookFrames = 2;
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		private void ShowCursor(bool restorePosition = true)
		{
			if (!m_HasCapturedCursor) return;
			Cursor.lockState = CursorLockMode.None;
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
			if (restorePosition && Application.isFocused && m_HasSavedCursorPosition)
				SetCursorPos(m_SavedCursorPosition.x, m_SavedCursorPosition.y);
			m_HasSavedCursorPosition = false;
#endif
			Cursor.visible = true;
			m_HasCapturedCursor = false;
			m_SuppressLookFrames = 0;
		}

		private Vector2 GetMoveInput(RuntimeInputState inputState, bool forceForward)
		{
			float x = GetAxis(inputState.m_MoveLeft, inputState.m_MoveRight, inputState.m_Move.x);
			bool forward = inputState.m_MoveForward.m_IsPressed || forceForward;
			bool backward = inputState.m_MoveBackward.m_IsPressed;
			float y = GetAxis(forward, backward, inputState.m_Move.y);
			return new Vector2(x, y);
		}

		private float GetAxis(RuntimeInputButton negative, RuntimeInputButton positive, float fallbackValue)
		{
			return GetAxis(positive.m_IsPressed, negative.m_IsPressed, fallbackValue);
		}

		private float GetAxis(bool positive, bool negative, float fallbackValue)
		{
			if (positive && negative)
			{
				return 0f;
			}

			if (positive)
			{
				return 1f;
			}

			if (negative)
			{
				return -1f;
			}

			return fallbackValue;
		}
	}
}

