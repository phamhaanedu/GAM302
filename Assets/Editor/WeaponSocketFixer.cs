using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace GAM301.EditorTools
{
    public static class WeaponSocketFixer
    {
        [MenuItem("Tools/Fix Weapon Socket For PBRCharacter")]
        public static void FixWeaponSocket()
        {
            var pbr = GameObject.Find("PBRCharacter");
            if (pbr == null)
            {
                var hero = GameObject.Find("Player_Hero");
                if (hero != null) pbr = hero.GetComponentInChildren<Animator>()?.gameObject;
            }

            if (pbr == null)
            {
                Debug.LogError("[WeaponSocketFixer] PBRCharacter not found in scene!");
                return;
            }

            // Unpack prefab instance if connected so we can restructure hierarchy freely
            if (PrefabUtility.IsPartOfAnyPrefab(pbr))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(pbr);
                if (root != null)
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }

            var anim = pbr.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogError("[WeaponSocketFixer] Animator not found on PBRCharacter!");
                return;
            }

            var rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand == null)
            {
                Debug.LogError("[WeaponSocketFixer] RightHand bone not found!");
                return;
            }

            // Find or create Weapon_Socket_R under RightHand
            Transform socket = rightHand.Find("Weapon_Socket_R");
            if (socket == null)
            {
                var socketGo = new GameObject("Weapon_Socket_R");
                socket = socketGo.transform;
                socket.SetParent(rightHand, false);
                socket.localPosition = Vector3.zero;
                socket.localRotation = Quaternion.identity;
                socket.localScale = Vector3.one;
            }

            // Find all AssaultRifle objects
            var rifles = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == "AssaultRifle").ToList();

            Transform rifleTransform = null;
            foreach (var r in rifles)
            {
                if (r.parent == socket)
                {
                    rifleTransform = r;
                }
                else if (r.parent != null && (r.parent.name == "PBRCharacter" || r.parent.name == "Player_Hero"))
                {
                    if (rifleTransform == null)
                    {
                        rifleTransform = r;
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(r.gameObject);
                    }
                }
            }

            if (rifleTransform == null)
            {
                Debug.LogError("[WeaponSocketFixer] No AssaultRifle found in scene!");
                return;
            }

            // Convert SkinnedMeshRenderer to MeshFilter + MeshRenderer if needed
            var smr = rifleTransform.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = null;
            Material mat = null;
            if (smr != null)
            {
                mesh = smr.sharedMesh;
                mat = smr.sharedMaterial;
                Undo.DestroyObjectImmediate(smr);
            }

            var mf = rifleTransform.GetComponent<MeshFilter>();
            if (mf == null) mf = Undo.AddComponent<MeshFilter>(rifleTransform.gameObject);
            if (mesh != null) mf.sharedMesh = mesh;

            var mr = rifleTransform.GetComponent<MeshRenderer>();
            if (mr == null) mr = Undo.AddComponent<MeshRenderer>(rifleTransform.gameObject);
            if (mat != null) mr.sharedMaterial = mat;

            // Parent to socket
            Undo.SetTransformParent(rifleTransform, socket, "Attach Rifle to Socket");
            rifleTransform.localScale = Vector3.one;

            // Calibrated local transform under Weapon_Socket_R to align perfectly with both hands and aiming axis
            rifleTransform.localPosition = new Vector3(-4.88f, -2.10f, -0.86f);
            rifleTransform.localEulerAngles = new Vector3(4.70f, 351.96f, 91.24f);

            // Align MuzzlePoint on AssaultRifle exactly at barrel tip
            var playerHero = GameObject.Find("Player_Hero");
            if (playerHero != null)
            {
                var muzzle = rifleTransform.Find("MuzzlePoint");
                if (muzzle == null)
                {
                    var muzzleGo = new GameObject("MuzzlePoint");
                    muzzle = muzzleGo.transform;
                    muzzle.SetParent(rifleTransform, false);
                }
                muzzle.localPosition = new Vector3(0f, 127.08f, -1.86f);
                muzzle.localEulerAngles = new Vector3(270f, 0f, 0f);
                muzzle.localScale = Vector3.one;

                var pc = playerHero.GetComponent<PlayerController>();
                if (pc != null)
                {
                    var so = new SerializedObject(pc);
                    so.FindProperty("muzzlePoint").objectReferenceValue = muzzle;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[WeaponSocketFixer] Successfully aligned AssaultRifle to Hand_Right socket and saved scene!");
        }
    }
}
