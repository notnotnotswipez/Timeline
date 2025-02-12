using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timeline.WorldRecording.Extensions
{
    public interface OverridableRecording
    {
       
        public void OnOverrideStart(float sceneTime);

    }
}
