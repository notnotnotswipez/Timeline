using Il2CppTMPro;
using System;
using UnityEngine;

namespace Timeline.Settings.Menu
{
    public class NumericalSetting : MenuSetting
    {
        public string title;
        public float value;
        public Action<float> onValueChanged;
        
        
        public NumericalSetting(string name, float value, Action<float> onValueChanged)
        {
            title = name;
            this.prefab = TimelineAssets.numericalField;
            this.value = value;
            this.onValueChanged = onValueChanged;
        }

        public override void OnMakeGui(Transform transform)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(prefab, transform);
            gameObject.transform.Find("SettingTitle").GetComponent<TMP_Text>().text = title;
            TMP_InputField inputField = gameObject.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
            inputField.onValueChanged.AddListener(new Action<string>(s =>
            {
                try
                {
                    float num = float.Parse(s);
                    onValueChanged.Invoke(num);
                }
                catch (Exception e)
                {
                    // ignored
                }
            }));
        }
    }
}