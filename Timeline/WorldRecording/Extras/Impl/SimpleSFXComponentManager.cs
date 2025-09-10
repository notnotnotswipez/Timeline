using Il2CppSLZ.Bonelab;
using Il2CppSLZ.SFX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.WorldRecording.Events.BuiltIn;
using UnityEngine;

namespace Timeline.WorldRecording.Extras.Impl
{
    public class SimpleSFXComponentManager : ComponentManager
    {
        public override Type ComponentType => typeof(SimpleSFX);

        public override void OnReceiveComponentsFromPlayback(Component[] components)
        {
            
        }

        public override void OnReceiveComponentsFromRecorder(Component[] components) {
        
            
        }

        public override void OnRecorderCompleted(Component[] components)
        {
            
        }

        public void PlaySound(int index, int clipIndex) {
            GetComponentByIndex<SimpleSFX>(index).AUDIOPLAY(clipIndex);
        }
    }
}
