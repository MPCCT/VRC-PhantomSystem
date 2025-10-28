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
        private const string GeneratedTempPrefabFolder = "Assets/MPCCT/PhantomSystem/~Generated";

        private bool showAdvanced = false;

        private HashSet<string> SavedMenuGUID = new HashSet<string>();

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

        private const string LocalViewSysMenuPath_zh = "Assets/MPCCT/PhantomSystem/ViewSystem/Menu/Menu_zh/ViewMain_zh.asset";
        private const string LocalViewSysMenuPath_jp = "Assets/MPCCT/PhantomSystem/ViewSystem/Menu/Menu_jp/ViewMain_jp.asset";

        private static readonly Dictionary<string, (string en, string zh, string jp)> s_texts = new Dictionary<string, (string, string, string)>
        {
            ["LanguageLabel"] = ("Language", "����", "���Z"),
            ["BaseAvatar"] = ("Base Avatar", "����ģ��", "�٩`�����Х��`"),
            ["PhantomAvatar"] = ("Phantom Avatar", "����ģ��", "�ե���ȥॢ�Х��`"),
            ["RenameParameters"] = ("Rename phantom avatar parameters", "����������ģ�͵Ĳ���", "�ե���ȥ�Υѥ��`�����ͩ`�ह��"),
            ["RemoveViewSystem"] = ("Remove phantom view window", "ȥ�������ӽǴ���", "�ե���ȥ��ҕ�㥦����ɥ�������"),
            ["AdvancedSettings"] = ("Advanced Settings", "�߼�����", "Ԕ���O��"),
            ["RemovePhantomMenu"] = ("Remove phantom avatar menu", "ȥ������ģ�Ͳ˵�", "�ե���ȥ��˥�`������"),
            ["RemovePhantomAvatarMA"] = ("Remove Modular Avatar components from phantom", "ȥ������ģ��MA���", "�ե���ȥ�� MA ����ݩ`�ͥ�Ȥ�����"),
            ["RemoveOriginalAnimator"] = ("Remove Phantom Avatar's original FX", "ȥ������ģ��ԭʼFX", "�ե���ȥ��Ԫ�Υ��˥�`���`������"),
            ["ChangePBImmobileType"] = ("Change PhysBone ImmobileType (may break some physbones)", "���ķ���ģ�Ͷ���ImmobileType�����ܻ�ʹ�����ϲ��ֶ����쳣��", "PhysBone��ImmobileType�������Ǥ��������Ϥ��꣩"),
            ["UseRotationConstraint"] = ("Use Rotation Constraint (useful when bone hierarchies differ)", "ʹ��Rotation Constraint������ģ�ͺͻ���ģ�͹�����ͬʱ�������ã�", "Rotation Constraint��ʹ�ã��ܩ`�󘋳ɤ����ʤ���Ϥ��Є���"),
            ["RotationSolveInWorldSpace"] = ("Solve constraint in world space (may affect facing direction)", "ʹ������ռ��ϵ�Լ��������ģ�Ͳ�����������ã��ᵼ��ģ���泯���򲻹̶������磩", "��`��ɿ��g�ǽ⤯���򤭤��̶�����ʤ����Ϥ��꣩"),
            ["StartButton"] = ("Setup!", "��ʼ���ã�", "���åȥ��åפ��_ʼ"),
            ["SuccessTitle"] = ("Success", "�ɹ�", "�ɹ�"),
            ["SuccessMessage"] = ("Setup completed!", "������ɣ�", "�O�������ˤ��ޤ�����"),
            ["ErrorTitle"] = ("Error!", "����!", "����`!"),
            ["ErrorMessage"] = ("An error occurred. See Console.", "���ִ�����鿴Console", "����`���k�����ޤ��������󥽩`���_�J���Ƥ���������"),
            ["OK"] = ("OK", "ȷ��", "OK")
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

        // Calss to hold context data between setup steps
        private class SetupContext
        {
            public GameObject PhantomSystem;
            public GameObject PhantomAvatarRoot;
            public Animator PhantomAnimator;
            public Animator BaseAnimator;
            public Transform PhantomArmature;
            public Transform BaseArmature;
            public Dictionary<HumanBodyBones, string> PhantomBonePaths = new Dictionary<HumanBodyBones, string>();
            public string PhantomAmaturePath;
        }

        // Class to hold animation clips and controller
        private class SetupAnimation
        {
            public AnimatorController PhantomController;
            public AnimationClip PhantomOFF = new AnimationClip();
            public AnimationClip PhantomPrepare = new AnimationClip();
            public AnimationClip PhantomFreezeOff = new AnimationClip();
            public AnimationClip PhantomFreeze = new AnimationClip();
            public AnimationClip GrabOn = new AnimationClip();
            public AnimationClip GrabOff = new AnimationClip();

            public void Save(string animFolderForAvatar, string referenceAnimationPath)
            {
                // create folder if not exists
                if (!Directory.Exists(animFolderForAvatar))
                {
                    Directory.CreateDirectory(animFolderForAvatar);
                }

                // delete existing assets if any
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PhantomOFF.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PhantomPrepare.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PhantomFreezeOff.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PhantomFreeze.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/GrabOn.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/GrabOff.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PhantomSystem_FX.controller");


                // create assets
                AssetDatabase.CreateAsset(PhantomOFF, $"{animFolderForAvatar}/PhantomOFF.anim");
                AssetDatabase.CreateAsset(PhantomPrepare, $"{animFolderForAvatar}/PhantomPrepare.anim");
                AssetDatabase.CreateAsset(PhantomFreezeOff, $"{animFolderForAvatar}/PhantomFreezeOff.anim");
                AssetDatabase.CreateAsset(PhantomFreeze, $"{animFolderForAvatar}/PhantomFreeze.anim");
                AssetDatabase.CreateAsset(GrabOn, $"{animFolderForAvatar}/GrabOn.anim");
                AssetDatabase.CreateAsset(GrabOff, $"{animFolderForAvatar}/GrabOff.anim");

                // copy reference controller and replace states
                AssetDatabase.CopyAsset(referenceAnimationPath, $"{animFolderForAvatar}/PhantomSystem_FX.controller");
                PhantomController = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{animFolderForAvatar}/PhantomSystem_FX.controller");

                // validation reference controller
                if (!(PhantomController != null && PhantomController.layers.Length > 2))
                {
                   throw new InvalidOperationException("[PhantomSystem] Invalid Reference Animator Controller");
                }

                var MainStateMachine = PhantomController.layers[1].stateMachine;
                var GrabStateMachine = PhantomController.layers[2].stateMachine;

                foreach (var state in MainStateMachine.states)
                {
                    if (state.state.name == "PhantomOFF") state.state.motion = PhantomOFF;
                    else if (state.state.name == "PhantomPrepare") state.state.motion = PhantomPrepare;
                    else if (state.state.name == "PhantomFreezeOff") state.state.motion = PhantomFreezeOff;
                    else if (state.state.name == "PhantomFreeze") state.state.motion = PhantomFreeze;
                }
                
                foreach (var state in GrabStateMachine.states)
                {
                    if (state.state.name == "GrabOn") state.state.motion = GrabOn;
                    else if (state.state.name == "GrabOff") state.state.motion = GrabOff;
                }
                EditorUtility.SetDirty(PhantomController);

                AssetDatabase.SaveAssets();
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
            EditorGUILayout.LabelField("PhantomSystem v0.2.1-alpha Made By MPCCT");

            // Language selection
            string[] localeOptions = new[] { "English", "����", "�ձ��Z" };
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
            SavedMenuGUID.Clear();
            var ctx = new SetupContext();
            var anim = new SetupAnimation();

            // Validation: BaseAvatar and PhantomAvatar must be set
            if (BaseAvatar == null || PhantomAvatar == null)
            {
                throw new InvalidOperationException("[PhantomSystem] Base Avatar and Phantom Avatar must be set.");
            }

            BaseAvatar.gameObject.SetActive(true);
            PhantomAvatar.gameObject.SetActive(false);

            Debug.Log($"[PhantomSystem] Setting up Phantom System for {BaseAvatar.name} using {PhantomAvatar.name} ...");

            DeleteExistingPhantomSystem();
            PhantomSystemInit(ctx);

            // Validation: Animator should be humanoid
            ctx.PhantomAnimator = ctx.PhantomAvatarRoot.GetComponent<Animator>();
            if (ctx.PhantomAnimator == null || ctx.PhantomAnimator.isHuman == false)
            {
                throw new InvalidOperationException("[PhantomSystem] Phantom Avatar must have a humanoid Animator component.");
            }

            ctx.BaseAnimator = BaseAvatar.GetComponent<Animator>();
            if (ctx.BaseAnimator == null || ctx.BaseAnimator.isHuman == false)
            {
                throw new InvalidOperationException("[PhantomSystem] Base Avatar must have a humanoid Animator component.");
            }

            SetupArmatureConstraint(ctx, anim);
            SetupBoneConstraints(ctx, anim);
            AdaptModularAvatar(ctx);
            SetupGrabRoot(ctx, anim);
            anim.Save($"{GeneratedAnimationFolder}/{BaseAvatar.name}", ReferenceAnimationPath);
            AddMAPrefab(ctx, anim);
            if (!IsRemoveViewSystem) SetupViewSystem(ctx);
            if (!IsRemoveOriginalAnimator) MergeOriginalAnimator(ctx);
            if (IsChangePBImmobileType) ChangePhysBoneImmobileType(ctx);
            DeletePhantomAvatarRootComponents(ctx);
        }

        private void DeleteExistingPhantomSystem()
        {
            var existingPhantomSystem = BaseAvatar.transform.Find("PhantomSystem");
            if (existingPhantomSystem != null)
            {
                Debug.Log("[PhantomSystem] Deleting existing Phantom System");
                DestroyImmediate(existingPhantomSystem.gameObject);
            }
        }

        private void PhantomSystemInit(SetupContext ctx)
        {
            ctx.PhantomSystem = new GameObject("PhantomSystem");
            ctx.PhantomSystem.transform.parent = BaseAvatar.transform;

            // Instantiate Phantom Avatar as a temporary prefab to keep prefab connection
            GameObject tempPrefab;
            string tempPrefabName = $"{PhantomAvatar.name}_tempPrefab_{PhantomAvatar.GetInstanceID()}.prefab";

            // create folder if not exists
            if (!Directory.Exists(GeneratedTempPrefabFolder))
            {
                Directory.CreateDirectory(GeneratedTempPrefabFolder);
            }

            tempPrefab = PrefabUtility.SaveAsPrefabAsset(PhantomAvatar.gameObject, $"{GeneratedTempPrefabFolder}/{tempPrefabName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var instanceAvatar = PrefabUtility.InstantiatePrefab(tempPrefab, ctx.PhantomSystem.transform) as GameObject;
            ctx.PhantomAvatarRoot = instanceAvatar;
            PrefabUtility.UnpackPrefabInstance(instanceAvatar, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            AssetDatabase.DeleteAsset($"{GeneratedTempPrefabFolder}/{tempPrefabName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ctx.PhantomAvatarRoot.transform.position = BaseAvatar.transform.position;
            ctx.PhantomAvatarRoot.transform.rotation = BaseAvatar.transform.rotation;

            ctx.PhantomAvatarRoot.name = "PhantomAvatar";
            ctx.PhantomAvatarRoot.SetActive(false);
        }

        private void SetupArmatureConstraint(SetupContext ctx, SetupAnimation anim)
        {
            ctx.PhantomArmature = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            ctx.BaseArmature = ctx.BaseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;

            // Change the Phantom Avatar Armature's name
            ctx.PhantomAmaturePath = GetRelativePath(ctx.PhantomArmature, ctx.PhantomAvatarRoot.transform);
            ctx.PhantomArmature.name = "Armature_phantom";
            var ArmatureConstraint = ctx.PhantomArmature.gameObject.AddComponent<VRCParentConstraint>();
            ArmatureConstraint.Locked = true;
            ArmatureConstraint.IsActive = false;
            ArmatureConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ctx.BaseArmature,
                    Weight = 1f
                }
            };
            ArmatureConstraint.enabled = true;
            ArmatureConstraint.FreezeToWorld = true;

            // PhantomOFF: deactivate PhantomAvatar
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 0));
            // PhantomOFF: Armature constraint freeze to world; disable the constraint
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));

            // PhantomPrepare: activate PhantomAvatar
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomPrepare: Armature constraint unfreeze world; enable the constraint
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreezeOff: activate PhantomAvatar
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreezeOff: Armature constraint freeze world; enable the constraint
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreeze: activate PhantomAvatar
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreeze: Armature constraint freeze world; enable the constraint
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
        }

        private void SetupBoneConstraints(SetupContext ctx, SetupAnimation anim)
        {
            HumanBodyBones[] humanBones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
            foreach (var bone in humanBones)
            {
                if (bone == HumanBodyBones.LastBone) continue;

                Transform PhantomBone = ctx.PhantomAnimator.GetBoneTransform(bone);
                Transform BaseBone = ctx.BaseAnimator.GetBoneTransform(bone);

                if (PhantomBone != null)
                {
                    // Save phantom bone path
                    ctx.PhantomBonePaths.Add(bone, GetRelativePath(PhantomBone, BaseAvatar.transform));
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
                            anim.PhantomOFF.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomPrepare.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomFreezeOff.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomFreeze.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
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
                            constraint.SolveInLocalSpace = !IsRotationSolveInWorldSpace;
                            constraint.IsActive = true;
                            constraint.TryBakeCurrentOffsets(VRCConstraintBase.BakeOptions.BakeOffsets); // Auto calculate offsets
                            constraint.Locked = true;

                            // Add keyframes to the Animation Clips
                            anim.PhantomOFF.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomPrepare.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomFreezeOff.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
                            anim.PhantomFreeze.SetCurve(ctx.PhantomBonePaths[bone], typeof(VRCRotationConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
                        }
                    }
                }
            }
        }

        private void AdaptModularAvatar(SetupContext ctx)
        {
            var MABoneProxy = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarBoneProxy>(true);

            var MAMaterialSetter = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMaterialSetter>(true);
            var MAMaterialSwap = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMaterialSwap>(true);
            var MAObjectToogle = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarObjectToggle>(true);
            var MAShapeChanger = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarShapeChanger>(true);
            var MAMeshCutter = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMeshCutter>(true);

            var MABlendshapeSync = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarBlendshapeSync>(true);
            var MAMergeArmature = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMergeArmature>(true);
            var MAMeshSettings = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMeshSettings>(true);
            var MAReplaceObject = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarReplaceObject>(true);

            var MAInstaller = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMenuInstaller>(true);
            var MAMenuItems = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMenuItem>(true);
            var MAParameter = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarParameters>(true);
            var MAMergeAnimator = ctx.PhantomAvatarRoot.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);

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
                    proxy.subPath = ctx.PhantomBonePaths[proxy.boneReference];
                    proxy.boneReference = HumanBodyBones.LastBone;
                }

                EditorUtility.SetDirty(proxy);
                if (PrefabUtility.IsPartOfPrefabInstance(proxy))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(proxy);
            }

            // MA blendshape sync adaption
            foreach (var sync in MABlendshapeSync)
            {
                BlendshapeBinding[] newBindings = new BlendshapeBinding[sync.Bindings.Count];
                for (int i = 0; i < sync.Bindings.Count; i++)
                {
                    BlendshapeBinding tempBind = new BlendshapeBinding();
                    tempBind.LocalBlendshape = sync.Bindings[i].LocalBlendshape;
                    tempBind.Blendshape = sync.Bindings[i].Blendshape;
                    tempBind.ReferenceMesh = RebaseAvatarObjectReference(ctx, sync.Bindings[i].ReferenceMesh);
                    newBindings[i] = tempBind;
                }
                sync.Bindings = newBindings.ToList();

                EditorUtility.SetDirty(sync);
                if (PrefabUtility.IsPartOfPrefabInstance(sync))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sync);
            }

            // MA material setter adaption
            foreach (var setter in MAMaterialSetter)
            {
                MaterialSwitchObject[] newObjs = new MaterialSwitchObject[setter.Objects.Count];
                for (int i = 0; i < setter.Objects.Count; i++)
                {
                    MaterialSwitchObject tempObj = new MaterialSwitchObject();
                    tempObj.Material = setter.Objects[i].Material;
                    tempObj.MaterialIndex = setter.Objects[i].MaterialIndex;
                    tempObj.Object = RebaseAvatarObjectReference(ctx, setter.Objects[i].Object);
                    newObjs[i] = tempObj;
                }
                setter.Objects = newObjs.ToList();

                EditorUtility.SetDirty(setter);
                if (PrefabUtility.IsPartOfPrefabInstance(setter))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(setter);
            }

            // MA material swap adaption
            foreach (var swap in MAMaterialSwap)
            {
                swap.Root = RebaseAvatarObjectReference(ctx, swap.Root);

                EditorUtility.SetDirty(swap);
                if (PrefabUtility.IsPartOfPrefabInstance(swap))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(swap);
            }

            // MA object toggle adaption
            foreach (var toggle in MAObjectToogle)
            {
                ToggledObject[] newToggles = new ToggledObject[toggle.Objects.Count];
                for (int i = 0; i < toggle.Objects.Count; i++)
                {
                    ToggledObject tempObj = new ToggledObject();
                    tempObj.Active = toggle.Objects[i].Active;
                    tempObj.Object = RebaseAvatarObjectReference(ctx, toggle.Objects[i].Object);
                    newToggles[i] = tempObj;
                }
                toggle.Objects = newToggles.ToList();

                EditorUtility.SetDirty(toggle);
                if (PrefabUtility.IsPartOfPrefabInstance(toggle))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(toggle);
            }

            // MA shape changer adaption
            foreach (var changer in MAShapeChanger)
            {
                ChangedShape[] newShapes = new ChangedShape[changer.Shapes.Count];
                for (int i = 0; i < changer.Shapes.Count; i++)
                {
                    ChangedShape tempShape = new ChangedShape();
                    tempShape.ShapeName = changer.Shapes[i].ShapeName;
                    tempShape.ChangeType = changer.Shapes[i].ChangeType;
                    tempShape.Value = changer.Shapes[i].Value;
                    tempShape.Object = RebaseAvatarObjectReference(ctx, changer.Shapes[i].Object);
                    newShapes[i] = tempShape;
                }
                changer.Shapes = newShapes.ToList();

                EditorUtility.SetDirty(changer);
                if (PrefabUtility.IsPartOfPrefabInstance(changer))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(changer);
            }

            // MA mesh cutter adaption
            foreach (var cutter in MAMeshCutter)
            {
                cutter.Object = RebaseAvatarObjectReference(ctx, cutter.Object);

                EditorUtility.SetDirty(cutter);
                if (PrefabUtility.IsPartOfPrefabInstance(cutter))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(cutter);
            }

            // MA merge armature adaption
            foreach (var armature in MAMergeArmature)
            {
                armature.mergeTarget = RebaseAvatarObjectReference(ctx, armature.mergeTarget);

                EditorUtility.SetDirty(armature);
                if (PrefabUtility.IsPartOfPrefabInstance(armature))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(armature);
            }

            // MA mesh settings adaption
            foreach (var setting in MAMeshSettings)
            {
                setting.ProbeAnchor = RebaseAvatarObjectReference(ctx, setting.ProbeAnchor);
                setting.RootBone = RebaseAvatarObjectReference(ctx, setting.RootBone);

                EditorUtility.SetDirty(setting);
                if (PrefabUtility.IsPartOfPrefabInstance(setting))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(setting);
            }

            // MA replace object adaption
            foreach (var replace in MAReplaceObject)
            {
                replace.targetObject = RebaseAvatarObjectReference(ctx, replace.targetObject);

                EditorUtility.SetDirty(replace);
                if (PrefabUtility.IsPartOfPrefabInstance(replace))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(replace);
            }

            if (IsRemovePhantomAvatarMA)
            {
                // remove the Phantom Avatar's MA components
                foreach (var installer in MAInstaller) DestroyImmediate(installer);
                foreach (var item in MAMenuItems) DestroyImmediate(item);
                foreach (var parameter in MAParameter) DestroyImmediate(parameter);
                foreach (var animator in MAMergeAnimator) DestroyImmediate(animator);
            }
            else
            {
                // Rebase MA menus and parameters
                if (!Directory.Exists($"{GeneratedMenuFolder}/{BaseAvatar.name}"))
                {
                    Directory.CreateDirectory($"{GeneratedMenuFolder}/{BaseAvatar.name}");
                }
                string[] existingMenus = Directory.GetFiles($"{GeneratedMenuFolder}/{BaseAvatar.name}");
                foreach (var existingmenu in existingMenus) AssetDatabase.DeleteAsset(existingmenu);

                foreach (var installer in MAInstaller)
                {
                    if (installer.installTargetMenu == null || installer.installTargetMenu == BaseAvatar.expressionsMenu)
                    {
                        installer.installTargetMenu = PhantomMenu;
                    }
                    else
                    {
                        installer.installTargetMenu = CopyExpressionMenuRecursively(installer.installTargetMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }

                    if (installer.menuToAppend != null)
                    {
                        installer.menuToAppend = CopyExpressionMenuRecursively(installer.menuToAppend, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }

                    EditorUtility.SetDirty(installer);
                    if (PrefabUtility.IsPartOfPrefabInstance(installer))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
                }

                foreach (var item in MAMenuItems)
                {
                    if (item.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && item.MenuSource == SubmenuSource.MenuAsset)
                    {
                        item.Control.subMenu = CopyExpressionMenuRecursively(item.Control.subMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    }
                    if (!string.IsNullOrEmpty(item.Control.parameter.name))
                    {
                        var p = item.Control.parameter;
                        p.name = "PhantomSystemRename_" + p.name;
                        item.Control.parameter = p;
                    }

                    EditorUtility.SetDirty(item);
                    if (PrefabUtility.IsPartOfPrefabInstance(item))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                }

                foreach (var animator in MAMergeAnimator)
                {
                    if (animator.pathMode == MergeAnimatorPathMode.Absolute)
                    {
                        animator.pathMode = MergeAnimatorPathMode.Relative;
                        AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                        tempRelativePathRoot.Set(ctx.PhantomAvatarRoot);
                        animator.relativePathRoot = tempRelativePathRoot;
                    }

                    EditorUtility.SetDirty(animator);
                    if (PrefabUtility.IsPartOfPrefabInstance(animator))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                }

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

                        EditorUtility.SetDirty(parameter);
                        if (PrefabUtility.IsPartOfPrefabInstance(parameter))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(parameter);
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
        }

        private void AddMAPrefab(SetupContext ctx, SetupAnimation anim)
        {
            GameObject MAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IsRemovePhantomMenu ? MAPrefabPath_NoPhantomMenu : MAPrefabPath);
            GameObject MAPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(MAPrefab, ctx.PhantomSystem.transform);
            MAPrefabInstance.name = "PhantomMA";
            MAPrefabInstance.GetComponent<ModularAvatarMergeAnimator>().animator = anim.PhantomController;

            // Localize Menu
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
        }

        private void SetupViewSystem(SetupContext ctx)
        {
            GameObject ViewSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IsRemovePhantomMenu ? ViewSystemPrefabPath_NoPhantomMenu : ViewSystemPrefabPath);
            GameObject ViewSystem = (GameObject)PrefabUtility.InstantiatePrefab(ViewSystemPrefab, ctx.PhantomSystem.transform);
            ViewSystem.name = "ViewSystem";

            ModularAvatarBoneProxy ArmatureMA = ViewSystem.transform.Find("ArmatureMA").gameObject.GetComponent<ModularAvatarBoneProxy>();
            ArmatureMA.subPath = GetRelativePath(ctx.BaseArmature, BaseAvatar.transform);

            GameObject BaseAvatarViewPoint = ViewSystem.transform.Find("BaseAvatarViewPoint").gameObject;
            BaseAvatarViewPoint.transform.position = BaseAvatar.ViewPosition;
            BaseAvatarViewPoint.transform.rotation = ctx.BaseAnimator.GetBoneTransform(HumanBodyBones.Head).rotation;
            GameObject PhantomAvatarViewPoint = ViewSystem.transform.Find("PhantomAvatarViewPoint").gameObject;
            PhantomAvatarViewPoint.transform.position = PhantomAvatar.ViewPosition;
            PhantomAvatarViewPoint.transform.rotation = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Head).rotation;

            ModularAvatarBoneProxy PhantomAvatarViewPointMA = PhantomAvatarViewPoint.GetComponent<ModularAvatarBoneProxy>();
            PhantomAvatarViewPointMA.subPath = ctx.PhantomBonePaths[HumanBodyBones.Head];
            PhantomAvatarViewPointMA.boneReference = HumanBodyBones.LastBone;

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

        private void MergeOriginalAnimator(SetupContext ctx)
        {
            GameObject PhantomAvatarMA = new GameObject("PhantomOriginalFX_MA");
            PhantomAvatarMA.transform.parent = ctx.PhantomSystem.transform;

            if (PhantomAvatar.customizeAnimationLayers)
            {
                var PhantomAvatarMAMergeAnimator = PhantomAvatarMA.AddComponent<ModularAvatarMergeAnimator>();
                PhantomAvatarMAMergeAnimator.animator = PhantomAvatar.baseAnimationLayers[4].animatorController;
                PhantomAvatarMAMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                PhantomAvatarMAMergeAnimator.deleteAttachedAnimator = true;
                PhantomAvatarMAMergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
                AvatarObjectReference tempRelativePathRoot = new AvatarObjectReference();
                tempRelativePathRoot.Set(ctx.PhantomAvatarRoot);
                PhantomAvatarMAMergeAnimator.relativePathRoot = tempRelativePathRoot;
                PhantomAvatarMAMergeAnimator.matchAvatarWriteDefaults = true;
                PhantomAvatarMAMergeAnimator.layerPriority = -1;
            }

            if (PhantomAvatar.customExpressions)
            {
                var PhantomAvatarMAParameters = PhantomAvatarMA.AddComponent<ModularAvatarParameters>();
                PhantomAvatarMAParameters.parameters = GetParameterFromVRCParameter(PhantomAvatar.expressionParameters, IsRenameParameters);
            }

            if (!IsRemovePhantomMenu)
            {
                var PhantomAvatarMAMenuInstaller = PhantomAvatarMA.AddComponent<ModularAvatarMenuInstaller>();
                if (PhantomAvatar.expressionsMenu != null)
                {
                    PhantomAvatarMAMenuInstaller.menuToAppend = CopyExpressionMenuRecursively(PhantomAvatar.expressionsMenu, $"{GeneratedMenuFolder}/{BaseAvatar.name}");
                    PhantomAvatarMAMenuInstaller.installTargetMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PhantomMenuPath);
                }
            }
        }

        private void SetupGrabRoot(SetupContext ctx, SetupAnimation anim)
        {
            GameObject GrabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrabPrefabPath);
            GameObject GrabRoot = (GameObject)PrefabUtility.InstantiatePrefab(GrabPrefab, ctx.PhantomSystem.transform);
            GrabRoot.name = "GrabRoot";

            ModularAvatarBoneProxy GrabRootMA = GrabRoot.GetComponent<ModularAvatarBoneProxy>();
            GrabRootMA.subPath = GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform);

            var GrabRootConstraint = GrabRoot.GetComponentInChildren<VRCParentConstraint>();
            GrabRootConstraint.Locked = true;
            GrabRootConstraint.IsActive = true;
            GrabRootConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips),
                    Weight = 1f
                }
            };
            GrabRootConstraint.enabled = true;

            var PhantomHipsPositionConstraint = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).gameObject.AddComponent<VRCPositionConstraint>();
            PhantomHipsPositionConstraint.Locked = true;
            PhantomHipsPositionConstraint.IsActive = false;
            PhantomHipsPositionConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = GrabRootConstraint.transform,
                    Weight = 1f
                }
            };
            PhantomHipsPositionConstraint.enabled = true;

            // Add keyframes to the Animation Clips
            // GrabOn: Disable Hips Parent Constraint
            anim.GrabOn.SetCurve(GetRelativePath(ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
            // GrabOn: Enable Hips Position Constraint
            anim.GrabOn.SetCurve(GetRelativePath(ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCPositionConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
            // GrabOn: Disable GrabRoot Constraint
            anim.GrabOn.SetCurve(GetRelativePath(GrabRootConstraint.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));

            // GrabOff: Enable Hips Parent Constraint
            anim.GrabOff.SetCurve(GetRelativePath(ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
            // GrabOff: Disable Hips Position Constraint
            anim.GrabOff.SetCurve(GetRelativePath(ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips), BaseAvatar.transform), typeof(VRCPositionConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));
            // GrabOff: Enable GrabRoot Constraint
            anim.GrabOff.SetCurve(GetRelativePath(GrabRootConstraint.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));
        }

        private void ChangePhysBoneImmobileType(SetupContext ctx)
        {
            var PhantomPhysBones = ctx.PhantomAvatarRoot.GetComponentsInChildren<VRCPhysBone>(true);
            foreach (var PB in PhantomPhysBones)
            {
                PB.immobileType = VRCPhysBoneBase.ImmobileType.AllMotion;
            }
        }

        private void DeletePhantomAvatarRootComponents(SetupContext ctx)
        {
            UnityEngine.Component[] PhantomAvatarOldComponents = ctx.PhantomAvatarRoot.GetComponents<UnityEngine.Component>();
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
            if (SavedMenuGUID.Contains(guid))
            {
                return AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>($"{path}/{MenuRename}.asset");
            }
            SavedMenuGUID.Add(guid);

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
                    var newSubMenu = CopyExpressionMenuRecursively(c.subMenu, path);
                    c.subMenu = newSubMenu;
                    controls[i] = c; // write back
                }
            }
            EditorUtility.SetDirty(copiedMenu);
            AssetDatabase.SaveAssetIfDirty(copiedMenu);
            return copiedMenu;
        }

        private AvatarObjectReference RebaseAvatarObjectReference(SetupContext ctx, AvatarObjectReference obj)
        {
            string newPath;
            AvatarObjectReference newObj = new AvatarObjectReference();

            if (!obj.referencePath.StartsWith("PhantomSystem/PhantomAvatar/"))
            {
                newPath = obj.referencePath;
                if (obj.referencePath.StartsWith(ctx.PhantomAmaturePath))
                {
                    var NewArmaturePath = GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform);
                    newPath = NewArmaturePath + newPath.Substring(ctx.PhantomAmaturePath.Length);
                }
                else
                {
                    newPath = "PhantomSystem/PhantomAvatar/" + newPath;
                }
                newObj.Set(BaseAvatar.transform.Find(newPath).gameObject);
                return newObj;
            }
            return obj;
        }
    }
}