using Il2CppTMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline.Settings.Menu
{
    public class ButtonSetting : MenuSetting
    {
        public bool enabled = false;
        public Action<bool> onChanged;

        Button latestButton;

        public ButtonSetting(string name, bool enabled, Action<bool> onChanged)
        {
            title = name;
            prefab = TimelineAssets.buttonField;
            this.enabled = enabled;
            this.onChanged = onChanged;
        }
        
        public override void OnMakeGui(Transform transform)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(prefab, transform);
            gameObject.transform.Find("SettingTitle").GetComponent<TMP_Text>().text = title;
            Button button = gameObject.transform.Find("Button").GetComponent<Button>();
            latestButton = button;

            Navigation noAutomaticNav = Navigation.defaultNavigation;
            noAutomaticNav.mode = Navigation.Mode.None;

            button.navigation = noAutomaticNav;

            UpdateIndicator();
            button.onClick.AddListener(new Action(() =>
            {
                enabled = !enabled;
                UpdateIndicator();
                onChanged.Invoke(enabled);
            }));
        }

        public void UpdateIndicator() {
            latestButton.transform.Find("EnabledIndicator").gameObject.SetActive(enabled);
        }
    }
}