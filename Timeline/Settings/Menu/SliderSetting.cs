using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline.Settings.Menu
{
    public class SliderSetting : MenuSetting
    {
        float value;
        float min;
        float max;
        Action<float> onValueChanged;

        private GameObject latestSpawned;
        
        public SliderSetting(string name, float value, float min, float max, Action<float> onValueChanged)
        {
            title = name;
            prefab = TimelineAssets.sliderField;
            this.value = value;
            this.min = min;
            this.max = max;
            this.onValueChanged = onValueChanged;
        }

        public override void OnMakeGui(Transform transform)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(prefab, transform);
            latestSpawned = gameObject;
            gameObject.transform.Find("SettingTitle").GetComponent<TMP_Text>().text = title;
            Slider slider = gameObject.transform.Find("Slider").GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            Navigation noAutomaticNav = Navigation.defaultNavigation;
            noAutomaticNav.mode = Navigation.Mode.None;

            slider.navigation = noAutomaticNav;
            
            TMP_Text valueText = gameObject.transform.Find("SettingValue").GetComponent<TMP_Text>();
            valueText.text = $"({Math.Round(value, 2)})";
            slider.onValueChanged.AddListener(new Action<float>((num) =>
            {
                valueText.text = $"({Math.Round(num, 2)})";
                value = num;
                onValueChanged.Invoke(num);
            }));
        }

        public void SetValue(float val) {
            value = val;

            if (latestSpawned) {
                TMP_Text valueText = latestSpawned.transform.Find("SettingValue").GetComponent<TMP_Text>();
                valueText.text = $"({Math.Round(value, 2)})";

                Slider slider = latestSpawned.transform.Find("Slider").GetComponent<Slider>();
                slider.value = val;
            }
        }
    }
}