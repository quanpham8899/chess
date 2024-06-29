using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


namespace Duke {
    public static class DukeHandler {
        public enum ConsoleLogType {
            Normal, Warning, Error
        };

        /// <summary>
        /// Use to comparing a LayerMask to GameObject.layer
        /// </summary>
        public static int FirstSetLayer(this LayerMask mask) {
            int value = mask.value;
            if (value == 0) return 0;  // Early out
            for (int l = 1; l < 32; l++)
                if ((value & (1 << l)) != 0) return l;  // Bitwise
            return -1;  // This line won't ever be reached but the compiler needs it
        }

        public static void LogMissingComponent(string component, string targetName, ConsoleLogType consoleType = ConsoleLogType.Normal) {
            string target = string.IsNullOrEmpty(targetName) ? "" : $" in {targetName}";
            string output = $"Missing {component}{target}";

            switch (consoleType) {
                case ConsoleLogType.Normal:
                    Debug.Log(output); break;
                case ConsoleLogType.Warning:
                    Debug.LogWarning(output); break;
                case ConsoleLogType.Error:
                    Debug.LogError(output); break;
            }
        }

        public static string FromNameToID (this string name, string prefix = "", string suffix = "") {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) {
                Debug.LogWarning("Name can't not be null or empty or whitespace");
                return "";
            }
            string ID = prefix;

            foreach (char c in name) {
                ID += c != ' ' ? c.ToString().ToLower() : '_';
            }

            return ID + suffix;
        }

        public static bool CheckAnimation(Animator animator, string clipName) {
            if (animator == null) return false;

            foreach (var clip in animator.runtimeAnimatorController.animationClips) {
                if (clip.name == clipName) {
                    return true;
                }
            }


            return false;
        }

        public static bool IsAnimationPlaying(Animator animator) {
            return animator.GetCurrentAnimatorStateInfo(0).length > animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public static bool IsAnimationPlaying(Animator animator, string stateName) {
            return IsAnimationPlaying(animator) && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }

#if UNITY_EDITOR
        public static void ClearLog() {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
#endif

    }

    namespace DukeFileDataHandler {

        [System.Serializable]
        public class FileDataHandler {
            [SerializeField][Tooltip("Place at default (Application.persistentDataPath + dataDirPath")]
            private string dataDirPath = "";
            [SerializeField][Tooltip("Please ensure file format (Suggest .json file)")]
            private string dataFileName = "";
            // private string path;

            //-------------------------

            public string DataDirPath { get => this.dataDirPath; }
            public string DataFileName { get => this.dataFileName; }

            //-------------------------

            bool CheckValid {
                get {
                    bool directory_check = string.IsNullOrEmpty(dataDirPath);
                    bool fileName_check = string.IsNullOrEmpty(dataFileName);

                    return !directory_check && !fileName_check ? true : false;
                }
            }

            public FileDataHandler(string dataDirPath, string dataFileName) {
                this.dataDirPath = dataDirPath;
                this.dataFileName = dataFileName;
            }

            /// <summary>
            /// Return a combine of directory and fileName
            /// </summary>
            /// <param name="isPersistentDataPath">Use persistent data path as default</param>
            public string FileDataPath(bool isPersistentDataPath = true) {
                if (!CheckValid) {
                    return "";
                }

                return Path.Combine(isPersistentDataPath ? Application.persistentDataPath + "/" + dataDirPath : dataDirPath, dataFileName);
            }
        }

        [System.Serializable]
        public class SerializeData<T> where T : class {
            // public static void SaveListData(List<T> lst_data, string path) {
            //     if (path == "") {
            //         Debug.LogWarning("Missing Path");
            //         return;
            //     }

            //     SerializeList<T> s_list = new SerializeList<T>();
            //     s_list.NewList(lst_data);

            //     SerializeData<List<T>>.SaveData(s_list, path);
            // }

            /// <summary>
            /// Save data at target directory
            /// </summary>
            /// <param name="data">Data want to save</param>
            /// <param name="path">Path/FileName (FileName must have format)</param>
            public static void SaveData(T data, string path) {
                try {
                    Debug.Log("Try to save data at: " + path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    //Serialize data to json file
                    string dataToStore = JsonUtility.ToJson(data, true);

                    using (FileStream stream = new FileStream(path, FileMode.Create)) {
                        using (StreamWriter writer = new StreamWriter(stream)) {
                            writer.Write(dataToStore);
                        }
                    }

                    Debug.Log("Finished save data");
                } catch (System.Exception e) {
                    Debug.LogError("Some error occurred when trying to save data to: " + path + "\nDetail: " + e);
                }
            }


            public static T LoadData(string path) {
                T data = null;

                if (File.Exists(path)) {
                    try {
                        string dataToLoad = "";

                        using (FileStream stream = new FileStream(path, FileMode.Open)) {
                            using (StreamReader reader = new StreamReader(stream)) {
                                dataToLoad = reader.ReadToEnd();
                            }
                        }

                        if (dataToLoad == "{}") {
                            return null;
                        }

                        //Deserialize from Json file to NPCsData 
                        data = JsonUtility.FromJson<T>(dataToLoad);
                    } catch (System.Exception e) {
                        Debug.LogWarning("Some error occurred when trying to load data from: " + path + "\nDetail: " + e);
                        Debug.LogWarning("Return to null data");
                        data = null;
                    }
                }

                return data;
            }
        }

    }
}

