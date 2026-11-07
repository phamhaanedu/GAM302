#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectBase.Editor
{
    [InitializeOnLoad]
    public static class HierarchyInspectorAutoDump
    {
        static HierarchyInspectorAutoDump()
        {
            EditorApplication.delayCall += DumpCurrentHierarchy;
        }

        [MenuItem("Tools/GAM301/Dump Scene Hierarchy")]
        public static void DumpCurrentHierarchy()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== SCENE HIERARCHY DUMP: {activeScene.name} (Path: {activeScene.path}) ===");
            sb.AppendLine($"Dumped at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Root GameObject Count: {roots.Length}");
            sb.AppendLine(new string('=', 60));

            foreach (var root in roots)
            {
                DumpGameObject(root, sb, 0);
            }

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Hierarchy_Dump.txt");
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"<color=green>[HierarchyInspectorAutoDump] Successfully dumped hierarchy ({roots.Length} roots) to: {outputPath}</color>");
        }

        private static void DumpGameObject(GameObject go, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 3);
            string activeStr = go.activeSelf ? "[ON]" : "[OFF]";
            string tagStr = go.CompareTag("Untagged") ? "" : $" [Tag: {go.tag}]";
            string layerStr = go.layer == 0 ? "" : $" [Layer: {LayerMask.LayerToName(go.layer)}]";

            var compNames = new System.Collections.Generic.List<string>();
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null)
                {
                    compNames.Add("MISSING_COMPONENT!");
                }
                else if (!(comp is Transform))
                {
                    compNames.Add(comp.GetType().Name);
                }
            }

            string comps = compNames.Count > 0 ? $" -> Components: ({string.Join(", ", compNames)})" : "";
            Vector3 pos = go.transform.localPosition;
            string posStr = $" (Pos: {pos.x:F1}, {pos.y:F1}, {pos.z:F1})";

            sb.AppendLine($"{indent}+- {go.name} {activeStr}{tagStr}{layerStr}{posStr}{comps}");

            for (int i = 0; i < go.transform.childCount; i++)
            {
                DumpGameObject(go.transform.GetChild(i).gameObject, sb, depth + 1);
            }
        }
    }
}
#endif
