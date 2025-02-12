using UnityEngine;

namespace Timeline.Settings.Menu
{
    public abstract class MenuSetting
    {
        public GameObject prefab;
        public string title;

        public abstract void OnMakeGui(Transform transform);
    }
}