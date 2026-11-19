// 메뉴: Anomaly > Build Player Animator
// HAON 팩의 대기·걷기 클립으로 플레이어 Animator Controller를 만든다(HAON 폴더는 수정하지 않음).
// Speed(float) > 0.1 이면 Walk, 아래면 Idle. 이미 있으면 같은 에셋 안에서 다시 구성한다(GUID 유지).
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anomaly.Editor.Animations
{
    public static class PlayerAnimatorBuilder
    {
        public const string ControllerPath = "Assets/_Project/Animations/Player/PlayerAnimator.controller";
        public const string SpeedParameter = "Speed";

        const string ClipDir = "Assets/Haons SD series Pack/Animation/Common/";
        const string IdleClipPath = ClipDir + "StandA_idleA.FBX";
        const string WalkClipPath = ClipDir + "WalkA_Front.FBX";
        const float WalkThreshold = 0.1f;   // 이 속도(m/s)를 넘으면 걷기
        const float BlendDuration = 0.15f;  // 전환 시간(정규화 아님, 초)

        [MenuItem("Anomaly/Build Player Animator")]
        public static void BuildFromMenu()
        {
            var controller = Build();
            if (controller == null) return;
            Selection.activeObject = controller;
            Debug.Log($"[PlayerAnimatorBuilder] 완료: {ControllerPath}");
        }

        public static AnimatorController GetOrCreate()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            return controller != null ? controller : Build();
        }

        public static AnimatorController Build()
        {
            var idle = LoadClip(IdleClipPath);
            var walk = LoadClip(WalkClipPath);
            if (idle == null || walk == null)
            {
                Debug.LogError($"[PlayerAnimatorBuilder] 클립을 찾지 못함. HAON 팩을 임포트했는지 확인: {IdleClipPath}, {WalkClipPath}");
                return null;
            }

            EnsureFolder("Assets/_Project", "Animations");
            EnsureFolder("Assets/_Project/Animations", "Player");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            else
            {
                controller.parameters = new AnimatorControllerParameter[0];
                var old = controller.layers[0].stateMachine;
                foreach (var s in old.states) old.RemoveState(s.state);
            }

            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            var walkState = sm.AddState("Walk");
            walkState.motion = walk;
            sm.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.hasFixedDuration = true;
            toWalk.duration = BlendDuration;
            toWalk.AddCondition(AnimatorConditionMode.Greater, WalkThreshold, SpeedParameter);

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.hasFixedDuration = true;
            toIdle.duration = BlendDuration;
            toIdle.AddCondition(AnimatorConditionMode.Less, WalkThreshold, SpeedParameter);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static AnimationClip LoadClip(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            return null;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
