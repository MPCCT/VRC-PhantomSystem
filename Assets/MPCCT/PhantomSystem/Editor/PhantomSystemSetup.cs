using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace MPCCT
{
    public class PhantomSystemSetup : EditorWindow
    {
        private VRCAvatarDescriptor BaseAvatar;
        private VRCAvatarDescriptor PhantomAvatar;
        private bool IsRenameParameters;
        private bool IsRemovePhantomMenu;
        private bool IsRemovePhantomAvatarMA;
        private bool IsRemoveOriginalAnimator;
        private bool IsUseRotationConstraint;
        private bool IsRotationSolveInWorldSpace;
        private const string MAPrefabPath = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA.prefab";
        private const string MAPrefabPath_NoPhantomMenu = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA_NoPhantomMenu.prefab";
        private const string MenuPath = "Assets/MPCCT/PhantomSystem/Menu";
        private const string PhantomMenuPath = "Assets/MPCCT/PhantomSystem/Menu/PhantomSystemPhantomMenu.asset";

        private bool showAdvanced = false;

        private HashSet<string> SavedMenuName = new HashSet<string>();

        [MenuItem("MPCCT/PhantomSystemSetup")]
        private static void Init()
        {
            var window = GetWindowWithRect<PhantomSystemSetup>(new Rect(0, 0, 500, 400));
            window.minSize = new Vector2(200, 200);
            window.maxSize = new Vector2(1000, 1000);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("PhantomSystem v0.1.4 Made By MPCCT");
            BaseAvatar = EditorGUILayout.ObjectField("基础模型", BaseAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            PhantomAvatar = EditorGUILayout.ObjectField("分身模型", PhantomAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            IsRenameParameters = EditorGUILayout.ToggleLeft("重命名分身模型的参数", IsRenameParameters);

            // Advanced Settings 
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高级设置", true);

            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                IsRemovePhantomMenu = EditorGUILayout.ToggleLeft("去除分身模型菜单(PhantomMenu)", IsRemovePhantomMenu);
                IsRemovePhantomAvatarMA = EditorGUILayout.ToggleLeft("去除分身模型MA组件", IsRemovePhantomAvatarMA);
                IsRemoveOriginalAnimator = EditorGUILayout.ToggleLeft("去除分身模型原始FX", IsRemoveOriginalAnimator);
                IsUseRotationConstraint = EditorGUILayout.ToggleLeft("使用Rotation Constraint（分身模型和基础模型骨骼不同时可能有用）", IsUseRotationConstraint);
                if (IsUseRotationConstraint)
                {
                    EditorGUI.indentLevel++;
                    IsRotationSolveInWorldSpace = EditorGUILayout.ToggleLeft("使用世界空间上的约束（对于模型不适配可能有用，会导致模型面朝方向不固定在世界）", IsRotationSolveInWorldSpace);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("开始配置！"))
            {
                try
                {
                    Setup();
                    EditorUtility.DisplayDialog("Success", "配置完成！", "OK");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog("Error!", "出现错误，请查看Console", "OK");
                }

                AssetDatabase.SaveAssets();
                // GC
                System.GC.Collect();
            }
        }

        private void Setup()
        {
            if (BaseAvatar == null || PhantomAvatar == null)
            {
                throw new InvalidOperationException("Base Avatar and Phantom Avatar must be set.");
            }

            // Magic Bug Fix
            BaseAvatar.gameObject.SetActive(true);
            PhantomAvatar.gameObject.SetActive(false);

            Debug.Log($"Setting up Phantom System for {BaseAvatar.name} using {PhantomAvatar.name} ...");
            // Delete existing Phantom System
            var existingPhantomSystem = BaseAvatar.transform.Find("PhantomSystem");
            if (existingPhantomSystem != null)
            {
                Debug.Log("Deleting existing Phantom System");
                DestroyImmediate(existingPhantomSystem.gameObject);
            }

            // Copy Phantom Avatar to Base Avatar
            var PhantomSystem = new GameObject("PhantomSystem");
            PhantomSystem.transform.parent = BaseAvatar.transform;
            GameObject PhantomAvatarRoot = Instantiate(PhantomAvatar.gameObject, PhantomSystem.transform);
            PhantomAvatarRoot.transform.position = BaseAvatar.transform.position;
            PhantomAvatarRoot.transform.rotation = BaseAvatar.transform.rotation;

            // Get the Animator component from the Phantom Avatar
            Animator PhantomAnimator = PhantomAvatarRoot.GetComponent<Animator>();
            if (PhantomAnimator == null || PhantomAnimator.isHuman == false)
            {
                throw new InvalidOperationException("Phantom Avatar must have a humanoid Animator component.");
            }
            //Get the Animator component from the Base Avatar
            Animator BaseAnimator = BaseAvatar.GetComponent<Animator>();
            if (BaseAnimator == null || BaseAnimator.isHuman == false)
            {
                throw new InvalidOperationException("Base Avatar must have a humanoid Animator component.");
            }

            // Amature Constraint
            Transform PhantomArmature = PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            Transform BaseArmature = BaseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            //Change the Phantom Avatar Armature's name
            PhantomArmature.name = "Armature_phantom";
            var AmatureConstraint = PhantomArmature.gameObject.AddComponent<VRCParentConstraint>();
            AmatureConstraint.Locked = true;
            AmatureConstraint.IsActive = false;
            AmatureConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = BaseArmature,
                    Weight = 1f
                }
            };
            AmatureConstraint.enabled = true;
            AmatureConstraint.FreezeToWorld = true;

            // Set the name of the Phantom Avatar
            PhantomAvatarRoot.name = "PhantomAvatar";
            // Deactivate the Phantom Avatar
            PhantomAvatarRoot.SetActive(false);

            // Animation path: "Assets/MPCCT/PhantomSystem/Animation/<name>"
            var PhantomOFF = new AnimationClip();
            var PhantomPrepare = new AnimationClip();
            var PhantomFreezeOff = new AnimationClip();
            var PhantomFreeze = new AnimationClip();

            // init all animation clips
            // PhantomOFF: deactivate the Phantom Avatar
            PhantomOFF.SetCurve(GetRelativePath(PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 0));
            // PhantomOFF: Armature constraint freeze to world; disable the constraint
            PhantomOFF.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            PhantomOFF.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));

            // PhantomPrepare: activate the Phantom Avatar
            PhantomPrepare.SetCurve(GetRelativePath(PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomPrepare: Armature constraint unfreeze world; enable the constraint
            PhantomPrepare.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            PhantomPrepare.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreezeOff: activate the Phantom Avatar
            PhantomFreezeOff.SetCurve(GetRelativePath(PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreezeOff: Armature constraint freeze world; enable the constraint
            PhantomFreezeOff.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            PhantomFreezeOff.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreeze: activate the Phantom Avatar
            PhantomFreeze.SetCurve(GetRelativePath(PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreeze: Armature constraint freeze world; enable the constraint
            PhantomFreeze.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            PhantomFreeze.SetCurve(GetRelativePath(PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // The rest bone's constraints
            HumanBodyBones[] humanBones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
            Dictionary<HumanBodyBones, string> PhantomBonePaths = new Dictionary<HumanBodyBones, string>();

            foreach (var bone in humanBones)
            {
                if (bone == HumanBodyBones.LastBone)
                {
                    continue; // Skip LastBone as they are handled separately
                }
                Transform PhantomBone = PhantomAnimator.GetBoneTransform(bone);
                Transform BaseBone = BaseAnimator.GetBoneTransform(bone);

                if (PhantomBone != null)
                {
                    // Save pahntom bone path
                    PhantomBonePaths.Add(bone, GetRelativePath(PhantomBone, BaseAvatar.transform));
                    if (BaseBone != null)
                    {
                        if (!IsUseRotationConstraint)
                        {
                            // Add Parent Constraint to the Phantom Avatar bone
                            var constraint = PhantomBone.gameObject.AddComponent<VRCParentConstraint>();
                            constraint.Locked = true;
                            constraint.IsActive = true;
                            constraint.Sources = new VRCConstraintSourceKeyableList
                            {
                                new VRCConstraintSource
                                {
                                    SourceTransform = BaseBone,
                                    Weight = 1f
                                }
                            };
                            constraint.enabled = true;
                            constraint.SolveInLocalSpace = true;

                            // Add keyframes to the Animation Clips

                            // PhantomOFF: constraint unfreeze world
                            PhantomOFF.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomPrepare: constraint unfreeze world
                            PhantomPrepare.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomFreezeOff: constraint unfreeze world
                            PhantomFreezeOff.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomFreeze: constraint freeze world
                            PhantomFreeze.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
                        }
                        else
                        {
                            // Add Rotation Constraint to the Phantom Avatar bone
                            var constraint = PhantomBone.gameObject.AddComponent<VRCRotationConstraint>();
                            constraint.Locked = false;
                            constraint.IsActive = false; // Initially inactive
                            constraint.Sources = new VRCConstraintSourceKeyableList
                            {
                                new VRCConstraintSource
                                {
                                    SourceTransform = BaseBone,
                                    Weight = 1f,
                                }
                            };
                            if (IsRotationSolveInWorldSpace)
                            {
                                constraint.SolveInLocalSpace = false; // Use world space solving
                            }
                            else
                            {
                                constraint.SolveInLocalSpace = true; // Use local space solving
                            }
                            constraint.IsActive = true;
                            constraint.TryBakeCurrentOffsets(VRCConstraintBase.BakeOptions.BakeOffsets); // Auto calculate offsets
                            constraint.Locked = true;

                            // Add keyframes to the Animation Clips

                            // PhantomOFF: constraint unfreeze world
                            PhantomOFF.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomPrepare: constraint unfreeze world
                            PhantomPrepare.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomFreezeOff: constraint unfreeze world
                            PhantomFreezeOff.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
                            // PhantomFreeze: constraint freeze world
                            PhantomFreeze.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
                        }
                    }
                }
            }


            string animationFolderPath = "Assets/MPCCT/PhantomSystem/Animation";
            // delete existing Animation Clips And Controllers
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomOFF.anim");
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomPrepare.anim");
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomFreezeOff.anim");
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomFreeze.anim");
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            // check if folder exists, if not, create it
            if (!Directory.Exists($"{animationFolderPath}/{BaseAvatar.name}"))
            {
                Directory.CreateDirectory($"{animationFolderPath}/{BaseAvatar.name}");
            }
            // save animation clips
            AssetDatabase.CreateAsset(PhantomOFF, $"{animationFolderPath}/{BaseAvatar.name}/PhantomOFF.anim");
            AssetDatabase.CreateAsset(PhantomPrepare, $"{animationFolderPath}/{BaseAvatar.name}/PhantomPrepare.anim");
            AssetDatabase.CreateAsset(PhantomFreezeOff, $"{animationFolderPath}/{BaseAvatar.name}/PhantomFreezeOff.anim");
            AssetDatabase.CreateAsset(PhantomFreeze, $"{animationFolderPath}/{BaseAvatar.name}/PhantomFreeze.anim");

            // delete existing animator controller
            AssetDatabase.DeleteAsset($"{animationFolderPath}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            // load reference animator controller
            AssetDatabase.CopyAsset($"{animationFolderPath}/PhantomSystem_FX_Reference.controller", $"{animationFolderPath}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            var PhantomController = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{animationFolderPath}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            // change controller's animation clips
            var MainStateMachine = PhantomController.layers[1].stateMachine;
            var states = MainStateMachine.states;
            foreach (var state in states)
            {
                if (state.state.name == "PhantomOFF")
                {
                    state.state.motion = PhantomOFF;
                }
                else if (state.state.name == "PhantomPrepare")
                {
                    state.state.motion = PhantomPrepare;
                }
                else if (state.state.name == "PhantomFreezeOff")
                {
                    state.state.motion = PhantomFreezeOff;
                }
                else if (state.state.name == "PhantomFreeze")
                {
                    state.state.motion = PhantomFreeze;
                }
            }
            EditorUtility.SetDirty(PhantomController);

            // Remove Phantom Avatar's existing components on root
            UnityEngine.Component[] PhantomAvatarOldComponents = PhantomAvatarRoot.GetComponents<UnityEngine.Component>();
            foreach (var component in PhantomAvatarOldComponents)
            {
                if (!(component is Transform))
                {
                    DestroyImmediate(component);
                }
            }


            // MA Adaptation
            var MABoneProxy = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarBoneProxy>(true);
            var MAMergeArmature = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMergeArmature>(true);
            var MAMeshSettings = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMeshSettings>(true);

            var MAInstaller = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMenuInstaller>(true);
            var MAMenuItems = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMenuItem>(true);
            var MAParameter = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarParameters>(true);
            var MAMergeAnimator = PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);

            VRCExpressionsMenu PhantomMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PhantomMenuPath);

            // MA bone proxy adaption
            foreach (var proxy in MABoneProxy)
            {
                if (proxy.boneReference == HumanBodyBones.LastBone)
                {
                    proxy.subPath = "PhantomSystem/PhantomAvatar/" + proxy.subPath;
                }
                else
                {
                    proxy.subPath = PhantomBonePaths[proxy.boneReference];
                    proxy.boneReference = HumanBodyBones.LastBone;
                }
            }

            // MA merge armature adaption
            foreach (var armature in MAMergeArmature)
            {
                // Sometimes MA merge armature will merge phantom avatar's cloth to base avatar's armature
                if (armature.mergeTarget.referencePath == BaseArmature.name)
                {
                    AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                    tempRelativePathRoot.Set(PhantomArmature.gameObject);
                    armature.mergeTarget = tempRelativePathRoot;
                }
            }

            // MA mesh settings adaption
            foreach (var setting in MAMeshSettings)
            {
                // Same as MA merge armature
                // only consider hip situation
                var BaseHipPath = GetRelativePath(BaseAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform);
                var PhantomHip = BaseAvatar.transform.Find(PhantomBonePaths[HumanBodyBones.Hips]).gameObject;
                AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                tempRelativePathRoot.Set(PhantomHip);
                if (setting.ProbeAnchor.referencePath == BaseHipPath)
                {
                    setting.ProbeAnchor = tempRelativePathRoot;
                }
                if (setting.RootBone.referencePath == BaseHipPath)
                {
                    setting.RootBone = tempRelativePathRoot;
                }
            }

            if (IsRemovePhantomAvatarMA)
            {
                // remove the Phantom Avatar's MA Menu installer if it exists
                foreach (var installer in MAInstaller)
                {
                    DestroyImmediate(installer);
                }
                // remove the Phantom Avatar's MA Menu Items if it exists
                foreach (var item in MAMenuItems)
                {
                    DestroyImmediate(item);
                }
                // remove the Phantom Avatar's MA Parameters if it exists
                foreach (var parameter in MAParameter)
                {
                    DestroyImmediate(parameter);
                }
                //remove the Phantom Avatar's MA Merge Animator if it exists
                foreach (var animator in MAMergeAnimator)
                {
                    DestroyImmediate(animator);
                }
            }
            else
            {
                // Rebase the Phantom Avatar's MA Menu installer to the PhantomMA

                // create menu folder if not exists
                if (!Directory.Exists($"{MenuPath}/{BaseAvatar.name}"))
                {
                    Directory.CreateDirectory($"{MenuPath}/{BaseAvatar.name}");
                }
                // delete existing menus
                string[] existingMenus = Directory.GetFiles($"{MenuPath}/{BaseAvatar.name}");
                foreach (var existingmenu in existingMenus)
                {
                    AssetDatabase.DeleteAsset(existingmenu);
                }

                // Rebase all MA Menu Installer
                foreach (var installer in MAInstaller)
                {
                    if (installer.installTargetMenu == null || installer.installTargetMenu == BaseAvatar.expressionsMenu)
                    {
                        installer.installTargetMenu = PhantomMenu;
                    }
                    else
                    {
                        // Copy the menu to the PhantomSystem folder
                        installer.installTargetMenu = CopyExpressionMenuRecursively(installer.installTargetMenu, $"{MenuPath}/{BaseAvatar.name}"); 
                    }

                    if (installer.menuToAppend != null)
                    {
                        // Copy the menu to the PhantomSystem folder
                        installer.menuToAppend = CopyExpressionMenuRecursively(installer.menuToAppend, $"{MenuPath}/{BaseAvatar.name}"); 
                    }
                }

                // Rebase all MA Menu Items
                foreach (var item in MAMenuItems)
                {
                    if (item.MenuSource == SubmenuSource.MenuAsset)
                    {
                        // Copy the menu to the PhantomSystem folder
                        item.Control.subMenu = CopyExpressionMenuRecursively(item.Control.subMenu, $"{MenuPath}/{BaseAvatar.name}");
                    }
                }

                // MA Merge Animator Absolute Path Rebase
                foreach (var animator in MAMergeAnimator)
                {
                    if (animator.pathMode == MergeAnimatorPathMode.Absolute)
                    {
                        animator.pathMode = MergeAnimatorPathMode.Relative;
                        AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                        tempRelativePathRoot.Set(PhantomAvatarRoot);
                        animator.relativePathRoot = tempRelativePathRoot;
                    }
                }

                // Rename all MA Parameters
                if (IsRenameParameters)
                {
                    foreach (var parameter in MAParameter)
                    {
                        for (int i = 0; i < parameter.parameters.Count; i++)
                        {
                            var p = parameter.parameters[i];
                            p.remapTo = "PhantomSystemRename_" + p.nameOrPrefix;
                            parameter.parameters[i] = p;
                        }
                    }
                }
            }

            // Add MA Prefab to PhantomSystem
            GameObject MAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IsRemovePhantomMenu ? MAPrefabPath_NoPhantomMenu : MAPrefabPath);
            GameObject MAPrefabInstance = Instantiate(MAPrefab, PhantomSystem.transform);
            MAPrefabInstance.name = "PhantomMA";
            // redirect the Phantom Avatar Animator to the new controller
            MAPrefabInstance.GetComponent<ModularAvatarMergeAnimator>().animator = PhantomController;

            if (!IsRemoveOriginalAnimator)
            {
                // Merge original Phantom Avatar's animator
                GameObject PhantomAvatarMA = new GameObject("PhantomOriginalFX_MA");
                PhantomAvatarMA.transform.parent = PhantomSystem.transform;
                // Set MAMergeAnimator for PhantomAvatarMA
                if (PhantomAvatar.customizeAnimationLayers)
                {
                    var PhantomAvatarMAMergeAnimator = PhantomAvatarMA.AddComponent<ModularAvatarMergeAnimator>();
                    PhantomAvatarMAMergeAnimator.animator = PhantomAvatar.baseAnimationLayers[4].animatorController;
                    PhantomAvatarMAMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                    PhantomAvatarMAMergeAnimator.deleteAttachedAnimator = true;
                    PhantomAvatarMAMergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
                    AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                    tempRelativePathRoot.Set(PhantomAvatarRoot);
                    PhantomAvatarMAMergeAnimator.relativePathRoot = tempRelativePathRoot;
                    PhantomAvatarMAMergeAnimator.matchAvatarWriteDefaults = true;
                    PhantomAvatarMAMergeAnimator.layerPriority = -1;
                }
                // Set MAParameters for PhantomAvatarMA
                if (PhantomAvatar.customExpressions)
                {
                    var PhantomAvatarMAParameters = PhantomAvatarMA.AddComponent<ModularAvatarParameters>();
                    PhantomAvatarMAParameters.parameters = GetParameterFromVRCParameter(PhantomAvatar.expressionParameters, IsRenameParameters);
                }

                if (!IsRemovePhantomMenu)
                {
                    // Set MA Menu Installer for PhantomAvatarMA
                    var PhantomAvatarMAMenuInstaller = PhantomAvatarMA.AddComponent<ModularAvatarMenuInstaller>();
                    if (PhantomAvatar.expressionsMenu != null)
                    {
                        // Copy the menu to the PhantomSystem folder
                        PhantomAvatarMAMenuInstaller.menuToAppend = CopyExpressionMenuRecursively(PhantomAvatar.expressionsMenu, $"{MenuPath}/{BaseAvatar.name}");
                        PhantomAvatarMAMenuInstaller.installTargetMenu = PhantomMenu;
                    }
                }
            }
        }

        private static string GetRelativePath(Transform target, Transform root)
        {
            if (target == root)
            {
                return "";
            }
            return GetRelativePath(target.parent, root) + (target.parent == root ? "" : "/") + target.name;
        }

        // Import Parameters from VRCExpressionParameters to MAParameters
        private List<ParameterConfig> GetParameterFromVRCParameter(VRCExpressionParameters parameters, bool IsRenamed)
        {
            List<ParameterConfig> result = new List<ParameterConfig>();
            string[] VRCDefaultParameters = { "VRCEmote", "VRCFaceBlendH", "VRCFaceBlendV" };

            foreach (var parameter in parameters.parameters)
            {
                // skip VRC default parameters
                if (VRCDefaultParameters.Contains(parameter.name)) continue;

                ParameterConfig config = new ParameterConfig
                {
                    nameOrPrefix = parameter.name,
                    defaultValue = parameter.defaultValue,
                    saved = parameter.saved,
                    localOnly = true
                };

                if (IsRenamed)
                {
                    config.remapTo = "PhantomSystemRename_" + parameter.name;
                }
                if (parameter.valueType == VRCExpressionParameters.ValueType.Bool)
                {
                    config.syncType = ParameterSyncType.Bool;
                }
                else if (parameter.valueType == VRCExpressionParameters.ValueType.Int)
                {
                    config.syncType = ParameterSyncType.Int;
                }
                else if (parameter.valueType == VRCExpressionParameters.ValueType.Float)
                {
                    config.syncType = ParameterSyncType.Float;
                }
                else
                {
                    config.syncType = ParameterSyncType.NotSynced;
                }
                if (parameter.networkSynced)
                {
                    config.localOnly = false;
                }
                result.Add(config);
            }
            return result;
        }

        private VRCExpressionsMenu CopyExpressionMenuRecursively(VRCExpressionsMenu sourceMenu, string path)
        {
            // Create unique menu name using GetInstanceID
            // use GUID
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceMenu));
            string MenuRename = $"{sourceMenu.name}_{guid.Substring(0, 8)}";
            // skip already copied menu
            if (SavedMenuName.Contains(MenuRename))
            {
                return AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>($"{path}/{MenuRename}.asset");
            }
            SavedMenuName.Add(MenuRename);

            // copy menu
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceMenu), $"{path}/{MenuRename}.asset");
            var copiedMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>($"{path}/{MenuRename}.asset");

            // trverse sub-menus
            var controls = copiedMenu.controls;
            for (int i=0;i < sourceMenu.controls.Count; i++)
            {
                var c = controls[i];
                if (c != null && c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null)
                {
                    c.subMenu = CopyExpressionMenuRecursively(c.subMenu, path);
                    controls[i] = c; // write back
                }
            }
            EditorUtility.SetDirty(copiedMenu);
            AssetDatabase.SaveAssetIfDirty(copiedMenu);
            return copiedMenu;
        }
    }
}