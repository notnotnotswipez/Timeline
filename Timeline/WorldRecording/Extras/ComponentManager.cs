using Il2CppInterop.Runtime;
using Il2CppSLZ.Marrow.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Recorders;
using UnityEngine;

namespace Timeline.WorldRecording.Extras
{
    public abstract class ComponentManager
    {
        private Dictionary<int, Component> indexToComponent = new Dictionary<int, Component>();
        private Dictionary<int, int> componentToIndex = new Dictionary<int, int>();
        public ObjectRecorder objectRecorder;

        public bool valid = false;

        public abstract Type ComponentType { get; }

        public abstract void OnReceiveComponentsFromRecorder(Component[] components);

        public abstract void OnReceiveComponentsFromPlayback(Component[] components);

        public void Populate(ObjectRecorder recorder) {
            indexToComponent.Clear();
            componentToIndex.Clear();

            int index = 0;

            bool found = false;

            objectRecorder = recorder;

            foreach (var comp in recorder.targetObject.GetComponentsInChildren(Il2CppType.From(ComponentType), true)) {
                found = true;
                indexToComponent.Add(index, comp);
                componentToIndex.Add(comp.GetInstanceID(), index);
                index++;
            }

            valid = found;
        }

        public T GetComponentByIndex<T>(int index) where T : Component {
            if (!indexToComponent.ContainsKey(index)) {
                return null;
            }
            return indexToComponent[index].TryCast<T>();
        }

        public int GetIndexFromComponent(Component component)
        {
            if (!componentToIndex.ContainsKey(component.GetInstanceID())) {
                return -1;
            }

            return componentToIndex[component.GetInstanceID()];
        }

        public Component[] GetAllComponentInstances() {
            return indexToComponent.Values.ToArray();
        }

        public abstract void OnRecorderCompleted(Component[] components);

        public virtual void OnUpdate(bool playback) {
            
        }
    }
}
