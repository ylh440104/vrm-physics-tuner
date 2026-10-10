using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name = "";
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null); }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object o) { return ReferenceEquals(this, o); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform;

        public T GetComponent<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            return gameObject != null ? gameObject.GetComponentsInChildren<T>(includeInactive) : new T[0];
        }
    }

    public class Behaviour : Component { }

    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public Transform transform;
        private readonly List<Component> _components = new List<Component>();

        public GameObject(string n)
        {
            name = n;
            transform = new Transform();
            transform.gameObject = this;
            transform.name = n;
            _components.Add(transform);
        }

        public void AddComponent(Component c)
        {
            c.gameObject = this;
            if (c.transform == null) c.transform = transform;
            _components.Add(c);
        }

        public T GetComponent<T>() where T : Component
        {
            for (int i = 0; i < _components.Count; i++)
            {
                var t = _components[i] as T;
                if (t != null) return t;
            }
            return null;
        }

        public T GetComponentInChildren<T>(bool includeInactive) where T : Component
        {
            var own = GetComponent<T>();
            if (own != null) return own;
            return transform != null ? transform.GetComponentInChildrenRecursive<T>() : null;
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            var list = new List<T>();
            var own = GetComponent<T>();
            if (own != null) list.Add(own);
            if (transform != null) transform.CollectComponents(list);
            return list.ToArray();
        }
    }

    public class Transform : Component
    {
        public Transform parent;
        private readonly List<Transform> _children = new List<Transform>();
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 localScale = new Vector3(1f, 1f, 1f);

        public Transform() { name = ""; }

        public int childCount { get { return _children.Count; } }

        public Transform GetChild(int i) { return _children[i]; }

        public void SetParent(Transform p)
        {
            if (parent != null) parent._children.Remove(this);
            parent = p;
            if (p != null) p._children.Add(this);
        }

        public void SetParentRaw(Transform p)
        {
            parent = p;
            if (p != null) p._children.Add(this);
        }

        public Vector3 InverseTransformPoint(Vector3 world)
        {
            var d = world - position;
            var inv = Quaternion.Inverse(rotation);
            var r = inv.Rotate(d);
            float sx = Math.Abs(localScale.x) > 1e-8f ? localScale.x : 1e-8f;
            float sy = Math.Abs(localScale.y) > 1e-8f ? localScale.y : 1e-8f;
            float sz = Math.Abs(localScale.z) > 1e-8f ? localScale.z : 1e-8f;
            return new Vector3(r.x / sx, r.y / sy, r.z / sz);
        }

        public Vector3 TransformPoint(Vector3 local)
        {
            var s = new Vector3(local.x * localScale.x, local.y * localScale.y, local.z * localScale.z);
            var r = rotation.Rotate(s);
            return new Vector3(position.x + r.x, position.y + r.y, position.z + r.z);
        }

        public T GetComponentInChildrenRecursive<T>() where T : Component
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i].gameObject != null ? _children[i].gameObject.GetComponent<T>() : null;
                if (c != null) return c;
                var deeper = _children[i].GetComponentInChildrenRecursive<T>();
                if (deeper != null) return deeper;
            }
            return null;
        }

        public void CollectComponents<T>(List<T> sink) where T : Component
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i].gameObject != null ? _children[i].gameObject.GetComponent<T>() : null;
                if (c != null) sink.Add(c);
                _children[i].CollectComponents(sink);
            }
        }

        public void CollectTransforms(List<Transform> sink)
        {
            sink.Add(this);
            for (int i = 0; i < _children.Count; i++) _children[i].CollectTransforms(sink);
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public float sqrMagnitude { get { return x * x + y * y + z * z; } }

        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }

        public static float Distance(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude > 0f ? (float)Math.Sqrt((a - b).sqrMagnitude) : 0f; }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static Quaternion identity { get { return new Quaternion(0f, 0f, 0f, 1f); } }

        public static Quaternion Inverse(Quaternion q)
        {
            float n = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            if (n < 1e-12f) return identity;
            return new Quaternion(-q.x / n, -q.y / n, -q.z / n, q.w / n);
        }

        public Vector3 Rotate(Vector3 v)
        {
            float tx = 2f * (y * v.z - z * v.y);
            float ty = 2f * (z * v.x - x * v.z);
            float tz = 2f * (x * v.y - y * v.x);
            return new Vector3(
                v.x + w * tx + (y * tz - z * ty),
                v.y + w * ty + (z * tx - x * tz),
                v.z + w * tz + (x * ty - y * tx));
        }

        public static Quaternion Euler(float x, float y, float z)
        {
            float cx = (float)Math.Cos(x * 0.5f);
            float sx = (float)Math.Sin(x * 0.5f);
            float cy = (float)Math.Cos(y * 0.5f);
            float sy = (float)Math.Sin(y * 0.5f);
            float cz = (float)Math.Cos(z * 0.5f);
            float sz = (float)Math.Sin(z * 0.5f);
            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz);
        }
    }

    public class Renderer : Component { }
    public class SkinnedMeshRenderer : Renderer { }

    public enum HumanBodyBones
    {
        Hips, Spine, Chest, UpperChest, Neck, Head,
        LeftUpperArm, RightUpperArm, LeftLowerArm, RightLowerArm
    }

    public class Animator : Behaviour
    {
        public bool isHuman = false;
        public Transform GetBoneTransform(HumanBodyBones bone) { return null; }
    }

    public class GUIContent
    {
        public string text;
        public GUIContent(string t) { text = t; }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class Gizmos
    {
        public static Color color;
        public static void DrawLine(Vector3 a, Vector3 b) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
    }

    public enum MessageType { None, Info, Warning, Error }

    public static class Application
    {
        public static string dataPath = "/tmp";
    }

    public static class GUILayout
    {
        public static bool Button(string label) { return false; }
        public static bool Button(string label, params object[] options) { return false; }
        public static object Width(float w) { return null; }
        public static object Height(float h) { return null; }
    }

    public class GUIStyle { }

    public class GUISkin { }

    public class GUILayoutOption { }
}

namespace UnityEngine.Networking
{
    public class DownloadHandler
    {
        public string text = "";
    }

    public class DownloadHandlerBuffer : DownloadHandler { }

    public class UploadHandler { }

    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }

    public class UnityWebRequestAsyncOperation
    {
        public bool isDone = true;
    }

    public class UnityWebRequest
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }

        public string url;
        public string error = "";
        public Result result = Result.Success;
        public bool isDone = true;
        public UploadHandler uploadHandler;
        public DownloadHandler downloadHandler;

        public UnityWebRequest(string url, string method)
        {
            this.url = url;
        }

        public void SetRequestHeader(string name, string value) { }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            return new UnityWebRequestAsyncOperation();
        }

        public void Abort() { }

        public void Dispose() { }
    }
}

namespace UnityEditor
{
    using UnityEngine;

    public class EditorWindow : MonoBehaviour
    {
        public GUIContent titleContent = new GUIContent("");
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

    public static class EditorGUILayout
    {
        public static void LabelField(string label) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static void Space() { }
        public static float Slider(string label, float value, float min, float max) { return value; }
        public static Object ObjectField(string label, Object value, Type type, bool allowSceneObjects) { return value; }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static Vector2 BeginScrollView(Vector2 pos, params object[] options) { return pos; }
        public static void EndScrollView() { }
        public static string TextField(string label, string value) { return value; }
        public static string PasswordField(string label, string value) { return value; }
        public static bool Toggle(bool value, params object[] options) { return value; }
        public static void HelpBox(string message, MessageType type) { }
    }

    public static class EditorGUI
    {
        public static void BeginDisabledGroup(bool disabled) { }
        public static void EndDisabledGroup() { }
    }

    public static class EditorUtility
    {
        public static string SaveFilePanel(string title, string dir, string defaultName, string extension)
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
        public MenuItemAttribute(string path) { }
    }
}