using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace ChaosCritters.Editor
{
    [InitializeOnLoad]
    public class BackendManager : EditorWindow
    {
        private static Process _serverProcess;
        private static string _pythonPath = "python"; // Configurable
        private const string MENU_PATH_START = "ChaosCritters/Backend/Start Server";
        private const string MENU_PATH_STOP = "ChaosCritters/Backend/Stop Server";
        private const string MENU_PATH_AUTO = "ChaosCritters/Backend/Auto-Start on Play";

        // Preference keys
        private const string PREF_AUTO_START = "ChaosCritters_AutoStartBackend";
        private const string PREF_PYTHON_PATH = "ChaosCritters_PythonPath";

        static BackendManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _pythonPath = EditorPrefs.GetString(PREF_PYTHON_PATH, "python");
        }

        [MenuItem(MENU_PATH_START)]
        public static void StartServer()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                Debug.LogWarning("Backend server is already running.");
                return;
            }

            _pythonPath = EditorPrefs.GetString(PREF_PYTHON_PATH, "python");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string scriptPath = Path.Combine(projectRoot, "backend", "server.py");

            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"Could not find server script at: {scriptPath}");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };

            try
            {
                _serverProcess = new Process();
                _serverProcess.StartInfo = startInfo;
                _serverProcess.OutputDataReceived += (sender, args) => 
                { 
                    if (!string.IsNullOrEmpty(args.Data)) Debug.Log($"[Backend] {args.Data}"); 
                };
                _serverProcess.ErrorDataReceived += (sender, args) => 
                { 
                    if (!string.IsNullOrEmpty(args.Data)) Debug.LogError($"[Backend Error] {args.Data}"); 
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                Debug.Log($"<color=green>Backend server started (PID: {_serverProcess.Id})</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start backend server: {e.Message}");
            }
        }

        [MenuItem(MENU_PATH_STOP)]
        public static void StopServer()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                    Debug.Log("<color=yellow>Backend server stopped.</color>");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error stopping server: {e.Message}");
                }
            }
            else
            {
               // Debug.Log("No backend server is running.");
            }
        }

        // Auto-start validation (Checkbox)
        [MenuItem(MENU_PATH_AUTO)]
        private static void ToggleAutoStart()
        {
            bool status = EditorPrefs.GetBool(PREF_AUTO_START, true);
            EditorPrefs.SetBool(PREF_AUTO_START, !status);
        }

        [MenuItem(MENU_PATH_AUTO, true)]
        private static bool ToggleAutoStartValidate()
        {
            Menu.SetChecked(MENU_PATH_AUTO, EditorPrefs.GetBool(PREF_AUTO_START, true));
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PREF_AUTO_START, true)) return;

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    StartServer();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    StopServer();
                    break;
            }
        }
        
        // Ensure server is killed when Unity closes/compiles
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
             // Optional: Cleanup if needed, though process might persist if static is lost.
             // Usually better to rely on AssemblyReloadEvents but simpler here:
             // If we really lost track, we can't kill it easily without finding by name.
        }
        
        public void OnDestroy()
        {
            StopServer();
        }

        [MenuItem("ChaosCritters/Backend/Configure...")]
        public static void OpenConfig()
        {
            BackendConfigWindow.ShowWindow();
        }
    }

    public class BackendConfigWindow : EditorWindow
    {
        private string _pythonPath;

        public static void ShowWindow()
        {
            GetWindow<BackendConfigWindow>("Backend Config");
        }

        private void OnEnable()
        {
            _pythonPath = EditorPrefs.GetString("ChaosCritters_PythonPath", "python");
        }

        private void OnGUI()
        {
            GUILayout.Label("Backend Settings", EditorStyles.boldLabel);
            _pythonPath = EditorGUILayout.TextField("Python Path", _pythonPath);

            if (GUILayout.Button("Save"))
            {
                EditorPrefs.SetString("ChaosCritters_PythonPath", _pythonPath);
                Debug.Log($"Python path saved: {_pythonPath}");
                Close();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Status:", EditorStyles.boldLabel);
            // We can't easily access the private process status from here without exposing it, 
            // but for now this is sufficient for config.
        }
    }
}
