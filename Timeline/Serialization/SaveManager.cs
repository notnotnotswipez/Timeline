using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Logging;
using Timeline.Serialization.Binary;
using Timeline.WorldRecording;

namespace Timeline.Serialization
{
    public static class SaveManager
    {

        public static string TIMELINE_SAVE_DIRECTORY = Path.Combine(MelonUtils.GameDirectory, "Timeline", "Saves");
        public static List<string> ALL_TIMELINE_SAVES_LIST = new List<string>();

        public const string TIMELINE_SAVE_EXTENSION = ".tl";

        // Update this whenever theres a breaking change to the binaries!
        public static int version = 0;

        public static void ValidateDirectories() {
            if (!Directory.Exists(TIMELINE_SAVE_DIRECTORY))
            {
                Directory.CreateDirectory(TIMELINE_SAVE_DIRECTORY);
            }

            PopulateTimelineSaves();
        }

        private static void PopulateTimelineSaves() {
            ALL_TIMELINE_SAVES_LIST.Clear();

            foreach (var file in Directory.GetFiles(TIMELINE_SAVE_DIRECTORY)) {
            
                string fileName = Path.GetFileName(file);
                string extension = Path.GetExtension(file);

                if (extension == TIMELINE_SAVE_EXTENSION) {
                    ALL_TIMELINE_SAVES_LIST.Add(fileName);
                }
            }
        }

        public static void SaveToFile(string name)
        {
            string filePath = Path.Combine(TIMELINE_SAVE_DIRECTORY, name + TIMELINE_SAVE_EXTENSION);

            WorldPlayer.Instance.TriggerSaveCallback();
            WorldPlayer.Instance.Stop();

            // Timeline data plus binary version number.
            BinaryStream totalStream = new BinaryStream(TimelineMainClass.timelineHolder.GetSize() + sizeof(int));
            totalStream.WriteInt32(version);
            TimelineMainClass.timelineHolder.WriteToStream(totalStream);

            int originalSize = totalStream.GetReservedData().Length;

            totalStream.Compress();

            int newSize = totalStream.GetReservedData().Length;

            TimelineLogger.Debug($"Compressed data from {originalSize} to {newSize}. Difference of {originalSize - newSize}");

            File.WriteAllBytes(filePath, totalStream.GetReservedData());

            PopulateTimelineSaves();
        }

        public static void LoadFromFile(string name)
        {
            string filePath = Path.Combine(TIMELINE_SAVE_DIRECTORY, name + TIMELINE_SAVE_EXTENSION);
            if (File.Exists(filePath))
            {

                byte[] bytes = File.ReadAllBytes(filePath);

                TimelineLogger.Debug("Got bytes from file: " + bytes.Length);

                BinaryStream stream = new BinaryStream(bytes);
                
                stream.Decompress();

                int version = stream.ReadInt32();
                stream.binaryVersion = version;

                TimelineMainClass.timelineHolder.ReadFromStream(stream);

                
                TimelineMainClass.timelineHolder.sessionVersion = version;
            }
        }
    }
}
