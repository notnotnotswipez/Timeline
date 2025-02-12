using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using MelonLoader;
using Timeline.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Timeline.Settings;
using Timeline.WorldRecording;

namespace Timeline.Audio
{
    public class TimelineAudioManager
    {
        private static string AUDIO_DIRECTORY = Path.Combine(MelonUtils.GameDirectory, "Timeline", "Audio");
        private static string VOICE_DIRECTORY = Path.Combine(AUDIO_DIRECTORY, "VO");

        // Constant calculated once is faster than dividing every sample
        const float _pcm16ToFloatConst = (1.0f / 32768.0f);
        const int _wavHeaderSize = 44;

        // Samples to peek to determine loudness
        const int _sampleWindow = 128;

        public static float loudnessMultiplier = 5f;

        public static bool recording = false;
        private static float _recordTimeStart = 0;
        private static AudioClip _recordingClip;

        private static Dictionary<string, AudioClip> _cachedAudioClipMemMap = new Dictionary<string, AudioClip>();
        private static List<Tuple<float, float>> pauses = new List<Tuple<float, float>>();

        static int lastMicPos = 0;
        static int sampleOffset = 0;

        public static void ValidateDirectories() {
            if (!Directory.Exists(AUDIO_DIRECTORY)) {
                Directory.CreateDirectory(AUDIO_DIRECTORY);
            }

            if (!Directory.Exists(VOICE_DIRECTORY))
            {
                Directory.CreateDirectory(VOICE_DIRECTORY);
            }
        }

        public static void StartMicrophoneRecording() {
            if (recording) {
                return;
            }

            recording = true;
            sampleOffset = 0;
            pauses.Clear();

            // Max of 999 seconds. Nobodies recording their audio for that long brah.
            // It gets trimmed when its done anyway.
            _recordingClip = Microphone.Start(GlobalSettings.targetMic, true, 999, 44100);
            _recordTimeStart = Time.realtimeSinceStartup;
        }

        public static void EndMicrophoneRecording(Action<AudioClip> onClipReceived) {
            if (!recording) {
                return;
            }

            recording = false;
            Microphone.End(GlobalSettings.targetMic);

            float time = Time.realtimeSinceStartup;
            float diff = time - _recordTimeStart;

            // Trim the clip to only what was actually recorded.
            AudioClip trimmedClip = Trim(_recordingClip, diff);

            // Remove game stutters
            AudioClip pauselessClip = FilterPausesFromAudioClip(trimmedClip, pauses);

            
            
            onClipReceived.Invoke(pauselessClip);

            _recordingClip = null;

        }

        public static void UpdateMicrophone() {
            if (!recording) {
                return;
            }

            // What is this for? Well I am certainly glad you asked!
            // Unity does not *drop* microphone data on frame loss, it queues it, meaning when the game gets delayed (IE. A prolonged stutter), the microphone data
            // Instantly becomes out of sync with our world time

            // We must manually do the correction by sampling when the microphone should drop and for how long
            int currentMicPos = Microphone.GetPosition(GlobalSettings.targetMic) - sampleOffset;

            // Sample space to realtime conversion
            float realTime = ((float)currentMicPos / (float)44100f);

            // Difference from realTime to playhead, (Ideally this should be roughly zero)
            float playHeadOffset = realTime - (WorldPlayer.playHead);

            // Its not roughly zero (BAD!!)
            if (playHeadOffset > 0.1f) {

                // The realtime new starting point for the recording cut
                float newStart = realTime - playHeadOffset;

                // Converts to sample space
                int toSampleSpace = Mathf.RoundToInt(newStart * 44100);

                // Difference in samples we just cut
                int sampleDiff = currentMicPos - toSampleSpace;

                // Offset all future sample readings by how many we cut just now
                sampleOffset += sampleDiff;

                pauses.Add(new Tuple<float, float>(toSampleSpace, playHeadOffset));
            }
        }

        public static List<string> GetAllAudioClipNames(bool fromVoiceFolder = false) {

            string rootPath = AUDIO_DIRECTORY;

            if (fromVoiceFolder)
            {
                rootPath = VOICE_DIRECTORY;
            }

            List<string> strings = new List<string>();

            foreach (var files in Directory.GetFiles(rootPath)) {
                strings.Add(Path.GetFileName(files));
            }

            return strings;
        }

        public static float GetMicrophoneLoudness() {
            if (recording) {
                float loudNess = GetLoudnessFromAudioClip(Microphone.GetPosition(GlobalSettings.targetMic), _recordingClip);
                return loudNess;
            }

            return 0f;
        }

        private static float GetLoudnessFromAudioClip(int samplePosition, AudioClip clip)
        {
            int startSample = samplePosition - _sampleWindow;
            if (startSample < 0)
            {
                startSample = 0;
            }

            Il2CppStructArray<float> waveData = new Il2CppStructArray<float>(_sampleWindow);
            clip.GetData(waveData, startSample);

            float totalLoudness = 0;

            for (int i = 0; i < _sampleWindow; i++)
            {
                totalLoudness += Mathf.Abs(waveData[i] * loudnessMultiplier);
            }

            return totalLoudness / _sampleWindow;
        }

        public static void ClearAudioCache() {
            foreach (var clip in _cachedAudioClipMemMap.Values) {
                GameObject.Destroy(clip);
            }

            _cachedAudioClipMemMap.Clear();
        }

        public static AudioClip AttemptLoad(string file, bool fromVoiceFolder = false) {
            
            if (!file.EndsWith(".wav"))
            {
                file += ".wav";
            }

            // We already got it we don't need to do the conversion.
            if (_cachedAudioClipMemMap.ContainsKey(file))
            {
                AudioClip targetClip = _cachedAudioClipMemMap[file];
                return targetClip;
            }

            string rootPath = AUDIO_DIRECTORY;

            if (fromVoiceFolder) {
                rootPath = VOICE_DIRECTORY;
            }

            string attemptedPath = Path.Combine(rootPath, file);
            
            if (File.Exists(attemptedPath)) {
                byte[] byteArr = File.ReadAllBytes(attemptedPath);
                AudioClip loadedClip = CreateAudioClipFromWavData(byteArr);
                _cachedAudioClipMemMap.Add(file, loadedClip);

                return loadedClip;
            }

            return null;
        }

        public static void AttemptSaveClipToFile(string fileName, AudioClip targetClip, bool toVoiceFolder = false) {
            if (!fileName.EndsWith(".wav")) {
                fileName += ".wav";
            }

            string rootPath = AUDIO_DIRECTORY;

            if (toVoiceFolder)
            {
                rootPath = VOICE_DIRECTORY;
            }

            string attemptedPath = Path.Combine(rootPath, fileName);

            byte[] wavBytes = AudioClipToWavBytes(targetClip);

            File.WriteAllBytes(attemptedPath, wavBytes);
        }

        static AudioClip CreateAudioClipFromWavData(byte[] wavData)
        {
            if (wavData == null || wavData.Length < _wavHeaderSize)
            {
                // This is not a proper wav file.
                return null;
            }

            // Header stuff for the WAV file.
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitDepth = BitConverter.ToInt16(wavData, 34);

            if (bitDepth != 16)
            {
                // Handle this later. Usually a WAV file is PCM 16-bit but dunno
                TimelineLogger.Error("Tried to read WAV file that was not 16-bit!");
                return null;
            }

            int dataLength = wavData.Length - _wavHeaderSize;
            int sampleCount = dataLength / 2;
            int sampleCountPerChannel = sampleCount / channels;

            float[] audioData = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(wavData, _wavHeaderSize + i * 2);

                // Convert the data to -1 to 1 range.
                audioData[i] = sample * _pcm16ToFloatConst;
            }

            AudioClip audioClip = AudioClip.Create("RETURNED_CLIP", sampleCountPerChannel, channels, sampleRate, false);
            audioClip.SetData(audioData, 0);

            audioClip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return audioClip;
        }

        public static byte[] AudioClipToWavBytes(AudioClip clip)
        {
            Il2CppStructArray<float> samples = new Il2CppStructArray<float>(clip.samples * clip.channels);
            clip.GetData(samples, 0);

            int sampleCount = samples.Length;
            short[] intData = new short[sampleCount];
            byte[] bytesData = new byte[sampleCount * 2];

            for (int i = 0; i < sampleCount; i++)
            {
                float clampedSample = Mathf.Clamp(samples[i], -1f, 1f);
                intData[i] = (short) (clampedSample * short.MaxValue);
            }

            Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            int fileSize = _wavHeaderSize + bytesData.Length - 8;
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            int byteRate = sampleRate * channels * 2;

            // Header stuff
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short) 1);
            writer.Write((short) channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short) (channels * 2));
            writer.Write((short) 16);

            // The actual data
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(bytesData.Length);

            writer.Write(bytesData);

            writer.Flush();
            byte[] wavFileBytes = stream.ToArray();

            writer.Close();
            stream.Close();

            return wavFileBytes;
        }

        // Ridiculous method
        private static AudioClip FilterPausesFromAudioClip(AudioClip clip, List<Tuple<float, float>> markers)
        {

            int totalFrames = clip.samples;
            int channels = clip.channels;
            Il2CppStructArray<float> clipData = new Il2CppStructArray<float>(totalFrames * channels);
            clip.GetData(clipData, 0);

            List<float> trimmedData = new List<float>();

            int lastFrameIndex = 0;

            foreach (var marker in markers)
            {
                int markerFrameIndex = (int) marker.Item1;
                int pauseFrameCount = (int) (marker.Item2 * clip.frequency);

                int framesToCopy = markerFrameIndex - lastFrameIndex;
                if (framesToCopy > 0)
                {
                    int sampleCount = framesToCopy * channels;
                    int startIndex = lastFrameIndex * channels;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        trimmedData.Add(clipData[startIndex + i]);
                    }
                }
                lastFrameIndex = markerFrameIndex + pauseFrameCount;
            }

            if (lastFrameIndex < totalFrames)
            {
                int framesToCopy = totalFrames - lastFrameIndex;
                int sampleCount = framesToCopy * channels;
                int startIndex = lastFrameIndex * channels;
                for (int i = 0; i < sampleCount; i++)
                {
                    trimmedData.Add(clipData[startIndex + i]);
                }
            }

            int newFrameCount = trimmedData.Count / channels;
            AudioClip trimmedClip = AudioClip.Create("RETURNED_NO_PAUSES", newFrameCount, channels, clip.frequency, false);
            trimmedClip.SetData(trimmedData.ToArray(), 0);

            return trimmedClip;
        }

        public static AudioClip Trim(AudioClip clip, float cutoffTime)
        {
            if (clip == null)
            {
                return null;
            }

            int totalSamplesPerChannel = clip.samples;
            int sampleRate = clip.frequency;
            int channels = clip.channels;

            int cutoffSampleCount = Mathf.FloorToInt(cutoffTime * sampleRate);

            // Its already short enough so we don't have to trim it.
            if (cutoffSampleCount >= totalSamplesPerChannel)
            {
                return clip;
            }

            Il2CppStructArray<float> originalData = new Il2CppStructArray<float>(totalSamplesPerChannel * channels);
            clip.GetData(originalData, 0);

            int trimmedDataLength = cutoffSampleCount * channels;
            float[] trimmedData = new float[trimmedDataLength];

            for (int i = 0; i < trimmedDataLength; i++) {
                trimmedData[i] = originalData[i];
            }

            AudioClip trimmedClip = AudioClip.Create(clip.name, cutoffSampleCount, channels, sampleRate, false);

            trimmedClip.SetData(trimmedData, 0);

            return trimmedClip;
        }
    }
}
