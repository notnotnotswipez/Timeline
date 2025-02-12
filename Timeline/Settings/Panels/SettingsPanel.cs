using MelonLoader;
using System;
using System.Collections.Generic;
using Timeline.Settings.Capture;
using Timeline.Settings.Menu;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace Timeline.Settings.Panels
{
    public abstract class SettingsPanel
    {
        public virtual string Name { get; }
        public List<MenuSetting> menuElements = new List<MenuSetting>();

        public abstract void InitializeMenu();
        
        public abstract SettingsPanelCapture MakeCapture();

        public abstract void HandleCapture(SettingsPanelCapture capture, SettingsPanelCapture previousCapture, float diff);

        public void CreateMenu(Transform parent)
        {
            foreach (var element in menuElements)
            {
                element.OnMakeGui(parent);
            }
        }
        
        public NumericalSetting AddNumericalSetting(string name, float value, Action<float> onValueChanged)
        {
            NumericalSetting numSetting = new NumericalSetting(name, value, onValueChanged);
            menuElements.Add(numSetting);

            return numSetting;
        }
        
        public SliderSetting AddSliderSetting(string name, float value, float min, float max, Action<float> onValueChanged)
        {
            SliderSetting sliderSetting = new SliderSetting(name, value, min, max, onValueChanged);
            menuElements.Add(sliderSetting);

            return sliderSetting;
        }
        
        public ButtonSetting AddButtonSetting(string name, bool enabled, Action<bool> onChanged)
        {
            ButtonSetting buttonSetting = new ButtonSetting(name, enabled, onChanged);
            menuElements.Add(buttonSetting);

            return buttonSetting;
        }
    }
}