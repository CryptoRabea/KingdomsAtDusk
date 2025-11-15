using UnityEngine;
using UnityEditor;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Editor utility to quickly set up animation system on units.
    /// Menu: Tools/RTS/Setup Unit Animation
    /// </summary>
    public class AnimationSetupHelper : Editor
    {
        [MenuItem("Tools/RTS/Setup Unit Animation")]
        static void SetupUnitAnimation()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "No GameObject Selected",
                    "Please select a unit GameObject in the hierarchy to set up animations.",
                    "OK"
                );
                return;
            }

            // Check for required components
            bool hasAnimator = selected.GetComponent<Animator>() != null;
            bool hasMovement = selected.GetComponent<UnitMovement>() != null;
            bool hasCombat = selected.GetComponent<UnitCombat>() != null;
            bool hasHealth = selected.GetComponent<UnitHealth>() != null;

            if (!hasAnimator)
            {
                if (EditorUtility.DisplayDialog(
                    "Missing Animator",
                    "This GameObject doesn't have an Animator component. Add one?",
                    "Yes", "Cancel"))
                {
                    selected.AddComponent<Animator>();
                    Debug.Log("✅ Added Animator component");
                }
                else
                {
                    return;
                }
            }

            // Add UnitAnimationController if not present
            if (selected.GetComponent<UnitAnimationController>() == null)
            {
                selected.AddComponent<UnitAnimationController>();
                Debug.Log("✅ Added UnitAnimationController");
            }

            // Optionally add UnitAnimationEvents
            if (EditorUtility.DisplayDialog(
                "Add Animation Events Handler?",
                "Would you like to add UnitAnimationEvents for audio and effects?",
                "Yes", "Skip"))
            {
                if (selected.GetComponent<UnitAnimationEvents>() == null)
                {
                    selected.AddComponent<UnitAnimationEvents>();
                    Debug.Log("✅ Added UnitAnimationEvents");
                }
            }

            // Optionally add UnitAnimationAdvanced
            if (EditorUtility.DisplayDialog(
                "Add Advanced Animation Features?",
                "Would you like to add UnitAnimationAdvanced for IK and advanced features?",
                "Yes", "Skip"))
            {
                if (selected.GetComponent<UnitAnimationAdvanced>() == null)
                {
                    selected.AddComponent<UnitAnimationAdvanced>();
                    Debug.Log("✅ Added UnitAnimationAdvanced");
                }
            }

            // Warnings for missing components
            if (!hasMovement)
            {
                Debug.LogWarning("⚠️ UnitMovement component not found. Add it for movement animations.");
            }

            if (!hasCombat)
            {
                Debug.LogWarning("⚠️ UnitCombat component not found. Add it for attack animations.");
            }

            if (!hasHealth)
            {
                Debug.LogWarning("⚠️ UnitHealth component not found. Add it for death animations.");
            }

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Animation components have been added.\n\n" +
                "Next steps:\n" +
                "1. Assign an Animator Controller\n" +
                "2. Configure parameters in Animator\n" +
                "3. Set up animation events in clips\n\n" +
                "See ANIMATION_SYSTEM_GUIDE.md for details.",
                "OK"
            );

            EditorUtility.SetDirty(selected);
        }

        [MenuItem("Tools/RTS/Validate Animator Parameters")]
        public static void ValidateAnimatorParameters()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject with an Animator.", "OK");
                return;
            }

            Animator animator = selected.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorUtility.DisplayDialog(
                    "No Animator Controller",
                    "Selected GameObject doesn't have an Animator Controller assigned.",
                    "OK"
                );
                return;
            }

            // Check for required parameters
            var controller = animator.runtimeAnimatorController;
            string report = "Animator Parameter Validation:\n\n";

            bool hasSpeed = HasParameter(animator, "Speed", AnimatorControllerParameterType.Float);
            bool hasIsMoving = HasParameter(animator, "IsMoving", AnimatorControllerParameterType.Bool);
            bool hasAttack = HasParameter(animator, "Attack", AnimatorControllerParameterType.Trigger);
            bool hasDeath = HasParameter(animator, "Death", AnimatorControllerParameterType.Trigger);
            bool hasIsDead = HasParameter(animator, "IsDead", AnimatorControllerParameterType.Bool);

            report += hasSpeed ? "✅ Speed (Float)\n" : "❌ Missing: Speed (Float)\n";
            report += hasIsMoving ? "✅ IsMoving (Bool)\n" : "❌ Missing: IsMoving (Bool)\n";
            report += hasAttack ? "✅ Attack (Trigger)\n" : "❌ Missing: Attack (Trigger)\n";
            report += hasDeath ? "✅ Death (Trigger)\n" : "❌ Missing: Death (Trigger)\n";
            report += hasIsDead ? "✅ IsDead (Bool)\n" : "❌ Missing: IsDead (Bool)\n";

            // Optional parameters
            bool hasHit = HasParameter(animator, "Hit", AnimatorControllerParameterType.Trigger);
            if (hasHit)
            {
                report += "✅ Hit (Trigger) - Optional\n";
            }

            bool allRequired = hasSpeed && hasIsMoving && hasAttack && hasDeath && hasIsDead;

            if (allRequired)
            {
                report += "\n✅ All required parameters present!";
            }
            else
            {
                report += "\n⚠️ Some required parameters are missing.";
                report += "\nAdd them to your Animator Controller.";
            }

            EditorUtility.DisplayDialog("Parameter Validation", report, "OK");
        }

        private static bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName && param.type == type)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Tools/RTS/Create Animation Config")]
        static void CreateAnimationConfig()
        {
            AnimationConfigSO config = ScriptableObject.CreateInstance<AnimationConfigSO>();

            string path = "Assets/AnimationConfig.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"✅ Created AnimationConfig at {path}");
        }
    }

    /// <summary>
    /// Custom inspector for UnitAnimationController.
    /// Shows helpful info and debugging tools.
    /// </summary>
    [CustomEditor(typeof(UnitAnimationController))]
    public class UnitAnimationControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UnitAnimationController controller = (UnitAnimationController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Current State:", controller.CurrentState.ToString());
                
                if (controller.Animator != null)
                {
                    var stateInfo = controller.Animator.GetCurrentAnimatorStateInfo(0);
                    EditorGUILayout.LabelField("Animator State:", stateInfo.shortNameHash.ToString());
                    EditorGUILayout.LabelField("Normalized Time:", stateInfo.normalizedTime.ToString("F2"));
                }

                EditorGUILayout.Space();
                
                if (GUILayout.Button("Play Attack"))
                {
                    controller.PlayAttack();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime info", MessageType.Info);
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Validate Animator Parameters"))
            {
                ValidateAnimatorParameters();
            }

            if (GUILayout.Button("Open Animation Guide"))
            {
                string guidePath = "Assets/ANIMATION_SYSTEM_GUIDE.md";
                if (System.IO.File.Exists(guidePath))
                {
                    Application.OpenURL(guidePath);
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Guide Not Found",
                        "ANIMATION_SYSTEM_GUIDE.md not found in Assets folder.",
                        "OK"
                    );
                }
            }
        }

        private void ValidateAnimatorParameters()
        {
            UnitAnimationController controller = (UnitAnimationController)target;
            
            if (controller.Animator == null || controller.Animator.runtimeAnimatorController == null)
            {
                EditorUtility.DisplayDialog(
                    "No Animator Controller",
                    "Please assign an Animator Controller first.",
                    "OK"
                );
                return;
            }

            // Run validation (reuse from AnimationSetupHelper)
            AnimationSetupHelper.ValidateAnimatorParameters();
        }
    }
}
