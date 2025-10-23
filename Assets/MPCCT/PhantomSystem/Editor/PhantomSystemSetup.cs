using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace MPCCT
{
    public class PhantomSystemSetup : EditorWindow
    {
        private VRCAvatarDescriptor BaseAvatar;
        private VRCAvatarDescriptor PhantomAvatar;
        private bool IsRenameParameters;
        private bool IsRemoveViewSystem;
        private bool IsRemovePhantomMenu;
        private bool IsRemovePhantomAvatarMA;
        private bool IsRemoveOriginalAnimator;
        private bool IsUseRotationConstraint;
        private bool IsRotationSolveInWorldSpace;
        private bool IsChangePBImmobileType;
        private const string MAPrefabPath = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA.prefab";
        private const string MAPrefabPath_NoPhantomMenu = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA_NoPhantomMenu.prefab";
        private const string ReferenceAnimationPath = "Assets/MPCCT/PhantomSystem/Animation/PhantomSystem_FX_Reference.controller";
        private const string PhantomMenuPath = "Assets/MPCCT/PhantomSystem/Menu/PhantomSystemPhantomMenu.asset";
        private const string ViewSystemPrefabPath = "Assets/MPCCT/PhantomSystem/ViewSystem/Prefab/PhantomView.prefab";
        private const string ViewSystemPrefabPath_NoPhantomMenu = "Assets/MPCCT/PhantomSystem/ViewSystem/Prefab/PhantomView_NoPhantomMenu.prefab";
        private const string GrabPrefabPath = "Assets/MPCCT/PhantomSystem/Prefab/GrabRoot.prefab";

        private const string GeneratedAnimationFolder = "Assets/MPCCT/PhantomSystem/~Generated/Animation";
        private const string GeneratedMenuFolder = "Assets/MPCCT/PhantomSystem/~Generated/Menu";

        private bool showAdvanced = false;

        private HashSet<string> SavedMenuName = new HashSet<string>();

        // --- Localization ---
        private enum Locale { English = 0, Chinese = 1, Japanese = 2 }
        private Locale currentLocale = Locale.English;

        private const string LocalMainMenuPath_zh = "Assets/MPCCT/PhantomSystem/Menu/Menu_zh/PhantomSystemMain_zh.asset";
        private const string LocalMainMenuPath_jp = "Assets/MPCCT/PhantomSystem/Menu/Menu_jp/PhantomSystemMain_jp.asset";
        private const string LocalSubMenuPath_zh = "Assets/MPCCT/PhantomSystem/Menu/Menu_zh/PhantomSystemSub_zh.asset";
        private const string LocalSubMenuPath_jp = "Assets/MPCCT/PhantomSystem/Menu/Menu_jp/PhantomSystemSub_jp.asset";

        private const string LocalMainMenu_NoPhantomMenuPath_zh = "Assets/MPCCT/PhantomSystem/Menu/Menu_zh/PhantomSystemMain_NoPhantomMenu_zh.asset";
        private const string LocalMainMenu_NoPhantomMenuPath_jp = "Assets/MPCCT/PhantomSystem/Menu/Menu_jp/PhantomSystemMain_NoPhantomMenu_jp.asset";
        private const string LocalSubMenu_NoPhantomMenuPath_zh = "Assets/MPCCT/PhantomSystem/Menu/Menu_zh/PhantomSystemSub_NoPhantomMenu_zh.asset";
        private const string LocalSubMenu_NoPhantomMenuPath_jp = "Assets/MPCCT/PhantomSystem/Menu/Menu_jp/PhantomSystemSub_NoPhantomMenu_jp.asset";

        private const string LocalViewSysMenuPath_zh = "Assets/MPCCT/PhantomSystem/ViewSystem/Animation/Menu/Menu_zh/ViewMain_zh.asset";
        private const string LocalViewSysMenuPath_jp = "Assets/MPCCT/PhantomSystem/ViewSystem/Animation/Menu/Menu_jp/ViewMain_jp.asset";

        private static readonly Dictionary<string, (string en, string zh, string jp)> s_texts = new Dictionary<string, (string, string, string)>
        {
            ["LanguageLabel"] = ("Language", "语言", "言語"),
            ["BaseAvatar"] = ("Base Avatar", "基础模型", "ベースアバター"),
            ["PhantomAvatar"] = ("Phantom Avatar", "分身模型", "ファントムアバター"),
            ["RenameParameters"] = ("Rename phantom avatar parameters", "重命名分身模型的参数", "ファントムのパラメータをリネームする"),
            ["RemoveViewSystem"] = ("Remove phantom view window", "去除分身视角窗口", "ファントムの視点ウィンドウを削除"),
            ["AdvancedSettings"] = ("Advanced Settings", "高级设置", "詳細設定"),
            ["RemovePhantomMenu"] = ("Remove phantom avatar menu", "去除分身模型菜单", "ファントムメニューを削除"),
            ["RemovePhantomAvatarMA"] = ("Remove Modular Avatar components from phantom", "去除分身模型MA组件", "ファントムの MA コンポーネントを削除"),
            ["RemoveOriginalAnimator"] = ("Remove Phantom Avatar's original FX", "去除分身模型原始FX", "ファントムの元のアニメーターを削除"),
            ["ChangePBImmobileType"] = ("Change PhysBone ImmobileType (may break some physbones)", "更改分身模型动骨ImmobileType（可能会使分身上部分动骨异常）", "PhysBoneのImmobileTypeを変更（骨が崩れる場合あり）"),
            ["UseRotationConstraint"] = ("Use Rotation Constraint (useful when bone hierarchies differ)", "使用Rotation Constraint（分身模型和基础模型骨骼不同时可能有用）", "Rotation Constraintを使用（ボーン構成が異なる場合に有効）"),
            ["RotationSolveInWorldSpace"] = ("Solve constraint in world space (may affect facing direction)", "使用世界空间上的约束（对于模型不适配可能有用，会导致模型面朝方向不固定在世界）", "ワールド空間で解く（向きが固定されない場合あり）"),
            ["StartButton"] = ("Setup!", "开始配置！", "セットアップを開始"),
            ["SuccessTitle"] = ("Success", "成功", "成功"),
            ["SuccessMessage"] = ("Setup completed!", "配置完成！", "設定が完了しました！"),
            ["ErrorTitle"] = ("Error!", "错误!", "エラー!"),
            ["ErrorMessage"] = ("An error occurred. See Console.", "出现错误，请查看Console", "エラーが発生しました。コンソールを確認してください。"),
            ["OK"] = ("OK", "确定", "OK")
        };

        private string T(string key)
        {
            if (!s_texts.TryGetValue(key, out var tuple)) return key;
            switch (currentLocale)
            {
                case Locale.Chinese:
                    return tuple.zh;
                case Locale.Japanese:
                    return tuple.jp;
                default:
                    return tuple.en;
            }
        }

        private void OnEnable()
        {
            // Load saved locale
            currentLocale = (Locale)EditorPrefs.GetInt("MPCCT_PhantomSystem_Locale", (int)Locale.English);
        }

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
            // Title
            EditorGUILayout.LabelField("PhantomSystem v0.2.0-alpha Made By MPCCT");

            // Language selection
            string[] localeOptions = new[] { "English", "中文", "日本語" };
            int newLocale = EditorGUILayout.Popup(T("LanguageLabel"), (int)currentLocale, localeOptions);
            if (newLocale != (int)currentLocale)
            {
                currentLocale = (Locale)newLocale;
                EditorPrefs.SetInt("MPCCT_PhantomSystem_Locale", newLocale);
            }

            // Main fields
            BaseAvatar = EditorGUILayout.ObjectField(T("BaseAvatar"), BaseAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            PhantomAvatar = EditorGUILayout.ObjectField(T("PhantomAvatar"), PhantomAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            IsRenameParameters = EditorGUILayout.ToggleLeft(T("RenameParameters"), IsRenameParameters);
            IsRemoveViewSystem = EditorGUILayout.ToggleLeft(T("RemoveViewSystem"), IsRemoveViewSystem);

            // Advanced Settings 
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, T("AdvancedSettings"), true);

            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                IsRemovePhantomMenu = EditorGUILayout.ToggleLeft(T("RemovePhantomMenu"), IsRemovePhantomMenu);
                IsRemovePhantomAvatarMA = EditorGUILayout.ToggleLeft(T("RemovePhantomAvatarMA"), IsRemovePhantomAvatarMA);
                IsRemoveOriginalAnimator = EditorGUILayout.ToggleLeft(T("RemoveOriginalAnimator"), IsRemoveOriginalAnimator);
                IsChangePBImmobileType = EditorGUILayout.ToggleLeft(T("ChangePBImmobileType"), IsChangePBImmobileType);
                IsUseRotationConstraint = EditorGUILayout.ToggleLeft(T("UseRotationConstraint"), IsUseRotationConstraint);
                if (IsUseRotationConstraint)
                {
                    EditorGUI.indentLevel++;
                    IsRotationSolveInWorldSpace = EditorGUILayout.ToggleLeft(T("RotationSolveInWorldSpace"), IsRotationSolveInWorldSpace);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button(T("StartButton")))
            {
                try
                {
                    Setup();
                    EditorUtility.DisplayDialog(T("SuccessTitle"), T("SuccessMessage"), T("OK"));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog(T("ErrorTitle"), T("ErrorMessage"), T("OK"));
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

            // Armature Constraint
            Transform PhantomArmature = PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            Transform BaseArmature = BaseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            // Change the Phantom Avatar Armature's name
            PhantomArmature.name = "Armature_phantom";
            var ArmatureConstraint = PhantomArmature.gameObject.AddComponent<VRCParentConstraint>();
            ArmatureConstraint.Locked = true;
            ArmatureConstraint.IsActive = false;
            ArmatureConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = BaseArmature,
                    Weight = 1f
                }
            };
            ArmatureConstraint.enabled = true;
            ArmatureConstraint.FreezeToWorld = true;

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
                    continue; // Skip LastBone
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

                            // PhantomOFF: constraint enable
                            PhantomOFF.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomPrepare: constraint enable
                            PhantomPrepare.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomFreezeOff: constraint enable
                            PhantomFreezeOff.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomFreeze: constraint disable
                            PhantomFreeze.SetCurve(PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
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

                            // PhantomOFF: constraint enable
                            PhantomOFF.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomPrepare: constraint enable
                            PhantomPrepare.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomFreezeOff: constraint enable
                            PhantomFreezeOff.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            // PhantomFreeze: constraint disable
                            PhantomFreeze.SetCurve(PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
                        }
                    }
                }
            }

            // delete existing Animation Clips And Controllers
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomOFF.anim");
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomPrepare.anim");
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomFreezeOff.anim");
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomFreeze.anim");
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            // check if folder exists, if not, create it
            if (!Directory.Exists($"{GeneratedAnimationFolder}/{BaseAvatar.name}"))
            {
                Directory.CreateDirectory($"{GeneratedAnimationFolder}/{BaseAvatar.name}");
            }
            // save animation clips
            AssetDatabase.CreateAsset(PhantomOFF, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomOFF.anim");
            AssetDatabase.CreateAsset(PhantomPrepare, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomPrepare.anim");
            AssetDatabase.CreateAsset(PhantomFreezeOff, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomFreezeOff.anim");
            AssetDatabase.CreateAsset(PhantomFreeze, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomFreeze.anim");

            // delete existing animator controller
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            // load reference animator controller
            AssetDatabase.CopyAsset(ReferenceAnimationPath, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomSystem_FX.controller");
            var PhantomController = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{GeneratedAnimationFolder}/{BaseAvatar.name}/PhantomSystem_FX.controller");
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
                var PhantomHip = PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).gameObject;
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
                if (!Directory.Exists($"{GeneratedMenuFolder}/{BaseAvatar.name}"))
                {
                    Directory.CreateDirectory($"{GeneratedMenuFolder}/{BaseAvatar.name}");
                }
                // delete existing menus
                string[] existingMenus = Directory.GetFiles($"{GeneratedMenuFolder}/{BaseAvatar.name}");
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
                        installer.installTargetMenu = CopyExpressionMenuRecursively(installer.installTargetMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }

                    if (installer.menuToAppend != null)
                    {
                        // Copy the menu to the PhantomSystem folder
                        installer.menuToAppend = CopyExpressionMenuRecursively(installer.menuToAppend, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }
                }

                // Rebase all MA Menu Items
                foreach (var item in MAMenuItems)
                {
                    if (item.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && item.MenuSource == SubmenuSource.MenuAsset)
                    {
                        // Copy the menu to the PhantomSystem folder
                        item.Control.subMenu = CopyExpressionMenuRecursively(item.Control.subMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }
                    // Rename parameters if MA Menu Item use custom parameters,
                    // because using identical parameter names in MA Menu Item across the phantom avatar and the base avatar can cause unexpected behavior.
                    // this may cause problem when using parameters in phantom avatar's FX, but no better solution for now.
                    if (!string.IsNullOrEmpty(item.Control.parameter.name))
                    {
                        var p = item.Control.parameter;
                        p.name = "PhantomSystemRename_" + p.name;
                        item.Control.parameter = p;
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
            GameObject MAPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(MAPrefab, PhantomSystem.transform);
            MAPrefabInstance.name = "PhantomMA";
            // redirect the Phantom Avatar Animator to the new controller
            MAPrefabInstance.GetComponent<ModularAvatarMergeAnimator>().animator = PhantomController;
            // Menu Localization
            switch (currentLocale)
            {
                case Locale.Chinese:
                    {
                        var MainMenu = IsRemovePhantomMenu ? AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenu_NoPhantomMenuPath_zh) : AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenuPath_zh);
                        MAPrefabInstance.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = MainMenu;
                        break;
                    }
                case Locale.English:
                    {
                        break;
                    }
                case Locale.Japanese:
                    {
                        var MainMenu = IsRemovePhantomMenu ? AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenu_NoPhantomMenuPath_jp) : AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenuPath_jp);
                        MAPrefabInstance.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = MainMenu;
                        break;
                    }
            }

            // View System
            if (!IsRemoveViewSystem)
            {
                // Add ViewSystem Prefab to PhantomSystem
                GameObject ViewSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IsRemovePhantomMenu ? ViewSystemPrefabPath_NoPhantomMenu : ViewSystemPrefabPath);
                GameObject ViewSystem = (GameObject)PrefabUtility.InstantiatePrefab(ViewSystemPrefab, PhantomSystem.transform);
                ViewSystem.name = "ViewSystem";

                // Set Armature MA Bone Proxy
                ModularAvatarBoneProxy ArmatureMA = ViewSystem.transform.Find("ArmatureMA").gameObject.GetComponent<ModularAvatarBoneProxy>();
                ArmatureMA.subPath = GetRelativePath(BaseArmature, BaseAvatar.transform);

                // Set ViewPoint
                GameObject BaseAvatarViewPoint = ViewSystem.transform.Find("BaseAvatarViewPoint").gameObject;
                BaseAvatarViewPoint.transform.position = BaseAvatar.ViewPosition;
                BaseAvatarViewPoint.transform.rotation = BaseAnimator.GetBoneTransform(HumanBodyBones.Head).rotation;
                GameObject PhantomAvatarViewPoint = ViewSystem.transform.Find("PhantomAvatarViewPoint").gameObject;
                PhantomAvatarViewPoint.transform.position = PhantomAvatar.ViewPosition;
                PhantomAvatarViewPoint.transform.rotation = PhantomAnimator.GetBoneTransform(HumanBodyBones.Head).rotation;

                // Set MA Bone Proxy for BaseAvatarViewPoint
                ModularAvatarBoneProxy PhantomViewPointProxy = PhantomAvatarViewPoint.GetComponent<ModularAvatarBoneProxy>();
                PhantomViewPointProxy.subPath = PhantomBonePaths[HumanBodyBones.Head];

                // Menu Localization
                switch (currentLocale)
                {
                    case Locale.Chinese:
                        {
                            VRCExpressionsMenu ViewMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalViewSysMenuPath_zh);
                            ViewSystem.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = ViewMenu;
                            ViewSystem.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ? 
                                AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenu_NoPhantomMenuPath_zh) : 
                                AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenuPath_zh);
                            break;
                        }
                    case Locale.English:
                        {
                            break;
                        }
                    case Locale.Japanese:
                        {
                            VRCExpressionsMenu ViewMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalViewSysMenuPath_jp);
                            ViewSystem.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = ViewMenu;
                            ViewSystem.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                                AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenu_NoPhantomMenuPath_jp) :
                                AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenuPath_jp);
                            break;
                        }
                }
            }

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
                        PhantomAvatarMAMenuInstaller.menuToAppend = CopyExpressionMenuRecursively(PhantomAvatar.expressionsMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                        PhantomAvatarMAMenuInstaller.installTargetMenu = PhantomMenu;
                    }
                }
            }

            // Setup GrabRoot
            GameObject GrabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrabPrefabPath);
            GameObject GrabRoot = (GameObject)PrefabUtility.InstantiatePrefab(GrabPrefab, PhantomSystem.transform);
            GrabRoot.name = "GrabRoot";
            // Set MA Bone Proxy for GrabRoot
            ModularAvatarBoneProxy GrabRootMA = GrabRoot.GetComponent<ModularAvatarBoneProxy>();
            GrabRootMA.subPath = GetRelativePath(PhantomArmature,BaseAvatar.transform);
            // Set Constraints for GrabRoot
            var GrabRootConstraint = GrabRoot.GetComponentInChildren<VRCParentConstraint>();
            GrabRootConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips),
                    Weight = 1f
                }
            };
            // Add Constraints for Phantom Avatar's Hips to follow GrabRoot when grabbed
            var PhantomHipsPositionConstraint = PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).gameObject.AddComponent<VRCPositionConstraint>();
            PhantomHipsPositionConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = GrabRootConstraint.transform,
                    Weight = 1f
                }
            };
            // Create Animation Clip for GrabRoot
            var GrabOn = new AnimationClip();
            var GrabOff = new AnimationClip();
            var GrabPrepare = new AnimationClip();
            // GrabOn: Disable Hips Parent Constraint
            GrabOn.SetCurve(GetRelativePath(PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
            // GrabOn: Enable Hips Position Constraint
            GrabOn.SetCurve(GetRelativePath(PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCPositionConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
            // GrabOn: Disable GrabRoot Constraint
            GrabOn.SetCurve(GetRelativePath(GrabRootConstraint.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));

            // GrabOff: Enable Hips Parent Constraint
            GrabOff.SetCurve(GetRelativePath(PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
            // GrabOff: Disable Hips Position Constraint
            GrabOff.SetCurve(GetRelativePath(PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCPositionConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
            // GrabOff: Enable GrabRoot Constraint
            GrabOff.SetCurve(GetRelativePath(GrabRootConstraint.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // delete existing Grab Animation Clips
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/GrabOn.anim");
            AssetDatabase.DeleteAsset($"{GeneratedAnimationFolder}/{BaseAvatar.name}/GrabOff.anim");
            // save Grab Animation Clips
            AssetDatabase.CreateAsset(GrabOn, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/GrabOn.anim");
            AssetDatabase.CreateAsset(GrabOff, $"{GeneratedAnimationFolder}/{BaseAvatar.name}/GrabOff.anim");

            // change controller's Grab animation clips
            var GrabStateMachine = PhantomController.layers[2].stateMachine;
            foreach (var state in GrabStateMachine.states)
            {
                if (state.state.name == "GrabOn")
                {
                    state.state.motion = GrabOn;
                }
                else if (state.state.name == "GrabOff")
                {
                    state.state.motion = GrabOff;
                }
            }
            EditorUtility.SetDirty(PhantomController);

            // Change PhysBone ImmobileType
            var PhantomPhysBones = PhantomAvatarRoot.GetComponentsInChildren<VRCPhysBone>(true);
            if (IsChangePBImmobileType)
            {
                foreach (var PB in PhantomPhysBones)
                {
                    PB.immobileType = VRCPhysBoneBase.ImmobileType.AllMotion;
                }
            }

            // Remove Phantom Avatar's existing components on root
            UnityEngine.Component[] PhantomAvatarOldComponents = PhantomAvatarRoot.GetComponents<UnityEngine.Component>();
            foreach (var component in PhantomAvatarOldComponents)
            {
                if (!(component is Transform || component is ModularAvatarMeshSettings))
                {
                    DestroyImmediate(component);
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
            // Create unique menu name
            // use GUID
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceMenu));
            string MenuRename = $"{sourceMenu.name}_{guid[..8]}";
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
            for (int i = 0; i < sourceMenu.controls.Count; i++)
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