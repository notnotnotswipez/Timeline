using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections.Concurrent;
using Il2CppTMPro;

namespace Timeline.Utils
{
    public class TimerManager
    {
        private static List<TimerDelayedAction> timerDelayedJobs = new List<TimerDelayedAction>();

        public static void Update()
        {
            foreach (var delayedAction in timerDelayedJobs) {
                delayedAction.time -= TimelineMainClass.lastDeltaTime;
                if (delayedAction.time <= 0) {
                    delayedAction.onTimeOver.Invoke();
                    delayedAction.completed = true;
                }
            }

            timerDelayedJobs.RemoveAll(x => x.completed);
        }

        public static void DelayAction(float time, Action onCompleted)
        {
            timerDelayedJobs.Add(new TimerDelayedAction()
            {
                time = time,
                onTimeOver = onCompleted
            });
        }
    }

    public class TimerDelayedAction {
        public float time;
        public Action onTimeOver;
        public bool completed = false;
    }
}
