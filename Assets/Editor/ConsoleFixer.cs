#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace ProjectBase.Editor
{
    public static class ConsoleFixer
    {
        [MenuItem("Tools/Clear Console")]
        public static void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod?.Invoke(null, null);
            Debug.Log("[ConsoleFixer] Console cleared.");
        }
    }
}
#endif
