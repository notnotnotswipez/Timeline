using System;
using System.Collections.Generic;
using Il2CppSystem;
using Il2CppTMPro;
using MelonLoader;
using Timeline.Audio;
using Timeline.BuiltInKeyframes;
using Timeline.CameraRelated;
using Timeline.Logging;
using Timeline.Serialization;
using Timeline.Serialization.Binary;
using Timeline.Serialization.Registry;
using Timeline.Settings;
using Timeline.Settings.Capture;
using Timeline.Settings.Menu;
using Timeline.Settings.Panels;
using Timeline.Utils;
using Timeline.WorldRecording;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Action = System.Action;
using Activator = System.Activator;
using Console = System.Console;
using Math = System.Math;
using TimeSpan = System.TimeSpan;

namespace Timeline
{
    public enum TimelineSelectedTab {
        PROPERTIES,
        LOAD
    }

    public class TimelineHolder : SerializableMember
    {
        public SortedList<float, Keyframe> keyframes = new SortedList<float, Keyframe>();
        public SortedList<float, System.Tuple<SettingsPanel, SettingsPanelCapture>> settingsKeyframes = new SortedList<float, System.Tuple<SettingsPanel, SettingsPanelCapture>>();
        public List<GameObject> keyframesOnDisplay = new List<GameObject>();
        public GameObject mainUi;
        public GameObject uiHolderEventSystem;
        
        public float playhead;
        public float length = 10;
        public CameraController controller;
        public bool playing = false;

        private float minKeyframeX = -542.7f;
        private float maxKeyframeX = 557f;
        private float keyframeY = 418f;
        private GameObject settingsPanel;
        private GameObject keyframePanel;
        
        TMP_Text _timeDisplay;
        
        public bool isHiddenCompletely = true;
        public bool isUiHidden = false;
        
        public EventSystem eventSystem;
        public EventSystem previousEventSystem;
        
        public Slider _slider;
        
        public Keyframe selectedKeyframe;
        public static bool useWorldScene = true;

        public List<SettingsPanel> settingPanels = new List<SettingsPanel>();
        public int settingIndex = 0;

        public GameObject extraSettingsPanel;
        public GameObject backPanelArrow;
        public GameObject nextPanelArrow;
        public GameObject contentRoot;
        public TMP_Text panelTitle;

        public GameObject pullOutButton;
        public GameObject pullInButton;

        public WorldPlayer worldPlayer;

        public TimelineSelectedTab selectedTab;

        public GameObject propertiesTabObject;
        public GameObject loadTabObject;

        public GameObject selectedTabSelector;
        public TMP_Dropdown loadSceneDropdown;
        public TMP_InputField nameField;

        public TMP_Text propertiesTabText;
        public TMP_Text loadTabText;

        public float propertiesTabX = 6.1f;
        public float loadTabX = 132.7f;

        public string selectedSessionName = "default";

        public int sessionVersion = SaveManager.version;

        public override byte SerializeableID => (byte) TimelineSerializedTypes.TIMELINE_HOLDER;

        public TimelineHolder(CameraController controller)
        {
            this.controller = controller;
            controller.holder = this;
        }

        public void RegisterSettings()
        {
            // TODO: Register camera settings here
            RegisterSetting<CameraPanel>();
            RegisterSetting<GreenScreenPanel>();
            //RegisterSetting<DepthOfFieldPanel>();
        }

        private void RegisterSetting<T>() where T : SettingsPanel
        {
            SettingsPanel panel = Activator.CreateInstance<T>();
            panel.InitializeMenu();
            settingPanels.Add(panel);
        }

        public T GetSettingPanelInstance<T>() where T : SettingsPanel {
            foreach (var settingPanel in settingPanels) {
                if (settingPanel is T) {
                    return (T) settingPanel;
                }
            }

            return null;
        }

        private void ShowPanel(int index)
        {
            if (index < 0)
            {
                index = settingPanels.Count - 1;
            }
            else if (index >= settingPanels.Count)
            {
                index = 0;
            }
            
            backPanelArrow.SetActive(true);
            nextPanelArrow.SetActive(true);

            if (index == 0)
            {
                backPanelArrow.SetActive(false);
            }
            
            if (index == settingPanels.Count - 1)
            {
                nextPanelArrow.SetActive(false);
            }
            
            if (index > 0)
            {
                backPanelArrow.SetActive(true);
            }
            
            // Delete all children in content root
            int childCount = contentRoot.transform.childCount;
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < childCount; i++)
            {
                toDestroy.Add(contentRoot.transform.GetChild(i).gameObject);
            }
            
            foreach (var gameObject in toDestroy)
            {
                GameObject.Destroy(gameObject);
            }

            SettingsPanel panel = settingPanels[index];
            
            panelTitle.text = panel.Name;
            panel.CreateMenu(contentRoot.transform);
        }

        public void HideAll(bool toHide)
        {
            if (!previousEventSystem)
            {
                previousEventSystem = EventSystem.current;
            }

            controller.Validate();
            controller.gameObject.SetActive(!toHide);
            uiHolderEventSystem.SetActive(!toHide);
            EventSystem.current = toHide ? previousEventSystem : eventSystem;
            controller.LockMouse(!toHide);
            playing = false;
            
            // Animation controller is on the event system object, we have to reset all the stuff moved by animations when the menu is hidden
            if (toHide)
            {
                ResetMenu();
            }
            
            isHiddenCompletely = toHide;
        }
        
        public void HideUI(bool toHide)
        {
            if (toHide) {
                UnSelectAllSelectedButtons();
            }
            //EventSystem.current = toHide ? previousEventSystem : eventSystem;
            mainUi.SetActive(!toHide);
            if (!toHide)
            {
                if (!playing)
                {
                    _slider.interactable = true;
                }

                if (controller.retainControl) {
                    // Free mouse as the user no longer should have total cam control while they previously had it
                    controller.retainControl = false;
                    controller.LockMouse(false);
                }
                
            }

            isUiHidden = toHide;
        }

        public void StartUI()
        {
            worldPlayer = new WorldPlayer();
            uiHolderEventSystem = GameObject.Instantiate(TimelineAssets.timelineUi);
            EventSystem eventSystem = uiHolderEventSystem.GetComponent<EventSystem>();
            StandaloneInputModule standaloneInputModule = uiHolderEventSystem.GetComponent<StandaloneInputModule>();
            this.eventSystem = eventSystem;
            eventSystem.m_CurrentInputModule = standaloneInputModule;
            mainUi = uiHolderEventSystem.transform.Find("TimelineMainUI").gameObject;
            _slider = mainUi.transform.Find("Slider").GetComponent<Slider>();
            _timeDisplay = mainUi.transform.Find("Slider").Find("Handle Slide Area").Find("Handle").Find("Timer").Find("TimeText").GetComponent<TMP_Text>();
            _slider.interactable = true;
            _slider.onValueChanged.AddListener(new System.Action<float>((floatVal) =>
            {
                if (_slider.interactable)
                {
                    playhead = floatVal;
              
                    UpdateCameraKeyframeState(playhead);
                    worldPlayer.UpdateScene(playhead);

                }
            }));
            settingsPanel = mainUi.transform.Find("SettingsPanel").gameObject;

            selectedTabSelector = settingsPanel.transform.Find("TabSelector").gameObject;

            settingsPanel.transform.Find("SelectLoadTabButton").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                selectedTab = TimelineSelectedTab.LOAD;
                UpdateSettingsPanel();
            }));

            settingsPanel.transform.Find("SelectPropertiesTabButton").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                selectedTab = TimelineSelectedTab.PROPERTIES;
                UpdateSettingsPanel();
            }));

            propertiesTabObject = settingsPanel.transform.Find("PropertiesTab").gameObject;
            loadTabObject = settingsPanel.transform.Find("LoadTab").gameObject;

            propertiesTabText = settingsPanel.transform.Find("PropertiesText").GetComponent<TMP_Text>();
            loadTabText = settingsPanel.transform.Find("LoadText").GetComponent<TMP_Text>();

            loadSceneDropdown = loadTabObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();

            loadTabObject.transform.Find("LoadButton").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                string target = loadSceneDropdown.options[loadSceneDropdown.value].text;

                TimelineLogger.Msg("LOADING target file: " + target);
                SaveManager.LoadFromFile(target);
                selectedSessionName = target;
            }));

            propertiesTabObject.transform.Find("ClearButton").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                keyframes.Clear();
                worldPlayer.RemoveAllRecorders();
                UpdateKeyframeDisplays();
            }));

            propertiesTabObject.transform.Find("SaveButton").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                SaveManager.SaveToFile(selectedSessionName);
            }));

            nameField = propertiesTabObject.transform.Find("NameField").GetComponent<TMP_InputField>();

            nameField.onValueChanged.AddListener(new System.Action<string>((s) =>
            {
                selectedSessionName = s;
            }));

            extraSettingsPanel = mainUi.transform.Find("ExtraCamSettings").gameObject;
            contentRoot = extraSettingsPanel.transform.Find("Controls").Find("ScrollView").Find("View").Find("Content").gameObject;
            panelTitle = extraSettingsPanel.transform.Find("SettingsTitle").GetComponent<TMP_Text>();
            
            backPanelArrow = extraSettingsPanel.transform.Find("BackSetting").gameObject;
            nextPanelArrow = extraSettingsPanel.transform.Find("NextSetting").gameObject;
            
            pullInButton = extraSettingsPanel.transform.Find("TabButtons").Find("PullInButton").gameObject;
            pullOutButton = extraSettingsPanel.transform.Find("TabButtons").Find("PullOutButton").gameObject;
            
            Button backButton = backPanelArrow.GetComponent<Button>();
            backButton.onClick.AddListener(new Action(() =>
            {
                settingIndex--;
                ShowPanel(settingIndex);
            }));
            
            Button nextButton = nextPanelArrow.GetComponent<Button>();
            nextButton.onClick.AddListener(new Action(() =>
            {
                settingIndex++;
                ShowPanel(settingIndex);
            }));
            
            
            settingsPanel.SetActive(false);
            keyframePanel = mainUi.transform.Find("KeyframeTypeList").gameObject;
            
            // This is hardcoded, yucky, but I wont change it until I add more keyframe types
            keyframePanel.transform.Find("ScrollView").Find("Content").Find("InterpKeyframe").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                MakeKeyframeAtPlayhead(KeyframeTypes.LINEAR);
            }));
            keyframePanel.transform.Find("ScrollView").Find("Content").Find("InstantKeyframe").GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                MakeKeyframeAtPlayhead(KeyframeTypes.INSTANT);
            }));
            
            keyframePanel.SetActive(false);

            Button keyFrameAddButton = mainUi.transform.Find("KeyframeAddButton").GetComponent<Button>();
            keyFrameAddButton.onClick.AddListener(new Action(() =>
            {
                keyframePanel.SetActive(!keyframePanel.activeSelf);
            }));

            Navigation noAutomaticNav = Navigation.defaultNavigation;
            noAutomaticNav.mode = Navigation.Mode.None;

            keyFrameAddButton.navigation = noAutomaticNav;

            Button settingsButtonPressable = mainUi.transform.Find("SettingsButton").GetComponent<Button>();
            settingsButtonPressable.onClick.AddListener(new Action(() =>
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }));

            settingsButtonPressable.navigation = noAutomaticNav;

            uiHolderEventSystem.SetActive(false);
            
            RegisterSettings();

            selectedTab = TimelineSelectedTab.PROPERTIES;
            UpdateSettingsPanel();

            ShowPanel(0);
        }

        private void PopulateLoadDropdownOptions() {
            loadSceneDropdown.options.Clear();

            foreach (string loadableFile in SaveManager.ALL_TIMELINE_SAVES_LIST) {
                loadSceneDropdown.options.Add(new TMP_Dropdown.OptionData(loadableFile.Replace(SaveManager.TIMELINE_SAVE_EXTENSION, "")));
            }
        }

        private void UpdateSettingsPanel() {
            RectTransform rectTransform = selectedTabSelector.GetComponent<RectTransform>();
            Vector3 pos = rectTransform.localPosition;

            switch (selectedTab) {
                case TimelineSelectedTab.PROPERTIES:
                    propertiesTabObject.SetActive(true);
                    loadTabObject.SetActive(false);

                    pos.x = propertiesTabX;

                    rectTransform.localPosition = pos;

                    loadTabText.color = Color.gray;
                    propertiesTabText.color = Color.white;

                    nameField.text = selectedSessionName;
                    break;
                case TimelineSelectedTab.LOAD:
                    propertiesTabObject.SetActive(false);
                    loadTabObject.SetActive(true);

                    pos.x = loadTabX;

                    rectTransform.localPosition = pos;

                    loadTabText.color = Color.white;
                    propertiesTabText.color = Color.gray;

                    PopulateLoadDropdownOptions();
                    break;
            }
        }

        public void ResetMenu()
        {
            extraSettingsPanel.transform.GetComponent<RectTransform>().localPosition = new Vector3(1160, -65.8f, 0);
            pullInButton.SetActive(false);
            pullOutButton.SetActive(true);
        }

        public void UpdateSlider()
        {
            _slider.maxValue = length;
            _slider.value = playhead;
            UpdateKeyframeDisplays();
        }

        public void UpdateKeyframeDisplays()
        {
            foreach (var displayKeyframe in keyframesOnDisplay)
            {
                if (displayKeyframe)
                {
                    GameObject.Destroy(displayKeyframe);
                }
            }
            keyframesOnDisplay.Clear();
            
            foreach (var existingKeyframe in keyframes)
            {
                GameObject keyframeDisplay = GameObject.Instantiate(TimelineAssets.keyframeGenericHolder);
                Button button = keyframeDisplay.GetComponentInChildren<Button>();
                button.onClick.AddListener(new Action(() =>
                {
                    selectedKeyframe = existingKeyframe.Value;
                    foreach (var keyframe in keyframes.Values)

                    {
                        keyframe.selected = false;
                    }
                    selectedKeyframe.selected = true;

                    UpdateKeyframeDisplays();

                    // Jump to keyframe
                    if (Input.GetKey(KeyCode.LeftShift)) {
                        playhead = existingKeyframe.Key;
                        _slider.value = playhead;
                    }
                }));
                GameObject selected = keyframeDisplay.transform.Find("SelectedBorder").gameObject;
                selected.SetActive(existingKeyframe.Value.selected);
                RawImage rawImage = keyframeDisplay.GetComponent<RawImage>();
                rawImage.texture = existingKeyframe.Value.texture;
                keyframeDisplay.transform.parent = mainUi.transform;
                RectTransform rectTransform = keyframeDisplay.transform.GetComponent<RectTransform>();
                float betweenValue = Mathf.InverseLerp(0, length, existingKeyframe.Key);
                float xValue = Mathf.Lerp(minKeyframeX, maxKeyframeX, betweenValue);
                rectTransform.localPosition = new Vector3(xValue, keyframeY, 0);

                keyframesOnDisplay.Add(keyframeDisplay);

                TimelineLogger.Debug("Made physical keyframe button");
            }
        }

        public void Play()
        {
            _slider.interactable = false;
            playing = true;

            bool previouslyHidenState = isUiHidden;

            controller.LockMouse(true);
            HideUI(true);

            // Was already hidden, user probably wants freecam
            if (previouslyHidenState) {
                controller.retainControl = true;
            }

            // End of timeline replay, they probably want to restart from the beginning if they pressed play while the playhead was at the end.
            if (Math.Abs(playhead - length) < 0.5) {
                playhead = 0f;
            }

            if (useWorldScene)
            {
                worldPlayer.Stop(false, false);
                worldPlayer.Play(playhead, true, TimelineAudioManager.AttemptLoad(GlobalSettings.referenceTrackName));
            }
        }
        
        public void Pause()
        {
            playing = false;
            
            _slider.interactable = true;

            if (!controller.retainControl) {
                HideUI(false);
                controller.LockMouse(false);
            }

            if (useWorldScene)
            {
                worldPlayer.Pause();
            }
        }

        public bool IgnoreUIInputs() {
            return nameField.isFocused;
        }
        
        public void Update()
        {

            if (useWorldScene)
            {
                if (Math.Abs(worldPlayer.totalSceneLength - length) > 0.1f)
                {
                    length = worldPlayer.totalSceneLength;
                    UpdateSlider();
                }
            }

            TimeSpan goalTime = TimeSpan.FromSeconds(playhead);
            _timeDisplay.text = string.Format("{0:D2}:{1:D2}.{2:D2}", goalTime.Minutes, goalTime.Seconds, goalTime.Milliseconds);
            
            if (playhead > length)
            {
                Pause();
                playhead = length;
            }

            if (playing)
            {
                if (useWorldScene)
                {
                    playhead = WorldPlayer.playHead;
                }
                else
                {
                    playhead += TimelineMainClass.lastDeltaTime;
                }
                
                _slider.value = playhead;
                UpdateCameraKeyframeState(playhead);
                
            }

            if (!IgnoreUIInputs()) {
                if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (selectedKeyframe != null)
                    {
                        keyframes.Remove(selectedKeyframe.time);
                        selectedKeyframe = null;
                        UpdateKeyframeDisplays();
                    }
                }
            }
        }

        public void UpdateCameraKeyframeState(float time)
        {
            if (keyframes.Count == 0)
            {
                return;
            }

            // Don't override player desired pos if they want total freecam.
            if (controller.retainControl) {
                return;
            }
            
            Keyframe nextKeyframe = null;
            Keyframe previousKeyframe = null;

            System.Tuple<SettingsPanel, SettingsPanelCapture> nextSettingsPanelCapture = null;
            System.Tuple<SettingsPanel, SettingsPanelCapture> previousSettingsPanelCapture = null;
            
            foreach (var settingsKeyframe in settingsKeyframes)
            {
                if (settingsKeyframe.Key > time)
                {
                    nextSettingsPanelCapture = settingsKeyframe.Value;
                    break;
                }
                previousSettingsPanelCapture = settingsKeyframe.Value;
            }
            
            foreach (Keyframe keyframe in keyframes.Values)
            {
                if (keyframe.time > time)
                {
                    nextKeyframe = keyframe;
                    break;
                }
                previousKeyframe = keyframe;
            }
            
            if (nextSettingsPanelCapture != null)
            {
                if (previousSettingsPanelCapture == null)
                {
                    nextSettingsPanelCapture.Item1.HandleCapture(nextSettingsPanelCapture.Item2, null, 1);
                }
                else
                {
                    float closeness = (time - previousSettingsPanelCapture.Item2.time) / (nextSettingsPanelCapture.Item2.time - previousSettingsPanelCapture.Item2.time);
                    nextSettingsPanelCapture.Item1.HandleCapture(nextSettingsPanelCapture.Item2, previousSettingsPanelCapture.Item2, closeness);
                }
            }

            if (nextKeyframe != null)
            {
                if (previousKeyframe == null)
                {
                    nextKeyframe.PreformAction(controller, null, 1);
                }
                else
                {
                    float closeness = (playhead - previousKeyframe.time) / (nextKeyframe.time - previousKeyframe.time);
                    nextKeyframe.PreformAction(controller, previousKeyframe, closeness);
                }
            }
            else {
                if (previousKeyframe != null) {
                    previousKeyframe.PreformAction(controller, null, 1);
                }
            }
        }

        public void UnSelectAllSelectedButtons() {
            eventSystem.SetSelectedGameObject(null);
        }

        public void MakeKeyframeAtPlayhead(KeyframeTypes types)
        {
            // Remove existing keyframe at playhead
            if (keyframes.ContainsKey(playhead)) {
                keyframes.Remove(playhead);
            }

            try
            {
                switch (types)
                {
                    case KeyframeTypes.LINEAR:
                        LinearKeyframe linearKeyframe = new LinearKeyframe();
                        linearKeyframe.time = playhead;
                        linearKeyframe.cameraStateCapture = controller.GetCameraStateCapture();
                        selectedKeyframe = linearKeyframe;
                        linearKeyframe.selected = true;
                        for (int i = 0; i < keyframes.Count; i++)
                        {
                            keyframes.Values[i].selected = false;
                        }
                        keyframes.Add(playhead, linearKeyframe);
                        break;
                    case KeyframeTypes.INSTANT:
                        InstantKeyframe instantKeyframe = new InstantKeyframe();
                        instantKeyframe.time = playhead;
                        instantKeyframe.cameraStateCapture = controller.GetCameraStateCapture();
                        selectedKeyframe = instantKeyframe;
                        instantKeyframe.selected = true;
                        for (int i = 0; i < keyframes.Count; i++)
                        {
                            keyframes.Values[i].selected = false;
                        }
                        keyframes.Add(playhead, instantKeyframe);
                        break;
                }

                // TODO: Map the setting panel anims to their keyframe types
                foreach (var settingsPanel in settingPanels)
                {
                    SettingsPanelCapture capture = settingsPanel.MakeCapture();
                    capture.time = playhead;
                    capture.Complete();
                    settingsKeyframes.Add(playhead, new System.Tuple<SettingsPanel, SettingsPanelCapture>(settingsPanel, capture));
                }
            }
            catch (System.Exception e) {
                
            }
            
            
            
            UpdateKeyframeDisplays();
        }

        public override int GetSize()
        {
            int totalSize = 0;
            totalSize += sizeof(int);

            foreach (var keyPair in keyframes) {
                totalSize += sizeof(float);
                totalSize += sizeof(byte);
                totalSize += keyPair.Value.GetSize();
            }

            totalSize += sizeof(int);

            foreach (var keyPair in settingsKeyframes)
            {
                // Time
                totalSize += sizeof(float);

                System.Tuple<SettingsPanel, SettingsPanelCapture> tuple = keyPair.Value;

                // Index of settings panel.
                totalSize += sizeof(int);

                // Size of settings panel capture.
                totalSize += sizeof(int);

                // Settings panel capture length
                totalSize += tuple.Item2.GetBytes().Length;
            }

            return totalSize + worldPlayer.GetSize();
        }

        public override void WriteToStream(BinaryStream stream)
        {
            stream.WriteInt32(keyframes.Count);
              
            foreach (var keyPair in keyframes)
            {
                stream.WriteSingle(keyPair.Key);
                stream.WriteByte((byte)keyPair.Value.type);
                stream.WriteSerializableMember(keyPair.Value);
            }

            stream.WriteInt32(settingsKeyframes.Count);

            foreach (var keyPair in settingsKeyframes)
            {
                stream.WriteSingle(keyPair.Key);

                SettingsPanel panel = keyPair.Value.Item1;

                int index = 0;

                for (int i = 0; i < settingPanels.Count; i++) {
                    if (settingPanels[i] == panel) {
                        index = i;
                        break;
                    }
                }

                stream.WriteInt32(index);

                SettingsPanelCapture settingsPanelCapture = keyPair.Value.Item2;

                stream.WriteInt32(settingsPanelCapture.GetBytes().Length);

                stream.WriteByteArray(settingsPanelCapture.GetBytes());
            }

            stream.WriteSerializableMember(worldPlayer);
        }

        public override void ReadFromStream(BinaryStream stream)
        {
            worldPlayer.Stop();

            keyframes.Clear();
            settingsKeyframes.Clear();

            int keyframeCount = stream.ReadInt32();

            for (int i = 0; i < keyframeCount; i++) {
                float time = stream.ReadSingle();
                byte keyFrameType = stream.ReadByte();

                SerializableRegistry.AttemptGetKeyframeFromType(keyFrameType, out var keyframeType);

                Keyframe keyframe = (Keyframe) stream.ReadSerializableMember(keyframeType);

                keyframe.time = time;

                keyframes.Add(time, keyframe);
            }

            int settingsCaptureCount = stream.ReadInt32();

            for (int i = 0; i < settingsCaptureCount; i++)
            {
                float time = stream.ReadSingle();
                
                int settingsPanelIndex = stream.ReadInt32();

                SettingsPanel settingsPanel = settingPanels[settingsPanelIndex];

                int settingsDataLength = stream.ReadInt32();

                SettingsPanelCapture settingsPanelCapture = new SettingsPanelCapture(stream.ReadByteArray(settingsDataLength));
                settingsPanelCapture.time = time;

                settingsKeyframes.Add(time, new System.Tuple<SettingsPanel, SettingsPanelCapture>(settingsPanel, settingsPanelCapture));
            }

            TimelineLogger.Debug($"Read keyframe count: {keyframeCount}, {settingsCaptureCount}");

            stream.ReadSerializableMember(worldPlayer);

            UpdateKeyframeDisplays();
        }
    }
}