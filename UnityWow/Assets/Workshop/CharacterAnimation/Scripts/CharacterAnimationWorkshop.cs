using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;

namespace CR
{
    public class CharacterAnimationWorkshop : MonoBehaviour
    {
        [SerializeField] private Animator m_Character;
        [SerializeField] private AnimationClip[] m_Clips;
        [SerializeField] private Camera m_Camera;
        private PlayableGraph m_Graph;
        private AnimationClipPlayable m_Playable;
        private readonly List<int> m_Filtered = new List<int>();
        private readonly List<UnityEngine.UI.Button> m_ClipButtons = new List<UnityEngine.UI.Button>();
        private readonly string[] m_Categories = { "Core 14", "All", "Combat", "Spells", "Movement", "Emotes", "Other" };
        private readonly string[] m_CoreActions = { "Stand", "Stand_V01", "Stand_V02", "Walk", "Run", "Walkbackwards", "ShuffleLeft", "ShuffleRight", "JumpStart", "Jump", "JumpEnd", "JumpLandRun", "Fall", "Sprint" };
        private Font m_Font;
        private UnityEngine.UI.Text m_Title;
        private UnityEngine.UI.Text m_Status;
        private UnityEngine.UI.Text m_PageLabel;
        private UnityEngine.UI.Text m_PlayLabel;
        private UnityEngine.UI.Text m_LoopLabel;
        private UnityEngine.UI.Text m_CategoryLabel;
        private UnityEngine.UI.Slider m_Timeline;
        private string m_Search = "";
        private int m_Category = 1;
        private int m_Selected;
        private int m_Page;
        private float m_Time;
        private float m_Speed = 1f;
        private float m_Yaw;
        private float m_Pitch = 8f;
        private float m_Distance = 3.4f;
        private bool m_IsCameraDragging;
        private Vector2 m_PreviousMousePosition;
        private bool m_Playing = true;
        private bool m_Loop;
        private const int m_PageSize = 11;

        public int ClipCount => m_Clips == null ? 0 : m_Clips.Length;
        public string CurrentClip => ClipCount == 0 ? "" : m_Clips[m_Selected].name;

        public void Configure(Animator character, AnimationClip[] clips, Camera camera)
        {
            m_Character = character;
            m_Clips = clips;
            m_Camera = camera;
        }

        private void Start()
        {
            if (m_Character == null || ClipCount == 0)
            {
                Debug.LogError("Character Animation Workshop requires a character and animation clips.", this);
                enabled = false;
                return;
            }
            m_Character.runtimeAnimatorController = null;
            m_Character.applyRootMotion = false;
            m_Character.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            m_Graph = PlayableGraph.Create("Character Animation Workshop");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            BuildInterface();
            FilterClips();
            int stand = Array.FindIndex(m_Clips, clip => clip.name == "Stand");
            SelectClip(Mathf.Max(0, stand));
            UpdateCamera();
        }

        private void Update()
        {
            UpdateCameraDrag();
            if (!m_Playable.IsValid()) return;
            float duration = m_Clips[m_Selected].length;
            if (m_Playing)
            {
                m_Time += Time.unscaledDeltaTime * m_Speed;
                if (m_Time > duration)
                {
                    if (m_Loop && duration > 0) m_Time %= duration;
                    else { m_Time = duration; m_Playing = false; }
                }
            }
            EvaluatePose();
            m_Timeline.SetValueWithoutNotify(duration > 0 ? m_Time / duration : 0);
            m_Status.text = $"{m_Time:0.00} / {duration:0.00}s    |    {m_Speed:0.00}x    |    30 FPS";
            m_PlayLabel.text = m_Playing ? "Pause" : "Play";
        }

        public void SelectClip(int index)
        {
            if (index < 0 || index >= ClipCount || !m_Graph.IsValid()) return;
            if (m_Playable.IsValid()) m_Graph.DestroyPlayable(m_Playable);
            if (m_Graph.GetOutputCount() > 0) m_Graph.DestroyOutput(m_Graph.GetOutput(0));
            m_Selected = index;
            m_Time = 0;
            m_Playing = true;
            m_Loop = m_Clips[index].isLooping;
            m_Playable = AnimationClipPlayable.Create(m_Graph, m_Clips[index]);
            m_Playable.SetApplyFootIK(false);
            m_Playable.SetApplyPlayableIK(false);
            m_Playable.SetSpeed(0);
            var output = AnimationPlayableOutput.Create(m_Graph, "Character", m_Character);
            output.SetSourcePlayable(m_Playable);
            m_Graph.Play();
            m_Title.text = m_Clips[index].name;
            m_LoopLabel.text = m_Loop ? "Loop: ON" : "Loop: OFF";
            EvaluatePose();
            RefreshPage();
        }

        public void Seek(float normalizedTime)
        {
            if (!m_Playable.IsValid()) return;
            m_Playing = false;
            m_Time = Mathf.Clamp01(normalizedTime) * m_Clips[m_Selected].length;
            EvaluatePose();
        }

        private void EvaluatePose()
        {
            m_Playable.SetTime(m_Time);
            m_Playable.SetDone(false);
            m_Graph.Evaluate(0);
        }

        private void OnDestroy()
        {
            if (m_Graph.IsValid()) m_Graph.Destroy();

        }

        private string GetCategory(string clipName)
        {
            if (clipName.Contains("Spell") || clipName.Contains("Cast")) return "Spells";
            if (clipName.StartsWith("Attack") || clipName.StartsWith("Ready") || clipName.StartsWith("Parry") || clipName.StartsWith("Special") || clipName.StartsWith("Shield") || clipName.StartsWith("Hold") || clipName.StartsWith("Load") || clipName.Contains("Wound") || clipName is "Kick" or "Dodge" or "CombatCritical" or "BattleRoar" or "Whirlwind") return "Combat";
            if (clipName.StartsWith("Emote")) return "Emotes";
            if (clipName.StartsWith("Stand") || clipName.StartsWith("Walk") || clipName.StartsWith("Run") || clipName.StartsWith("Jump") || clipName.StartsWith("Swim") || clipName.StartsWith("Shuffle") || clipName.StartsWith("Stealth") || clipName is "Sprint" or "Fall" or "Stop") return "Movement";
            return "Other";
        }

        private void FilterClips()
        {
            m_Filtered.Clear();
            for (int i = 0; i < ClipCount; i++)
                if (m_Clips[i].name.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0 && (m_Category == 1 || m_Category == 0 && Array.IndexOf(m_CoreActions, m_Clips[i].name) >= 0 || GetCategory(m_Clips[i].name) == m_Categories[m_Category])) m_Filtered.Add(i);
            m_Filtered.Sort(CompareClips);
            m_Page = 0;
            RefreshPage();
        }

        private int CompareClips(int left, int right)
        {
            int leftCore = Array.IndexOf(m_CoreActions, m_Clips[left].name);
            int rightCore = Array.IndexOf(m_CoreActions, m_Clips[right].name);
            int leftOrder = leftCore < 0 ? int.MaxValue : leftCore;
            int rightOrder = rightCore < 0 ? int.MaxValue : rightCore;
            int order = leftOrder.CompareTo(rightOrder);
            return order != 0 ? order : StringComparer.Ordinal.Compare(m_Clips[left].name, m_Clips[right].name);
        }

        private void RefreshPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(m_Filtered.Count / (float)m_PageSize));
            m_Page = Mathf.Clamp(m_Page, 0, pages - 1);
            m_PageLabel.text = $"{m_Filtered.Count} clips    |    {m_Page + 1} / {pages}";
            for (int i = 0; i < m_ClipButtons.Count; i++)
            {
                int position = m_Page * m_PageSize + i;
                var button = m_ClipButtons[i];
                button.gameObject.SetActive(position < m_Filtered.Count);
                if (position >= m_Filtered.Count) continue;
                int clipIndex = m_Filtered[position];
                button.GetComponentInChildren<UnityEngine.UI.Text>().text = m_Clips[clipIndex].name;
                button.image.color = clipIndex == m_Selected ? new Color(0.12f, 0.46f, 0.52f) : new Color(0.13f, 0.18f, 0.23f);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectClip(clipIndex));
            }
        }

        private void UpdateCamera()
        {
            Vector3 center = new Vector3(0, 0.9f, 0);
            m_Camera.transform.position = center + Quaternion.Euler(-m_Pitch, m_Yaw, 0) * new Vector3(0, 0, m_Distance);
            m_Camera.transform.LookAt(center);
        }

        private void UpdateCameraDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused || !mouse.leftButton.isPressed)
            {
                m_IsCameraDragging = false;
                return;
            }
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                m_IsCameraDragging = m_Camera.pixelRect.Contains(position) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                m_PreviousMousePosition = position;
            }
            if (!m_IsCameraDragging) return;
            Vector2 delta = position - m_PreviousMousePosition;
            m_PreviousMousePosition = position;
            m_Yaw += delta.x * 0.25f;
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * 0.2f, -20f, 75f);
            UpdateCamera();
        }

        private RectTransform CreateRect(string objectName, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private UnityEngine.UI.Text CreateText(Transform parent, string value, float x, float y, float width, float height, int size = 18)
        {
            var text = CreateRect("Label", parent, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = m_Font;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.9f, 0.94f, 0.97f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private UnityEngine.UI.Button CreateButton(Transform parent, string value, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            var rect = CreateRect(value, parent, x, y, width, 34);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.13f, 0.18f, 0.23f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var label = CreateText(rect, value, 10, 0, width - 20, 34, 16);
            button.onClick.AddListener(action);
            return button;
        }

        private UnityEngine.UI.Slider CreateSlider(Transform parent, float x, float y, float width, float minimum, float maximum, float value, UnityEngine.Events.UnityAction<float> action)
        {
            var rect = CreateRect("Slider", parent, x, y, width, 24);
            var background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.08f, 0.12f, 0.17f);
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            var handle = CreateRect("Handle", rect, 0, 0, 12, 24);
            handle.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.35f, 0.8f, 0.82f);
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = value;
            slider.onValueChanged.AddListener(action);
            return slider;
        }

        private void BuildInterface()
        {
            m_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("WorkshopInterface", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 1;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("WorkshopEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            var panel = CreateRect("AnimationBrowser", canvasObject.transform, 16, 16, 340, 768);
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.96f);
            CreateText(panel, "WOWGIRL  /  WORKSHOP", 16, 12, 310, 34, 23);
            CreateText(panel, "HumanFemale  -  140 animations", 16, 45, 310, 25, 16);
            var searchRect = CreateRect("Search", panel, 16, 82, 308, 34);
            searchRect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.13f, 0.18f, 0.23f);
            var input = searchRect.gameObject.AddComponent<UnityEngine.UI.InputField>();
            var viewport = CreateRect("Viewport", searchRect, 8, 0, 292, 34);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            input.textComponent = (UnityEngine.UI.Text)CreateText(viewport, "", 0, 0, 292, 34, 16);
            var placeholder = CreateText(viewport, "Search actions...", 0, 0, 292, 34, 16);
            placeholder.color = new Color(0.55f, 0.65f, 0.72f);
            input.placeholder = placeholder;
            input.onValueChanged.AddListener(value => { m_Search = value; FilterClips(); });
            var category = CreateButton(panel, "Category: " + m_Categories[m_Category], 16, 124, 308, () => { m_Category = (m_Category + 1) % m_Categories.Length; m_CategoryLabel.text = "Category: " + m_Categories[m_Category]; FilterClips(); });
            m_CategoryLabel = category.GetComponentInChildren<UnityEngine.UI.Text>();
            for (int i = 0; i < m_PageSize; i++) m_ClipButtons.Add(CreateButton(panel, "Clip", 16, 172 + i * 40, 308, () => { }));
            CreateButton(panel, "Previous", 16, 620, 148, () => { m_Page--; RefreshPage(); });
            CreateButton(panel, "Next", 176, 620, 148, () => { m_Page++; RefreshPage(); });
            m_PageLabel = CreateText(panel, "", 16, 660, 308, 28, 16);
            CreateText(panel, "Select an action, then inspect its pose.\nStatic source poses remain short holds.", 16, 700, 308, 52, 14).horizontalOverflow = HorizontalWrapMode.Wrap;
            var controls = CreateRect("Playback", canvasObject.transform, 380, 632, 870, 152);
            controls.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.96f);
            m_Title = CreateText(controls, "Stand", 16, 4, 400, 35, 24);
            m_Status = CreateText(controls, "", 436, 4, 420, 35, 17);
            m_PlayLabel = CreateButton(controls, "Pause", 16, 46, 90, () => { if (m_Time >= m_Clips[m_Selected].length) m_Time = 0; m_Playing = !m_Playing; }).GetComponentInChildren<UnityEngine.UI.Text>();
            CreateButton(controls, "Restart", 114, 46, 90, () => { m_Time = 0; m_Playing = true; });
            m_LoopLabel = CreateButton(controls, "Loop", 212, 46, 114, () => { m_Loop = !m_Loop; m_LoopLabel.text = m_Loop ? "Loop: ON" : "Loop: OFF"; }).GetComponentInChildren<UnityEngine.UI.Text>();
            CreateText(controls, "Speed", 342, 46, 60, 34, 16);
            CreateSlider(controls, 406, 51, 160, 0.1f, 2f, 1f, value => m_Speed = value).name = "PlaybackSpeed";
            CreateButton(controls, "< View", 582, 46, 90, () => { m_Yaw -= 30; UpdateCamera(); });
            CreateButton(controls, "View >", 680, 46, 90, () => { m_Yaw += 30; UpdateCamera(); });
            CreateButton(controls, "Reset", 778, 46, 76, () => { m_Yaw = 0; m_Pitch = 8; m_Distance = 3.4f; UpdateCamera(); });
            m_Timeline = CreateSlider(controls, 16, 96, 658, 0, 1, 0, value => { m_Playing = false; Seek(value); });
            m_Timeline.name = "PlaybackTimeline";
            CreateText(controls, "Zoom", 694, 92, 54, 34, 16);
            CreateSlider(controls, 750, 97, 104, 2.2f, 6f, 3.4f, value => { m_Distance = value; UpdateCamera(); }).name = "CameraZoom";
            CreateText(canvasObject.transform, "Drag with left mouse to orbit", 396, 20, 460, 30, 17);
        }
    }
}
