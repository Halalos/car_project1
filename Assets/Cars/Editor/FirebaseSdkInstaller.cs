#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cars.Editor
{
    [InitializeOnLoad]
    public static class FirebaseSdkInstaller
    {
        const string PendingKey = "Cars.FirebaseImportPending";
        static FirebaseSdkInstaller()
        {
            AssetDatabase.importPackageCompleted += OnImported;
            AssetDatabase.importPackageFailed += OnFailed;
        }
        [MenuItem("Tools/Cars/Import Downloaded Firebase SDK")]
        public static void ImportDownloadedSdk()
        {
            foreach (string name in new [] { "FirebaseAuth", "FirebaseFirestore" })
                if (!File.Exists("Tools/FirebaseSDK/" + name + ".unitypackage"))
                    throw new FileNotFoundException("Download the official Firebase Unity SDK first; see Docs/FIREBASE_SETUP.md");
            UnityEditor.SessionState.SetInt(PendingKey, 1);
            AssetDatabase.ImportPackage(Path.GetFullPath("Tools/FirebaseSDK/FirebaseAuth.unitypackage"), false);
        }
        static void OnImported(string name)
        {
            int step = UnityEditor.SessionState.GetInt(PendingKey, 0);
            if (step == 1)
            {
                UnityEditor.SessionState.SetInt(PendingKey, 2);
                EditorApplication.delayCall += () => AssetDatabase.ImportPackage(Path.GetFullPath("Tools/FirebaseSDK/FirebaseFirestore.unitypackage"), false);
            }
            else if (step == 2)
            {
                UnityEditor.SessionState.SetInt(PendingKey, 0);
                Debug.Log("CARS_FIREBASE_SDK_IMPORTED: import completed; connection NOT verified.");
            }
        }
        static void OnFailed(string name, string message)
        { UnityEditor.SessionState.SetInt(PendingKey, 0); Debug.LogError("Firebase SDK import failed: " + message); }
        [MenuItem("Tools/Cars/Enable Firebase Adapter")]
        public static void EnableAdapter()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (Array.IndexOf(symbols.Split(';'), "CARS_FIREBASE") < 0)
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols + ";CARS_FIREBASE");
            Debug.Log("CARS_FIREBASE_ADAPTER_ENABLED: SDK compilation check requested, connection NOT verified.");
        }
    }
}
#endif
