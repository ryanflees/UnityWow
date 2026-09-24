// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CR
{
	public class SpellAnimationWorkshop : MonoBehaviour
	{
		public Character m_Character;
		public SpellConfigCollection m_SpellCollection;
		[Min(0f)] public float m_TransitionDuration = 0.12f;
		[Min(0f)] public float m_ReleaseAnimationDuration = 1.1f;
		public bool m_ShowInterface = true;
		public bool m_UseUpperBodyLayer = false;
		public bool m_EnableForwardIk = true;
		public bool m_MoveInPlace;
		public bool m_RepeatInstantSpell;
		[Min(0f)] public float m_MoveSpeed = 2.5f;
		[Min(0f)] public float m_BackwardSpeed = 1.4f;
		private Vector2 m_MoveInput;
		private Vector2 m_SelectedDirection;
		private Quaternion m_ReferenceRotation;
		private Vector3 m_StartPosition;
		private Camera m_PreviewCamera;
		private bool m_HadKeyboardMovement;
		private UnityEngine.UI.Text m_MovementLabel;
		private UnityEngine.UI.Text m_IkLabel;
		private UnityEngine.UI.Text m_TravelLabel;
		private UnityEngine.UI.Text m_UpperBodyPoseLabel;
		private UnityEngine.UI.Text m_RepeatLabel;
		private UnityEngine.UI.Text m_UpperBodyPoseStatus;
		private AnimationClip m_DirectedReleaseClip;
		private AnimationClip m_OmniReleaseClip;
		private int m_LastInstantIndex = 1;
		private float m_RepeatElapsed;
		private readonly UnityEngine.UI.Button[] m_DirectionButtons = new UnityEngine.UI.Button[9];
		private readonly Vector2[] m_Directions =
		{
			new Vector2(-1f, 1f), Vector2.up, Vector2.one,
			Vector2.left, Vector2.zero, Vector2.right,
			new Vector2(-1f, -1f), Vector2.down, new Vector2(1f, -1f)
		};
		private readonly string[] m_DirectionNames =
		{
			"FWD LEFT", "FORWARD", "FWD RIGHT", "LEFT", "STAND", "RIGHT",
			"BACK LEFT", "BACKWARD", "BACK RIGHT"
		};

		public Vector2 MoveInput => m_MoveInput;

		private SpellDefinition m_ActiveSpell;
		private SpellTestPhase m_Phase;
		private float m_PhaseElapsed;
		private float m_PhaseDuration;
		private GameObject m_InterfaceRoot;
		private Text m_ActiveSpellLabel;
		private Text m_PhaseLabel;
		private Text m_ProgressLabel;
		private RectTransform m_ProgressFill;
		private Image m_ProgressFillImage;
		private Font m_Font;
		private UnityEngine.UI.Toggle m_UpperBodyToggle;
		private UnityEngine.UI.Text m_LayerModeLabel;

		public void SetUpperBodyLayerEnabled(bool enabled)
		{
			if (m_UseUpperBodyLayer == enabled) return;
			int activeIndex = m_ActiveSpell == null ? -1 : m_SpellCollection.m_SpellList.IndexOf(m_ActiveSpell);
			m_UseUpperBodyLayer = enabled;
			if (activeIndex >= 0) BeginSpell(activeIndex);
			else StopSpell();
		}

		public void Configure(Character character, SpellConfigCollection spellCollection = null)
		{
			m_Character = character;
			m_SpellCollection = spellCollection;
		}

		private void Start()
		{
			if (m_SpellCollection == null)
			{
				m_SpellCollection = VanillaRuntime.GetConfigCollection<SpellConfigCollection>();
			}

			if (m_Character == null || m_SpellCollection == null || m_SpellCollection.m_SpellList.Count < 6)
			{
				Debug.LogError("Spell Animation Workshop requires a Character and six test spells.", this);
				enabled = false;
			return;
			}

			m_StartPosition = m_Character.transform.position;
			m_ReferenceRotation = m_Character.transform.rotation;
			m_PreviewCamera = Camera.main;
			m_Character.SetLookAtDirection(m_ReferenceRotation * Vector3.forward, Vector3.up);
			CharacterLookAtIk lookAt = m_Character.m_LookAtIk;
			lookAt.m_BaseTransform = m_Character.transform;
			lookAt.m_Head = m_Character.m_Animator.GetBoneTransform(HumanBodyBones.Head);
			lookAt.m_BodyBone = m_Character.m_Animator.GetBoneTransform(HumanBodyBones.Chest);
			lookAt.m_MaxAngle = 90f;
			lookAt.m_EnableBody = true;
			lookAt.m_BodyWeight = 0.35f;
			lookAt.Calibrate();
			foreach (AnimationClip clip in m_Character.m_Animator.runtimeAnimatorController.animationClips)
			{
				if (clip.name == "SpellCastDirected") m_DirectedReleaseClip = clip;
				if (clip.name == "SpellCastOmni") m_OmniReleaseClip = clip;
			}
			BuildInterface();
			m_Character.StopSpellAnimation(0f);
			RefreshInterface();
			Debug.Log("Spell Animation Workshop UI ready with six spell buttons.", this);
		}

		private void Update()
		{
			HandleInput();
			UpdateMovement();
			UpdateActiveSpell();
			RefreshInterface();
		}

		private void HandleInput()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
			{
				return;
			}
			Vector2 input = new Vector2(
				(keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
				(keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
			bool hasMovement = input != Vector2.zero;
			if (hasMovement || m_HadKeyboardMovement) SetMovementDirection(input);
			m_HadKeyboardMovement = hasMovement;
			if (keyboard.spaceKey.wasPressedThisFrame) SetMovementDirection(Vector2.zero);
			if (keyboard.rKey.wasPressedThisFrame) ResetPosition();
			if (keyboard.tKey.wasPressedThisFrame) ToggleUpperBodyPose();

			if (keyboard.f1Key.wasPressedThisFrame)
			{
				m_ShowInterface = !m_ShowInterface;
				m_InterfaceRoot.SetActive(m_ShowInterface);
			}

			if (keyboard.digit1Key.wasPressedThisFrame) BeginSpell(0);
			if (keyboard.digit2Key.wasPressedThisFrame) BeginSpell(1);
			if (keyboard.digit3Key.wasPressedThisFrame) BeginSpell(2);
			if (keyboard.digit4Key.wasPressedThisFrame) BeginSpell(3);
			if (keyboard.digit5Key.wasPressedThisFrame) BeginSpell(4);
			if (keyboard.digit6Key.wasPressedThisFrame) BeginSpell(5);
			if (keyboard.digit0Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) StopSpell();
		}

		public void SetMovementDirection(Vector2 direction)
		{
			m_SelectedDirection = new Vector2(Mathf.Abs(direction.x) < 0.5f ? 0f : Mathf.Sign(direction.x),
				Mathf.Abs(direction.y) < 0.5f ? 0f : Mathf.Sign(direction.y));
			if (m_SelectedDirection != Vector2.zero && m_ActiveSpell != null &&
				m_ActiveSpell.m_Cast.m_Type != SpellCastType.Instant) StopSpell();
			ApplyLocomotion();
		}

		private void ApplyLocomotion()
		{
			m_MoveInput = m_SelectedDirection;
			if (m_MoveInput == Vector2.zero)
			{
				if (m_Phase == SpellTestPhase.Idle || m_Phase == SpellTestPhase.Release) m_Character.PlayStand();
			}
			else if (m_MoveInput.y < 0f) m_Character.PlayMoveBackward();
			else m_Character.PlayMoveForward();
		}

		private void UpdateMovement()
		{
			ApplyLocomotion();
			Vector3 direction = new Vector3(m_MoveInput.x, 0f, m_MoveInput.y).normalized;
			Vector3 facing = m_MoveInput.y < 0f ? -direction : direction;
			Quaternion rotation = facing == Vector3.zero ? m_ReferenceRotation :
				m_ReferenceRotation * Quaternion.LookRotation(facing, Vector3.up);
			m_Character.transform.rotation = Quaternion.RotateTowards(m_Character.transform.rotation, rotation, 540f * Time.deltaTime);
			if (!m_MoveInPlace)
			{
				float speed = m_MoveInput.y < 0f ? m_BackwardSpeed : m_MoveSpeed;
				Vector3 delta = m_ReferenceRotation * direction * (speed * Time.deltaTime);
				Vector3 position = m_Character.transform.position + delta;
				position.x = Mathf.Clamp(position.x, m_StartPosition.x - 7f, m_StartPosition.x + 7f);
				position.z = Mathf.Clamp(position.z, m_StartPosition.z - 7f, m_StartPosition.z + 7f);
				MovePreview(position - m_Character.transform.position);
			}
			m_Character.m_LookAtIk.m_Enable = m_EnableForwardIk;
			m_Character.SetLookAtDirection(m_ReferenceRotation * Vector3.forward, Vector3.up);
			m_Character.m_LookAtIk.SetUpperBodyReferenceRotation(m_ReferenceRotation);
			if (m_Phase == SpellTestPhase.Release && m_MoveInput != Vector2.zero)
				m_Character.m_Animator.SetLayerWeight(1, 1f);
		}

		public void ResetPosition()
		{
			SetMovementDirection(Vector2.zero);
			MovePreview(m_StartPosition - m_Character.transform.position);
			m_Character.transform.rotation = m_ReferenceRotation;
		}

		private void MovePreview(Vector3 delta)
		{
			m_Character.transform.position += delta;
			if (m_PreviewCamera != null) m_PreviewCamera.transform.position += delta;
		}

		private void UpdateActiveSpell()
		{
			if (m_Phase == SpellTestPhase.Idle)
			{
				if (m_RepeatInstantSpell)
				{
					m_RepeatElapsed += Time.deltaTime;
					if (m_RepeatElapsed >= 0.35f) BeginSpell(m_LastInstantIndex);
				}
				return;
			}

			m_PhaseElapsed += Time.deltaTime;
			if (m_PhaseElapsed < m_PhaseDuration)
			{
				return;
			}

			if (m_Phase == SpellTestPhase.Casting)
			{
				m_Character.PlaySpellAnimation(m_ActiveSpell.m_Presentation.m_AnimationType, true,
					m_TransitionDuration, m_UseUpperBodyLayer);
				StartUpperBodyPose(m_ActiveSpell.m_Presentation.m_AnimationType, m_UseUpperBodyLayer ? 1 : 0);
				m_Phase = SpellTestPhase.Release;
				m_PhaseElapsed = 0f;
				m_PhaseDuration = m_ReleaseAnimationDuration;
				return;
			}

			EndSpell();
		}

		public void BeginSpell(int index)
		{
			if (index < 0 || index >= m_SpellCollection.m_SpellList.Count)
			{
				return;
			}

			EndSpell();
			m_ActiveSpell = m_SpellCollection.m_SpellList[index];
			SpellCastData cast = m_ActiveSpell.m_Cast;
			SpellAnimationType animationType = m_ActiveSpell.m_Presentation.m_AnimationType;
			if (cast.m_Type != SpellCastType.Instant)
			{
				m_RepeatInstantSpell = false;
				m_SelectedDirection = Vector2.zero;
				ApplyLocomotion();
			}

			switch (cast.m_Type)
			{
				case SpellCastType.Instant:
					m_LastInstantIndex = index;
					m_Character.PlaySpellAnimation(animationType, true, m_TransitionDuration, true);
					StartUpperBodyPose(animationType, 1);
					if (m_MoveInput == Vector2.zero && !m_UseUpperBodyLayer)
						m_Character.m_Animator.SetLayerWeight(1, 0f);
					m_Phase = SpellTestPhase.Release;
					m_PhaseDuration = m_ReleaseAnimationDuration;
					break;
				case SpellCastType.CastTime:
					m_Character.PlaySpellAnimation(animationType, false, m_TransitionDuration, m_UseUpperBodyLayer);
					m_Phase = SpellTestPhase.Casting;
					m_PhaseDuration = cast.m_CastTime;
					break;
				case SpellCastType.Channeled:
					m_Character.PlaySpellAnimation(animationType, false, m_TransitionDuration, m_UseUpperBodyLayer);
					m_Phase = SpellTestPhase.Channeling;
					m_PhaseDuration = cast.m_ChannelDuration;
					break;
			}

			m_PhaseElapsed = 0f;
			RefreshInterface();
		}

		public void StopSpell()
		{
			m_RepeatInstantSpell = false;
			EndSpell();
		}

		private void StartUpperBodyPose(SpellAnimationType animationType, int sourceLayer)
		{
			bool directed = animationType == SpellAnimationType.CastDirected ||
				animationType == SpellAnimationType.ChannelDirected;
			AnimationClip clip = directed ? m_DirectedReleaseClip : m_OmniReleaseClip;
			int stateHash = Animator.StringToHash(directed ? "CastSpellDirectedFinish" : "CastSpellOmniFinish");
			if (!m_Character.m_LookAtIk.PlayUpperBodyPose(clip, stateHash, sourceLayer, m_ReferenceRotation))
				Debug.LogWarning("Upper-body stabilization requires a valid Humanoid release clip and chest bone.", this);
		}

		private void ToggleUpperBodyPose()
		{
			m_Character.m_LookAtIk.m_EnableUpperBodyStabilization = !m_Character.m_LookAtIk.m_EnableUpperBodyStabilization;
		}

		private void EndSpell()
		{
			if (m_Character != null && m_Character.m_LookAtIk != null) m_Character.m_LookAtIk.StopUpperBodyPose();
			if (m_Character != null)
			{
				m_Character.StopSpellAnimation(m_TransitionDuration);
			}

			m_ActiveSpell = null;
			m_Phase = SpellTestPhase.Idle;
			m_PhaseElapsed = 0f;
			m_PhaseDuration = 0f;
			m_RepeatElapsed = 0f;
			RefreshInterface();
		}

		private void RefreshInterface()
		{
			if (m_ActiveSpellLabel == null)
			{
				return;
			}

			m_ActiveSpellLabel.text = m_ActiveSpell == null
				? "ACTIVE SPELL  /  None"
				: $"ACTIVE SPELL  /  {m_ActiveSpell.m_DisplayName}";
			m_PhaseLabel.text = $"PHASE  /  {m_Phase}";
			m_UpperBodyToggle.SetIsOnWithoutNotify(m_UseUpperBodyLayer);
			m_LayerModeLabel.text = m_UseUpperBodyLayer
				? "Base + UpperBody (release)"
				: "Full body / Base only";
			if (m_MoveInput != Vector2.zero) m_LayerModeLabel.text = "Moving: UpperBody automatic";
			for (int i = 0; i < m_Directions.Length; i++)
			{
				bool selected = m_Directions[i] == m_MoveInput;
				m_DirectionButtons[i].GetComponent<UnityEngine.UI.Image>().color = selected
					? new Color(0.12f, 0.55f, 0.7f) : new Color(0.1f, 0.18f, 0.25f);
				if (selected) m_MovementLabel.text = "MOVEMENT / " + m_DirectionNames[i];
			}
			m_IkLabel.text = "LOOK AT / " + (m_EnableForwardIk ? "ON" : "OFF");
			m_TravelLabel.text = m_MoveInPlace ? "TRAVEL / IN PLACE" : "TRAVEL / MOVE";
			m_UpperBodyPoseLabel.text = "T  UPPER BODY / " + (m_Character.m_LookAtIk.m_EnableUpperBodyStabilization ? "ON" : "OFF");
			m_RepeatLabel.text = "REPEAT INSTANT / " + (m_RepeatInstantSpell ? "ON" : "OFF");
			m_UpperBodyPoseStatus.text = $"Weight: {m_Character.m_LookAtIk.UpperBodyPoseWeight:0.00} | Pose offset: {m_Character.m_LookAtIk.UpperBodyRotationError:0.0} deg" +
				(m_Character.m_LookAtIk.IsUpperBodyTwistLimited ? " | LIMIT" : "");

			float progress = m_PhaseDuration <= 0f ? 0f : Mathf.Clamp01(m_PhaseElapsed / m_PhaseDuration);
			m_ProgressFill.anchorMax = new Vector2(progress, 1f);
			m_ProgressFillImage.color = m_Phase == SpellTestPhase.Channeling
				? new Color(0.62f, 0.38f, 0.95f)
				: new Color(0.2f, 0.68f, 1f);
			m_ProgressLabel.text = m_Phase == SpellTestPhase.Idle
				? "READY"
				: $"{m_PhaseElapsed:0.0} / {m_PhaseDuration:0.0}s";
		}

		private void BuildInterface()
		{
			m_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			GameObject canvasObject = new GameObject("SpellWorkshopInterface", typeof(Canvas),
				typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);
			Canvas canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280f, 720f);
			scaler.matchWidthOrHeight = 0.5f;

			if (FindFirstObjectByType<EventSystem>() == null)
			{
				GameObject eventSystem = new GameObject("SpellWorkshopEventSystem", typeof(EventSystem),
					typeof(InputSystemUIInputModule));
				eventSystem.transform.SetParent(transform, false);
			}

			RectTransform reminder = CreateRect("TimingReminder", canvasObject.transform,
				new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
				new Vector2(220f, -18f), new Vector2(820f, 48f));
			reminder.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.94f);
			Text reminderText = CreateText(reminder,
				"INSTANT: release now    |    CAST: 2.5s then release    |    CHANNEL: maximum 3.0s",
				new Vector2(14f, -4f), new Vector2(792f, 40f), 17, TextAnchor.MiddleCenter);
			reminderText.color = new Color(0.78f, 0.88f, 1f);

			RectTransform panel = CreateRect("SpellControls", canvasObject.transform,
				new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
				new Vector2(18f, -82f), new Vector2(420f, 590f));
			panel.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.96f);
			CreateText(panel, "SPELL ANIMATION WORKSHOP", new Vector2(20f, -14f),
				new Vector2(380f, 34f), 23, TextAnchor.MiddleLeft);
			CreateLayerToggle(panel);

			CreateSpellButton(panel, 0, "1  INSTANT DIRECTED", "Immediate directed release", 88f,
				new Color(0.1f, 0.35f, 0.52f));
			CreateSpellButton(panel, 1, "2  INSTANT OMNI", "Immediate omnidirectional release", 148f,
				new Color(0.1f, 0.35f, 0.52f));
			CreateSpellButton(panel, 2, "3  CAST DIRECTED", "2.5s preparation, then directed release", 218f,
				new Color(0.15f, 0.3f, 0.58f));
			CreateSpellButton(panel, 3, "4  CAST OMNI", "2.5s preparation, then omni release", 278f,
				new Color(0.15f, 0.3f, 0.58f));
			CreateSpellButton(panel, 4, "5  CHANNEL DIRECTED", "Directed channel, stops after 3.0s", 348f,
				new Color(0.33f, 0.2f, 0.55f));
			CreateSpellButton(panel, 5, "6  CHANNEL OMNI", "Omni channel, stops after 3.0s", 408f,
				new Color(0.33f, 0.2f, 0.55f));

			m_ActiveSpellLabel = CreateText(panel, "ACTIVE SPELL  /  None", new Vector2(20f, -472f),
				new Vector2(380f, 24f), 16, TextAnchor.MiddleLeft);
			m_PhaseLabel = CreateText(panel, "PHASE  /  Idle", new Vector2(20f, -498f),
				new Vector2(380f, 24f), 15, TextAnchor.MiddleLeft);
			CreateProgressBar(panel, 20f, 528f, 380f);
			CreateButton(panel, "0 / ESC  CANCEL", new Vector2(20f, -558f), new Vector2(180f, 28f),
				new Color(0.52f, 0.16f, 0.18f), StopSpell);
			Text footer = CreateText(panel, "F1 toggles this UI", new Vector2(218f, -558f),
				new Vector2(182f, 28f), 14, TextAnchor.MiddleRight);
			footer.color = new Color(0.58f, 0.68f, 0.78f);

			BuildMovementInterface(canvasObject.transform);
			m_InterfaceRoot = canvasObject;
			m_InterfaceRoot.SetActive(m_ShowInterface);
		}

		private void BuildMovementInterface(Transform parent)
		{
			RectTransform panel = CreateRect("MovementControls", parent, Vector2.one, Vector2.one,
				Vector2.one, new Vector2(-18f, -82f), new Vector2(330f, 452f));
			panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.96f);
			CreateText(panel, "8-WAY MOVEMENT + INSTANT", new Vector2(14f, -12f), new Vector2(302f, 30f), 19, TextAnchor.MiddleLeft);
			CreateText(panel, "WASD: hold  |  Buttons: continuous", new Vector2(14f, -44f), new Vector2(302f, 24f), 14, TextAnchor.MiddleLeft);
			for (int i = 0; i < m_Directions.Length; i++)
			{
				int index = i;
				m_DirectionButtons[i] = CreateButton(panel, m_DirectionNames[i],
					new Vector2(14f + i % 3 * 102f, -78f - i / 3 * 44f), new Vector2(98f, 40f),
					new Color(0.1f, 0.18f, 0.25f), () => SetMovementDirection(m_Directions[index]));
			}
			m_MovementLabel = CreateText(panel, "", new Vector2(14f, -212f), new Vector2(302f, 24f), 15, TextAnchor.MiddleLeft);
			UnityEngine.UI.Button ikButton = CreateButton(panel, "", new Vector2(14f, -244f), new Vector2(148f, 34f),
				new Color(0.1f, 0.3f, 0.4f), () => m_EnableForwardIk = !m_EnableForwardIk);
			m_IkLabel = ikButton.GetComponentInChildren<UnityEngine.UI.Text>();
			UnityEngine.UI.Button travelButton = CreateButton(panel, "", new Vector2(168f, -244f), new Vector2(148f, 34f),
				new Color(0.1f, 0.3f, 0.4f), () => m_MoveInPlace = !m_MoveInPlace);
			m_TravelLabel = travelButton.GetComponentInChildren<UnityEngine.UI.Text>();
			CreateButton(panel, "R  RESET POSITION", new Vector2(14f, -288f), new Vector2(148f, 36f),
				new Color(0.25f, 0.25f, 0.35f), ResetPosition);
			CreateText(panel, "SPACE: stop\n1 / 2: instant spells", new Vector2(174f, -284f), new Vector2(142f, 48f), 14, TextAnchor.MiddleLeft);
			UnityEngine.UI.Button upperBodyButton = CreateButton(panel, "", new Vector2(14f, -332f), new Vector2(302f, 34f),
				new Color(0.14f, 0.38f, 0.32f), ToggleUpperBodyPose);
			upperBodyButton.name = "ToggleUpperBodyPose";
			m_UpperBodyPoseLabel = upperBodyButton.GetComponentInChildren<UnityEngine.UI.Text>();
			UnityEngine.UI.Button repeatButton = CreateButton(panel, "", new Vector2(14f, -374f), new Vector2(302f, 34f),
				new Color(0.2f, 0.27f, 0.4f), () => m_RepeatInstantSpell = !m_RepeatInstantSpell);
			repeatButton.name = "ToggleRepeatInstantSpell";
			m_RepeatLabel = repeatButton.GetComponentInChildren<UnityEngine.UI.Text>();
			m_UpperBodyPoseStatus = CreateText(panel, "", new Vector2(14f, -414f), new Vector2(302f, 26f), 12, TextAnchor.MiddleLeft);
		}

		private void CreateLayerToggle(Transform parent)
		{
			RectTransform rect = CreateRect("UpperBodyLayerToggle", parent, Vector2.up, Vector2.up,
				Vector2.up, new Vector2(20f, -50f), new Vector2(380f, 30f));
			UnityEngine.UI.Image background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
			background.color = new Color(0.08f, 0.16f, 0.23f);
			RectTransform box = CreateRect("Checkbox", rect, Vector2.up, Vector2.up, Vector2.up,
				new Vector2(6f, -4f), new Vector2(22f, 22f));
			box.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.28f, 0.36f, 0.44f);
			RectTransform check = CreateRect("Checked", box, Vector2.up, Vector2.up, Vector2.up,
				new Vector2(4f, -4f), new Vector2(14f, 14f));
			UnityEngine.UI.Image checkImage = check.gameObject.AddComponent<UnityEngine.UI.Image>();
			checkImage.color = new Color(0.2f, 0.8f, 1f);
			checkImage.raycastTarget = false;
			m_LayerModeLabel = CreateText(rect, "", new Vector2(38f, 0f), new Vector2(334f, 30f),
				16, TextAnchor.MiddleLeft);
			m_UpperBodyToggle = rect.gameObject.AddComponent<UnityEngine.UI.Toggle>();
			m_UpperBodyToggle.targetGraphic = background;
			m_UpperBodyToggle.graphic = checkImage;
			m_UpperBodyToggle.SetIsOnWithoutNotify(m_UseUpperBodyLayer);
			m_UpperBodyToggle.onValueChanged.AddListener(SetUpperBodyLayerEnabled);
		}

		private void CreateSpellButton(Transform parent, int index, string title, string description,
			float y, Color color)
		{
			CreateButton(parent, title, new Vector2(20f, -y), new Vector2(190f, 48f), color,
				() => BeginSpell(index));
			Text descriptionLabel = CreateText(parent, description, new Vector2(220f, -y),
				new Vector2(180f, 48f), 13, TextAnchor.MiddleLeft);
			descriptionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
			descriptionLabel.verticalOverflow = VerticalWrapMode.Truncate;
			descriptionLabel.color = new Color(0.68f, 0.74f, 0.8f);
		}

		private void CreateProgressBar(Transform parent, float x, float y, float width)
		{
			RectTransform background = CreateRect("Progress", parent, Vector2.up, Vector2.up, Vector2.up,
				new Vector2(x, -y), new Vector2(width, 24f));
			background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.17f);
			m_ProgressFill = CreateRect("Fill", background, Vector2.zero, Vector2.up, new Vector2(0f, 0.5f),
				Vector2.zero, Vector2.zero);
			m_ProgressFillImage = m_ProgressFill.gameObject.AddComponent<Image>();
			m_ProgressLabel = CreateText(background, "READY", Vector2.zero, new Vector2(width, 24f),
				14, TextAnchor.MiddleCenter);
		}

		private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size,
			Color color, UnityEngine.Events.UnityAction action)
		{
			RectTransform rect = CreateRect(label, parent, Vector2.up, Vector2.up, Vector2.up, position, size);
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			Button button = rect.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			ColorBlock colors = button.colors;
			colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
			colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
			button.colors = colors;
			button.onClick.AddListener(action);
			CreateText(rect, label, Vector2.zero, size, 14, TextAnchor.MiddleCenter);
			return button;
		}

		private Text CreateText(Transform parent, string value, Vector2 position, Vector2 size,
			int fontSize, TextAnchor alignment)
		{
			RectTransform rect = CreateRect("Label", parent, Vector2.up, Vector2.up, Vector2.up, position, size);
			Text text = rect.gameObject.AddComponent<Text>();
			text.font = m_Font;
			text.text = value;
			text.fontSize = fontSize;
			text.color = new Color(0.92f, 0.95f, 0.98f);
			text.alignment = alignment;
			text.raycastTarget = false;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			text.verticalOverflow = VerticalWrapMode.Truncate;
			return text;
		}

		private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin,
			Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
		{
			RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
			return rect;
		}

		private enum SpellTestPhase
		{
			Idle,
			Casting,
			Release,
			Channeling
		}
	}
}

