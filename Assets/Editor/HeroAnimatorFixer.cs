#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

namespace ProjectBase.Editor
{
    public static class HeroAnimatorFixer
    {
        private const string AnimFolder = "Assets/Import/SciFiWarriorPBRHPPolyart/Animations";
        private const string ControllerPath = "Assets/Import/SciFiWarriorPBRHPPolyart/Animators/SciFiWarrior.controller";

        [MenuItem("Tools/Fix SciFiWarrior Animator Controller")]
        public static void FixAnimatorAndClips()
        {
            Debug.Log("[HeroAnimatorFixer] Bắt đầu sửa đổi Animator Controller và Loop Clips...");

            // 1. Ép loopTime = true cho các file animation lặp
            SetClipLoopTime($"{AnimFolder}/Idle_guard_ar.fbx", true);
            SetClipLoopTime($"{AnimFolder}/Run_guard_AR.fbx", true);
            SetClipLoopTime($"{AnimFolder}/Run_gunMiddle_AR.fbx", true);
            SetClipLoopTime($"{AnimFolder}/WalkFront_Shoot_ar.fbx", true);

            // 2. Load các animation clip chính xác
            AnimationClip idleClip = LoadClipFromFbx($"{AnimFolder}/Idle_guard_ar.fbx");
            AnimationClip runClip = LoadClipFromFbx($"{AnimFolder}/Run_guard_AR.fbx");
            AnimationClip shootClip = LoadClipFromFbx($"{AnimFolder}/Shoot_SingleShot_AR.fbx");
            AnimationClip jumpClip = LoadClipFromFbx($"{AnimFolder}/Jump.fbx");
            AnimationClip dieClip = LoadClipFromFbx($"{AnimFolder}/Die.fbx");

            if (idleClip == null || runClip == null)
            {
                Debug.LogError("[HeroAnimatorFixer] Không load được clip Idle hoặc Run!");
                return;
            }

            // 3. Load hoặc tạo mới AnimatorController
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Xóa sạch parameters cũ và thêm mới chuẩn xác
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("isRun", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("isAtk", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("isJump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("isDie", AnimatorControllerParameterType.Trigger);

            // Lấy root state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            // Xóa sạch các state & transition cũ để xây dựng pipeline mượt mà 100%
            foreach (var childState in rootStateMachine.states)
            {
                rootStateMachine.RemoveState(childState.state);
            }
            foreach (var entryTrans in rootStateMachine.entryTransitions)
            {
                rootStateMachine.RemoveEntryTransition(entryTrans);
            }
            foreach (var anyTrans in rootStateMachine.anyStateTransitions)
            {
                rootStateMachine.RemoveAnyStateTransition(anyTrans);
            }

            // 4. Tạo các State
            AnimatorState idleState = rootStateMachine.AddState("Idle_Guard", new Vector3(200, 0, 0));
            idleState.motion = idleClip;

            AnimatorState runState = rootStateMachine.AddState("Run_Guard", new Vector3(200, 100, 0));
            runState.motion = runClip;

            AnimatorState shootState = rootStateMachine.AddState("Shoot_Single", new Vector3(450, 0, 0));
            shootState.motion = shootClip != null ? shootClip : idleClip;

            AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(450, 100, 0));
            jumpState.motion = jumpClip != null ? jumpClip : idleClip;

            AnimatorState dieState = rootStateMachine.AddState("Die", new Vector3(450, 200, 0));
            dieState.motion = dieClip != null ? dieClip : idleClip;

            rootStateMachine.defaultState = idleState;

            // 5. Cấu hình Transitions mượt mà (Crossfade duration 0.15s, hasExitTime = false)
            // Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.15f;
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isRun");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isRun");

            // AnyState -> Shoot
            var anyToShoot = rootStateMachine.AddAnyStateTransition(shootState);
            anyToShoot.hasExitTime = false;
            anyToShoot.duration = 0.05f;
            anyToShoot.AddCondition(AnimatorConditionMode.If, 0, "isAtk");

            var shootToIdle = shootState.AddTransition(idleState);
            shootToIdle.hasExitTime = true;
            shootToIdle.exitTime = 0.65f;
            shootToIdle.duration = 0.1f;

            // AnyState -> Jump
            var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.08f;
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "isJump");

            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.hasExitTime = true;
            jumpToIdle.exitTime = 0.75f;
            jumpToIdle.duration = 0.15f;

            // AnyState -> Die
            var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.15f;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "isDie");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // 6. Gán lại vào Animator trên Player_Hero trong Scene
            var playerHero = GameObject.Find("Player_Hero");
            if (playerHero != null)
            {
                var anim = playerHero.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    Undo.RecordObject(anim, "Assign Fixed Controller");
                    anim.runtimeAnimatorController = controller;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    EditorUtility.SetDirty(anim);
                }
            }

            Debug.Log("[HeroAnimatorFixer] Hoàn tất setup Animator Controller không tì vết!");
        }

        private static void SetClipLoopTime(string fbxPath, bool loop)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;

            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime != loop)
                {
                    clips[i].loopTime = loop;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log($"[HeroAnimatorFixer] Đã cập nhật loopTime={loop} cho: {fbxPath}");
            }
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
#endif
