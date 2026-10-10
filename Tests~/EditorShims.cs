using System;
using UnityEngine;

namespace UnityEditor
{
    public class EditorWindow : MonoBehaviour
    {
        public GUIContent titleContent;
        public Vector2 minSize;

        public static T GetWindow<T>() where T : EditorWindow, new()
        {
            return new T();
        }

        public void Show() { }

        public void Repaint() { }
    }

    public static class EditorStyles
    {
        public static GUIStyle boldLabel = new GUIStyle();
        public static GUIStyle wordWrappedLabel = new GUIStyle();
        public static GUIStyle miniLabel = new GUIStyle();
    }

    public enum MessageType
    {
        None,
        Info,
        Warning,
        Error
    }

    public static class EditorGUILayout
    {
        public static void LabelField(string label) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static void Space() { }
        public static float Slider(string label, float value, float min, float max) { return value; }
        public static UnityEngine.Object ObjectField(string label, UnityEngine.Object value, Type type, bool allowSceneObjects) { return value; }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static Vector2 BeginScrollView(Vector2 pos, params GUILayoutOption[] options) { return pos; }
        public static void EndScrollView() { }
        public static string TextField(string label, string value) { return value; }
        public static string PasswordField(string label, string value) { return value; }
        public static bool Toggle(bool value, params GUILayoutOption[] options) { return value; }
        public static void HelpBox(string message, MessageType type) { }
    }

    public static class EditorGUI
    {
        public static void BeginDisabledGroup(bool disabled) { }
        public static void EndDisabledGroup() { }
    }

    public static class EditorUtility
    {
        public static string SaveFilePanel(string title, string directory, string defaultName, string extension)
        {
            return "";
        }

        public static void DisplayProgressBar(string title, string info, float progress) { }

        public static void ClearProgressBar() { }
    }

    public static class EditorApplication
    {
        public static double timeSinceStartup = 0d;
        public static Action update;
    }

    public class SceneView : EditorWindow
    {
        public static Action<SceneView> duringSceneGui;
        public static void RepaintAll() { }
    }

    public static class AssetDatabase
    {
        public static void Refresh() { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string itemName) { }
    }
}