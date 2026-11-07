using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

namespace GAM301.EditorTools
{
    public static class GolemReplacer
    {
        [MenuItem("Tools/Setup PBR_Golem For Enemy_Patrol_FSM")]
        public static void SetupGolem()
        {
            var enemyObj = GameObject.Find("Enemy_Patrol_FSM");
            if (enemyObj == null)
            {
                Debug.LogError("[GolemReplacer] Enemy_Patrol_FSM not found in scene!");
                return;
            }

            Undo.RecordObject(enemyObj, "Setup Golem for Enemy_Patrol_FSM");

            // 1. Hide capsule mesh renderer
            var capsuleMr = enemyObj.GetComponent<MeshRenderer>();
            if (capsuleMr != null)
            {
                capsuleMr.enabled = false;
                EditorUtility.SetDirty(capsuleMr);
            }

            // 2. Find or instantiate PBR_Golem
            Transform golemTransform = enemyObj.transform.Find("PBR_Golem");
            if (golemTransform == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Import/Mini Legion Rock Golem PBR HP Polyart/Prefabs/PBR_Golem.prefab");
                if (prefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemyObj.transform);
                    instance.name = "PBR_Golem";
                    golemTransform = instance.transform;
                }
            }

            if (golemTransform == null)
            {
                Debug.LogError("[GolemReplacer] Failed to find or instantiate PBR_Golem!");
                return;
            }

            // 3. Align Golem Transform
            Undo.RecordObject(golemTransform, "Align Golem Transform");
            golemTransform.localPosition = Vector3.zero;
            golemTransform.localRotation = Quaternion.identity;
            golemTransform.localScale = Vector3.one * 0.65f;

            // 4. Ensure Animator has FreeGolem controller
            var anim = golemTransform.GetComponent<Animator>();
            if (anim != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Import/Mini Legion Rock Golem PBR HP Polyart/Animations/FreeGolem.controller");
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(anim);
                }
            }

            // 5. Configure CapsuleCollider to fit Golem body
            var col = enemyObj.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                Undo.RecordObject(col, "Adjust CapsuleCollider");
                col.center = new Vector3(0f, 1.2f, 0f);
                col.radius = 0.9f;
                col.height = 2.4f;
                EditorUtility.SetDirty(col);
            }

            // 6. Configure NavMeshAgent
            var agent = enemyObj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Undo.RecordObject(agent, "Adjust NavMeshAgent");
                agent.radius = 0.9f;
                agent.height = 2.4f;
                agent.baseOffset = 0f;
                agent.speed = 2.0f;
                agent.stoppingDistance = 2.2f;
                agent.angularSpeed = 180f;
                agent.acceleration = 8f;
                EditorUtility.SetDirty(agent);
            }

            // 7. Update EnemyPatrolFSM references & stats
            var fsm = enemyObj.GetComponent<EnemyPatrolFSM>();
            if (fsm != null)
            {
                Undo.RecordObject(fsm, "Adjust EnemyPatrolFSM");
                var serializedFsm = new SerializedObject(fsm);
                serializedFsm.FindProperty("animator").objectReferenceValue = anim;
                serializedFsm.FindProperty("attackRange").floatValue = 2.8f;
                serializedFsm.FindProperty("sightRange").floatValue = 10.0f;
                serializedFsm.FindProperty("maxHealth").floatValue = 100.0f;
                serializedFsm.FindProperty("patrolSpeed").floatValue = 2.0f;
                serializedFsm.FindProperty("chaseSpeed").floatValue = 3.6f;
                serializedFsm.ApplyModifiedProperties();
                EditorUtility.SetDirty(fsm);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[GolemReplacer] Setup PBR_Golem for Enemy_Patrol_FSM successfully completed!");
        }
    }
}
