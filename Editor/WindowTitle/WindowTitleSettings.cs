#if UNITY_SETTINGS_MANAGER
using System.Reflection;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEditor.SceneManagement; // Add this import

namespace QuickEye.Utility.Editor.WindowTitle
{
    internal static class WindowTitleSettings
    {
        private static Settings _instance;

public static Settings Instance =>
    _instance ?? (_instance = new Settings(new[] {
// Add UserSettingsRepository so SettingsGUILayout can write/read without warnings
        new UserSettingsRepository(),
 new PackageSettingsRepository("com.quickeye.utility", "com.quickeye.utility") 
}));


        private static readonly string _DisabledTextColorTag =
            $"<color=#{ColorUtility.ToHtmlStringRGB(EditorColorPalette.Current.DefaultText)}{128:X2}>";

        [UserSetting]
        private static readonly UserSetting<bool> _EnableCustomTitle =
            new UserSetting<bool>(Instance, "qe.window-title.enable", false, SettingsScope.Project);

        [UserSetting]
        private static readonly UserSetting<string> _FormatString =
            new UserSetting<string>(Instance, "qe.window-title.format-string", "<RepoDirName> | <Branch>",
                SettingsScope.Project);

        [UserSetting]
        private static readonly UserSetting<string> _RepositoryPath =
            new UserSetting<string>(Instance, "qe.window-title.repo-path", "./", SettingsScope.Project);

        public static bool EnableCustomTitle => _EnableCustomTitle.value;
        public static string WindowTitle => TitleFormatter.Format(_FormatString.value);
        public static string RepositoryPath => _RepositoryPath.value;

        // Ensure updates on domain load and playmode/scene/platform changes
        [InitializeOnLoadMethod]
        private static void Init()
        {
            UpdateWindowTitle();
            EditorApplication.playModeStateChanged += _ => UpdateWindowTitle();
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => UpdateWindowTitle();
            EditorUserBuildSettings.activeBuildTargetChanged += UpdateWindowTitle;
        }

        [UserSettingBlock(" ")]
        private static void OnGUI(string searchContext)
        {
            var style = new GUIStyle(EditorStyles.helpBox);
            style.fontSize = 13;
            style.richText = true;
            EditorGUI.BeginChangeCheck();

            var enable = SettingsGUILayout.SettingsToggle("Enable Custom Window Title", _EnableCustomTitle, searchContext);
            if (enable != _EnableCustomTitle.value)
            {
                _EnableCustomTitle.value = enable;
                Instance.Save();
                UpdateWindowTitle();
            }

            using (new EditorGUI.DisabledScope(!_EnableCustomTitle.value))
            {
                var format = SettingsGUILayout.SettingsTextField("Window Title Format String", _FormatString, searchContext);
                if (format != _FormatString.value)
                {
                    _FormatString.value = format;
                    Instance.Save();
                    UpdateWindowTitle();
                }

                var repo = SettingsGUILayout.SettingsTextField(
                    new GUIContent("Git Repository Path", "Git repository root directory"), _RepositoryPath, searchContext);
                if (repo != _RepositoryPath.value)
                {
                    _RepositoryPath.value = repo;
                    Instance.Save();
                    UpdateWindowTitle();
                }

                var parametersInfoBox = $@"Available title parameters:
    • <Branch> {_DisabledTextColorTag}{TitleFormatter.Format("<Branch>")}</color>
    • <SceneName> {_DisabledTextColorTag}{TitleFormatter.Format("<SceneName>")}</color>
    • <ProjectName> {_DisabledTextColorTag}{TitleFormatter.Format("<ProjectName>")}</color>
    • <RepoDirName> {_DisabledTextColorTag}{TitleFormatter.Format("<RepoDirName>")}</color>
    • <ProjectPath> {_DisabledTextColorTag}{TitleFormatter.Format("<ProjectPath>")}</color>
    • <RepoPath> {_DisabledTextColorTag}{TitleFormatter.Format("<RepoPath>")}</color>
    • <EditorVersion> {_DisabledTextColorTag}{TitleFormatter.Format("<EditorVersion>")}</color>
    • <TargetPlatform> {_DisabledTextColorTag}{TitleFormatter.Format("<TargetPlatform>")}</color>";
                GUILayout.Label(parametersInfoBox, style);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Fallback to ensure persistence; update was already called per-field above
                Instance.Save();
                UpdateWindowTitle();
            }
        }

        private static void UpdateWindowTitle()
        {
            try
            {
                var type = typeof(EditorApplication);
                var method = type.GetMethod("UpdateMainWindowTitle", BindingFlags.Static | BindingFlags.NonPublic);
                method?.Invoke(null, null);
            }
            catch
            {
            }
        }
    }
}
#endif