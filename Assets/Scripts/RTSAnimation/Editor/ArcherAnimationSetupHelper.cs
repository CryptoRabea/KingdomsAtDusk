using UnityEngine;
using UnityEditor;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Editor utility for setting up archer animation system.
    /// Provides one-click setup and validation tools.
    /// </summary>
    public class ArcherAnimationSetupHelper : UnityEditor.Editor
    {
        [MenuItem("Tools/RTS/Archer/Setup Archer Animation System")]
        static void SetupArcherAnimation()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "No GameObject Selected",
                    "Please select an archer GameObject in the hierarchy.",
                    "OK"
                );
                return;
            }

            int step = 0;
            bool success = true;

            // Step 1: Add Animator
            if (!AddComponent<Animator>(selected, "Animator", ref step))
            {
                Debug.LogWarning("⚠️ Animator already exists");
            }

            // Step 2: Add ArcherAnimationController
            if (AddComponent<ArcherAnimationController>(selected, "ArcherAnimationController", ref step))
            {
                Debug.Log("✅ Added ArcherAnimationController");
            }

            // Step 3: Add ArcherAimIK
            if (AddComponent<ArcherAimIK>(selected, "ArcherAimIK", ref step))
            {
                Debug.Log("✅ Added ArcherAimIK");
            }

            // Step 4: Add ArcherCombatMode
            if (AddComponent<ArcherCombatMode>(selected, "ArcherCombatMode", ref step))
            {
                Debug.Log("✅ Added ArcherCombatMode");
            }

            // Step 4: Configure Animator
            if (selected.TryGetComponent<Animator>(out var animator))
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                Debug.Log("✅ Configured Animator settings");
            }

            // Verify required components
            bool hasMovement = selected.GetComponent<UnitMovement>() != null;
            bool hasCombat = selected.GetComponent<UnitCombat>() != null;
            bool hasHealth = selected.GetComponent<UnitHealth>() != null;

            string warnings = "";
            if (!hasMovement) warnings += "• UnitMovement component\n";
            if (!hasCombat) warnings += "• UnitCombat component\n";
            if (!hasHealth) warnings += "• UnitHealth component\n";

            string message = "Archer animation system setup complete!\n\n";

            if (!string.IsNullOrEmpty(warnings))
            {
                message += "⚠️ Missing recommended components:\n" + warnings + "\n";
            }

            message += "Next steps:\n";
            message += "1. Assign Animator Controller\n";
            message += "2. Import your 100 animations\n";
            message += "3. Set up Animator Controller (see guide)\n";
            message += "4. Configure settings in inspector\n\n";
            message += "See ARCHER_ANIMATION_SETUP_GUIDE.md for details.";

            EditorUtility.DisplayDialog("Setup Complete!", message, "OK");
            EditorUtility.SetDirty(selected);
        }

        [MenuItem("Tools/RTS/Archer/Create Archer Animation Config")]
        static void CreateArcherConfig()
        {
            ArcherAnimationConfig config = ScriptableObject.CreateInstance<ArcherAnimationConfig>();

            string path = "Assets/ArcherAnimationConfig.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"✅ Created ArcherAnimationConfig at {path}");

            EditorUtility.DisplayDialog(
                "Config Created",
                $"Archer Animation Config created at:\n{path}\n\n" +
                "Configure your animation settings in the inspector.",
                "OK"
            );
        }

        [MenuItem("Tools/RTS/Archer/Create Upper Body Mask")]
        static void CreateUpperBodyMask()
        {
            AvatarMask mask = new AvatarMask();

            // Enable upper body transforms
            mask.transformCount = 0; // We'll use humanoid mask instead

            // Configure humanoid body parts
            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                bool enablePart = false;

                switch (part)
                {
                    case AvatarMaskBodyPart.Head:
                    case AvatarMaskBodyPart.Body:
                    case AvatarMaskBodyPart.LeftArm:
                    case AvatarMaskBodyPart.RightArm:
                        enablePart = true; // Upper body
                        break;

                    case AvatarMaskBodyPart.Root:
                    case AvatarMaskBodyPart.LeftLeg:
                    case AvatarMaskBodyPart.RightLeg:
                    case AvatarMaskBodyPart.LeftFingers:
                    case AvatarMaskBodyPart.RightFingers:
                        enablePart = false; // Lower body and extremities
                        break;
                }

                mask.SetHumanoidBodyPartActive(part, enablePart);
            }

            string path = "Assets/ArcherUpperBodyMask.mask";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(mask, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = mask;

            Debug.Log($"✅ Created Upper Body Mask at {path}");

            EditorUtility.DisplayDialog(
                "Avatar Mask Created",
                $"Upper Body Avatar Mask created at:\n{path}\n\n" +
                "Assign this to Layer 1 (Upper Body) in your Animator Controller.\n\n" +
                "Enabled: Head, Body, Arms\n" +
                "Disabled: Root, Legs",
                "OK"
            );
        }

        [MenuItem("Tools/RTS/Archer/Validate Archer Setup")]
        public static void ValidateArcherSetup()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select an archer GameObject.", "OK");
                return;
            }

            string report = "Archer Animation Setup Validation:\n\n";
            int passCount = 0;
            int totalChecks = 0;

            // Check components
            totalChecks++;
            bool hasAnimator = selected.GetComponent<Animator>() != null;
            report += (hasAnimator ? "✅" : "❌") + " Animator\n";
            if (hasAnimator) passCount++;

            totalChecks++;
            bool hasArcherController = selected.GetComponent<ArcherAnimationController>() != null;
            report += (hasArcherController ? "✅" : "❌") + " ArcherAnimationController\n";
            if (hasArcherController) passCount++;

            totalChecks++;
            bool hasIK = selected.GetComponent<ArcherAimIK>() != null;
            report += (hasIK ? "✅" : "❌") + " ArcherAimIK\n";
            if (hasIK) passCount++;

            totalChecks++;
            bool hasMovement = selected.GetComponent<UnitMovement>() != null;
            report += (hasMovement ? "✅" : "❌") + " UnitMovement\n";
            if (hasMovement) passCount++;

            totalChecks++;
            bool hasCombat = selected.GetComponent<UnitCombat>() != null;
            report += (hasCombat ? "✅" : "❌") + " UnitCombat\n";
            if (hasCombat) passCount++;

            totalChecks++;
            bool hasHealth = selected.GetComponent<UnitHealth>() != null;
            report += (hasHealth ? "✅" : "❌") + " UnitHealth\n";
            if (hasHealth) passCount++;

            // Check Animator Controller
            Animator animator = selected.GetComponent<Animator>();
            totalChecks++;
            bool hasController = animator != null && animator.runtimeAnimatorController != null;
            report += (hasController ? "✅" : "❌") + " Animator Controller Assigned\n";
            if (hasController) passCount++;

            if (hasController)
            {
                report += "\nAnimator Parameters:\n";

                bool hasDirectionX = HasParameter(animator, "DirectionX", AnimatorControllerParameterType.Float);
                bool hasDirectionY = HasParameter(animator, "DirectionY", AnimatorControllerParameterType.Float);
                bool hasSpeed = HasParameter(animator, "Speed", AnimatorControllerParameterType.Float);
                bool hasIsMoving = HasParameter(animator, "IsMoving", AnimatorControllerParameterType.Bool);
                bool hasCombatState = HasParameter(animator, "CombatState", AnimatorControllerParameterType.Int);
                bool hasDraw = HasParameter(animator, "Draw", AnimatorControllerParameterType.Trigger);
                bool hasAim = HasParameter(animator, "Aim", AnimatorControllerParameterType.Trigger);
                bool hasRelease = HasParameter(animator, "Release", AnimatorControllerParameterType.Trigger);

                report += (hasDirectionX ? "✅" : "❌") + " DirectionX (Float)\n";
                report += (hasDirectionY ? "✅" : "❌") + " DirectionY (Float)\n";
                report += (hasSpeed ? "✅" : "❌") + " Speed (Float)\n";
                report += (hasIsMoving ? "✅" : "❌") + " IsMoving (Bool)\n";
                report += (hasCombatState ? "✅" : "❌") + " CombatState (Int)\n";
                report += (hasDraw ? "✅" : "❌") + " Draw (Trigger)\n";
                report += (hasAim ? "✅" : "❌") + " Aim (Trigger)\n";
                report += (hasRelease ? "✅" : "❌") + " Release (Trigger)\n";

                // Check layer count
                report += "\nAnimator Layers:\n";
                int layerCount = animator.layerCount;
                report += $"Layer Count: {layerCount} " + (layerCount >= 2 ? "✅" : "⚠️ (need 2)") + "\n";

                if (layerCount >= 2)
                {
                    report += $"Layer 0: {animator.GetLayerName(0)}\n";
                    report += $"Layer 1: {animator.GetLayerName(1)} (weight: {animator.GetLayerWeight(1)})\n";
                }
            }

            // Final score
            report += $"\n━━━━━━━━━━━━━━━━━━━━\n";
            report += $"Score: {passCount}/{totalChecks} checks passed\n";

            if (passCount == totalChecks)
            {
                report += "\n🎉 Perfect! Your archer is ready!";
            }
            else if (passCount >= totalChecks - 2)
            {
                report += "\n⚠️ Almost there! Fix the issues above.";
            }
            else
            {
                report += "\n❌ Setup incomplete. See guide for help.";
            }

            EditorUtility.DisplayDialog("Validation Report", report, "OK");
        }

        [MenuItem("Tools/RTS/Archer/Open Setup Guide")]
        public static void OpenSetupGuide()
        {
            string guidePath = "ARCHER_ANIMATION_SETUP_GUIDE.md";

            if (System.IO.File.Exists(guidePath))
            {
                System.Diagnostics.Process.Start(guidePath);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Guide Not Found",
                    "ARCHER_ANIMATION_SETUP_GUIDE.md not found in project root.\n\n" +
                    "The guide should be at:\n" +
                    Application.dataPath + "/../ARCHER_ANIMATION_SETUP_GUIDE.md",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/RTS/Archer/Combat Mode/Set: Must Stand Still")]
        static void SetModeStandStill()
        {
            SetCombatModeForSelected(CombatMovementMode.MustStandStill);
        }

        [MenuItem("Tools/RTS/Archer/Combat Mode/Set: Can Shoot While Moving")]
        static void SetModeShootWhileMoving()
        {
            SetCombatModeForSelected(CombatMovementMode.CanShootWhileMoving);
        }

        [MenuItem("Tools/RTS/Archer/Combat Mode/Set: Adaptive")]
        static void SetModeAdaptive()
        {
            SetCombatModeForSelected(CombatMovementMode.Adaptive);
        }

        [MenuItem("Tools/RTS/Archer/Combat Mode/Toggle Mode")]
        static void ToggleCombatMode()
        {
            GameObject[] selected = Selection.gameObjects;

            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select archer GameObjects.", "OK");
                return;
            }

            int changed = 0;
            foreach (GameObject obj in selected)
            {
                if (obj.TryGetComponent<ArcherCombatMode>(out var combatMode))
                {
                    combatMode.ToggleCombatMode();
                    EditorUtility.SetDirty(combatMode);
                    changed++;
                }
            }

            Debug.Log($"Toggled combat mode on {changed} archers");
        }

        private static void SetCombatModeForSelected(CombatMovementMode mode)
        {
            GameObject[] selected = Selection.gameObjects;

            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select archer GameObjects.", "OK");
                return;
            }

            int changed = 0;
            foreach (GameObject obj in selected)
            {
                if (obj.TryGetComponent<ArcherCombatMode>(out var combatMode))
                {
                    combatMode.SetCombatMode(mode);
                    EditorUtility.SetDirty(combatMode);
                    changed++;
                }
            }

            Debug.Log($"Set {changed} archers to {mode} mode");
            EditorUtility.DisplayDialog(
                "Combat Mode Changed",
                $"Changed {changed} archer(s) to:\n{mode}",
                "OK"
            );
        }

        private static bool AddComponent<T>(GameObject target, string componentName, ref int step) where T : Component
        {
            if (target.GetComponent<T>() == null)
            {
                target.AddComponent<T>();
                step++;
                return true;
            }
            return false;
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
    }

    /// <summary>
    /// Custom inspector for ArcherAnimationController
    /// </summary>
    [CustomEditor(typeof(ArcherAnimationController))]
    public class ArcherAnimationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ArcherAnimationController controller = (ArcherAnimationController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Combat State:", controller.CombatState.ToString());
                EditorGUILayout.LabelField("Movement State:", controller.MovementState.ToString());
                EditorGUILayout.LabelField("LOD Level:", controller.LODLevel.ToString());

                EditorGUILayout.Space();

                if (GUILayout.Button("Force Draw Attack"))
                {
                    controller.ForceDrawAttack();
                }

                if (GUILayout.Button("Cancel Attack"))
                {
                    controller.CancelAttack();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime info and test controls", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Setup"))
            {
                ArcherAnimationSetupHelper.ValidateArcherSetup();
            }

            if (GUILayout.Button("Open Setup Guide"))
            {
                ArcherAnimationSetupHelper.OpenSetupGuide();
            }
        }
    }
}
