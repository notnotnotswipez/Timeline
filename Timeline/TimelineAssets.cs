using System.IO;
using MelonLoader;
using Timeline.Logging;
using UnityEngine;

namespace Timeline
{
    public class EmbeddedResourceHelper
    {
        public static byte[] GetResourceBytes(string filename)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (resource.Contains(filename))
                {
                    using (Stream resFilestream = assembly.GetManifestResourceStream(resource))
                    {
                        if (resFilestream == null) return null;
                        byte[] ba = new byte[resFilestream.Length];
                        resFilestream.Read(ba, 0, ba.Length);
                        return ba;
                    }
                }
            }
            return null;
        }
    }
    
    public static class AssetBundleExtension
    {
        public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object {
            var asset = bundle.LoadAsset(name);

            if (asset != null) {
                asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return asset.TryCast<T>();
            }

            return null;
        }
    }

    public class TimelineAssets
    {
        public static GameObject keyframeGenericHolder;
        public static GameObject timelineUi;
        public static GameObject buttonField;
        public static GameObject numericalField;
        public static GameObject sliderField;
        public static GameObject greenScreenPlane;
        public static Texture2D linearKeyframe;
        public static Texture2D instantKeyframe;
        public static AudioClip singularBeep;
        
        public static void LoadAssets(AssetBundle bundle)
        {
            TimelineLogger.Debug("Loading bundle assets...");

            timelineUi =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/timelinemainui.prefab");
            buttonField =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/buttonfield.prefab");
            numericalField =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/numericalfield.prefab");
            sliderField =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/sliderfield.prefab");
            keyframeGenericHolder =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/keyframeholder.prefab");
            greenScreenPlane =
                bundle.LoadPersistentAsset<GameObject>("assets/timelineassets/greenplane.prefab");
            instantKeyframe =
                bundle.LoadPersistentAsset<Texture2D>("assets/timelineassets/instantkeyframeicon.png");
            linearKeyframe =
                bundle.LoadPersistentAsset<Texture2D>("assets/timelineassets/interpolatekeyframeicon.png");

            singularBeep =
                bundle.LoadPersistentAsset<AudioClip>("assets/timelineassets/singularbeep.wav");
        }
    }
}