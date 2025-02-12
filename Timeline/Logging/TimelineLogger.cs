using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timeline.Logging
{
    public class TimelineLogger
    {
        private static MelonLogger.Instance Instance;

        private static bool debug = false;

        public static void SetLoggerInstance(MelonLogger.Instance instance) {
            Instance = instance;
        }

        public static void Debug(string msg) {
            if (debug) {
                Instance.Msg(msg);
            }
        }

        public static void Msg(string msg)
        {
            Instance.Msg(msg);
        }

        public static void Error(string msg)
        {
            Instance.Error(msg);
        }
    }
}
