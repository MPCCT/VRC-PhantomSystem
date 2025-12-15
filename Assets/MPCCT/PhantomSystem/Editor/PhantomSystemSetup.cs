using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace MPCCT
{
    public class PhantomSystemSetup : EditorWindow
    {
        #region --- Variables ---
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

        private List<ModularAvatarParameters> ExceptionParameters = new List<ModularAvatarParameters>();

        // GUI state
        private bool showExceptions = false;
        private bool showAdvanced = false;
        private bool showUnsupportedComponents = false;
        private Vector2 exceptionScrollPosition = Vector2.zero;
        private Vector2 componentsScrollPosition = Vector2.zero;

        private HashSet<string> SavedMenuGUID = new HashSet<string>();

        private string GeneratedAnimationPath;
        private string GeneratedMenuPath;

        private enum Locale { English = 0, Chinese = 1, Japanese = 2 }
        private Locale currentLocale = Locale.English;
        #endregion

        #region --- Constants & Texts ---
        private const string MAPrefabPath = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA.prefab";
        private const string MAPrefabPath_NoPhantomMenu = "Assets/MPCCT/PhantomSystem/Prefab/PhantomMA_NoPhantomMenu.prefab";
        private const string ReferenceAnimationPath = "Assets/MPCCT/PhantomSystem/Animation/PhantomSystem_FX_Reference.controller";
        private const string PhantomMenuPath = "Assets/MPCCT/PhantomSystem/Menu/PhantomSystemPhantomMenu.asset";
        private const string ViewSystemPrefabPath = "Assets/MPCCT/PhantomSystem/ViewSystem/Prefab/PhantomView.prefab";
        private const string ViewSystemPrefabPath_NoPhantomMenu = "Assets/MPCCT/PhantomSystem/ViewSystem/Prefab/PhantomView_NoPhantomMenu.prefab";
        private const string GrabPrefabPath = "Assets/MPCCT/PhantomSystem/Prefab/GrabRoot.prefab";

        private const string GeneratedMainFolder = "Assets/MPCCT/PhantomSystem/~Generated";

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
            ["LanguageLabel"] = ("Language", "语言", "言語"),
            ["BaseAvatar"] = ("Base Avatar", "基础模型", "ベースアバター"),
            ["PhantomAvatar"] = ("Phantom Avatar", "分身模型", "ファントムアバター"),
            ["RenameParameters"] = ("Rename phantom avatar parameters", "重命名分身模型的参数", "ファントムアバターのパラメータをリネームする"),
            ["Exceptions"] = ("Exceptions", "例外项", "例外"),
            ["ExceptionsMessage"] = ("The following Modular Avatar Parameters will NOT be renamed.", "以下Modular Avatar Parameters将不会被重命名。", "以下のModular Avatar Parametersはリネームされません。"),
            ["ChooseAllParameters"] = ("Choose All Modular Avatar Parameters", "选择所有Modular Avatar Parameters", "すべてのModular Avatar Parametersを選択"),
            ["DragMessage"] = ("Drag Modular Avatar Parameters here to add to exceptions", "将Modular Avatar Parameters拖拽到此处以添加到例外项", "Modular Avatar Parametersをここにドラッグして例外に追加"),
            ["RemoveViewSystem"] = ("Remove phantom view window", "去除分身视角窗口", "ファントムのビューウィンドウを削除"),
            ["AdvancedSettings"] = ("Advanced Settings", "高级设置", "詳細設定"),
            ["RemovePhantomMenu"] = ("Remove phantom avatar menu", "去除分身模型菜单", "ファントムアバターメニューを削除"),
            ["RemovePhantomAvatarMA"] = ("Remove Modular Avatar components from phantom", "去除分身模型MA组件", "ファントムのModular Avatarコンポーネントを削除"),
            ["RemoveOriginalAnimator"] = ("Remove Phantom Avatar's original FX", "去除分身模型原始FX", "ファントムアバターの元のFXを削除"),
            ["ChangePBImmobileType"] = ("Change PhysBone ImmobileType (may break some physbones)", "更改分身模型动骨ImmobileType（可能会使分身上部分动骨异常）", "PhysBoneのImmobileTypeを変更（一部物理ボーンが正常に動作しなくなる可能性あり）"),
            ["UseRotationConstraint"] = ("Use Rotation Constraint (useful when bone hierarchies differ)", "使用Rotation Constraint（分身模型和基础模型骨骼不同时可能有用）", "回転制約を使用（ボーン階層が異なる場合に有効）"),
            ["RotationSolveInWorldSpace"] = ("Solve constraint in world space (may affect facing direction)", "使用世界空间上的约束（对于模型不适配可能有用，会导致模型面朝方向不固定在世界）", "ワールド空間で制約を解決（向きが固定されなくなる可能性あり）"),
            ["StartButton"] = ("Setup!", "开始配置！", "セットアップ開始！"),
            ["SuccessTitle"] = ("Success", "成功", "成功"),
            ["SuccessMessage"] = ("Setup completed!", "配置完成！", "セットアップが完了しました！"),
            ["ErrorTitle"] = ("Error!", "错误!", "エラー！"),
            ["ErrorMessage"] = ("An error occurred. See Console.", "出现错误，请查看Console", "エラーが発生しました。コンソールを確認してください。"),
            ["OK"] = ("OK", "确定", "OK"),
            ["BaseAvatarValidationError"] = ("Base Avatar must be set.", "未设置基础模型", "ベースアバターを設定する必要があります"),
            ["PhantomAvatarValidationError"] = ("Phantom Avatar must be set.", "未设置分身模型", "ファントムアバターを設定する必要があります"),
            ["BaseAvatarAnimatorNotFound"] = ("Base Avatar's animator component not found", "未能找到基础模型的Animator组件", "ベースアバターのアニメーターコンポーネントが見つかりません"),
            ["PhantomAvatarAnimatorNotFound"] = ("Phantom Avatar's animator component not found", "未能找到分身模型的Animator组件", "ファントムアバターのアニメーターコンポーネントが見つかりません"),
            ["BaseAvatarAnimatorError"] = ("Base Avatar must be humanoid.", "基础模型需为humanoid", "ベースアバターはヒューマノイドである必要があります"),
            ["PhantomAvatarAnimatorError"] = ("Phantom Avatar must be humanoid.", "分身模型需为humanoid", "ファントムアバターはヒューマノイドである必要があります"),
            ["ReferenceControllerNotFound"] = ("Reference animation controller not found. Please reinstall PhantomSystem", "未找到参考动画控制器。请重装PhantomSystem", "参照用アニメーションコントローラーが見つかりません。PhantomSystemを再インストールしてください"),
            ["ReferenceControllerError"] = ("Reference animation controller is broken. Please reinstall PhantomSystem", "参考动画控制器损坏。请重装PhantomSystem", "参照用アニメーションコントローラーが壊れています。PhantomSystemを再インストールしてください"),
            ["UnsupportedComponentsWarning"] = ("Unsupported components found on Phantom Avatar. This may cause some issues.", "分身模型上检测到不支持的组件。这可能会导致一些问题。", "ファントムアバターにサポートされていないコンポーネントが見つかりました。問題が発生する可能性があります"),
            ["ShowUnsupportedComponents"] = ("Unsupported Components", "不支持的组件", "サポート外コンポーネント")
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

        private static readonly Type[] ComponentsWhiteList = new Type[]
        { 
            // VRC
            typeof(VRCAvatarDescriptor),
            typeof(VRCPositionConstraint),
            typeof(VRCParentConstraint),
            typeof(VRCAimConstraint),
            typeof(VRCParentConstraint),
            typeof(VRCRotationConstraint),
            typeof(VRCScaleConstraint),
            typeof(VRCContactReceiver),
            typeof(VRCContactSender),
            typeof(VRCHeadChop),
            typeof(VRCPhysBone),
            typeof(VRCPhysBoneCollider),
            typeof(PipelineManager),
            typeof(VRCSpatialAudioSource),
            typeof(VRCStation),

            // Unity
            typeof(Transform),
            typeof(Animator),
            typeof(Animation),
            typeof(AudioSource),
            typeof(Camera),
            typeof(Cloth),
            typeof(Collider),
            typeof(Joint),
            typeof(FlareLayer),
            typeof(Light),
            typeof(LineRenderer),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(ParticleSystem),
            typeof(ParticleSystemRenderer),
            typeof(TrailRenderer),
            typeof(Rigidbody),

            // Unity Animation Rigging
            typeof(AimConstraint),
            typeof(LookAtConstraint),
            typeof(ParentConstraint),
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ScaleConstraint),
        };
        #endregion

        #region --- Classes ---
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
            public List<ModularAvatarParameters> ExceptionParameters = new List<ModularAvatarParameters>();
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
            public AnimationClip PositionLockOn = new AnimationClip();
            public AnimationClip PositionLockOff = new AnimationClip();
            public AnimationClip PositionLockPrepare = new AnimationClip();

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
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PositionLockOn.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PositionLockOff.anim");
                AssetDatabase.DeleteAsset($"{animFolderForAvatar}/PositionLockPrepare.anim");


                // create assets
                AssetDatabase.CreateAsset(PhantomOFF, $"{animFolderForAvatar}/PhantomOFF.anim");
                AssetDatabase.CreateAsset(PhantomPrepare, $"{animFolderForAvatar}/PhantomPrepare.anim");
                AssetDatabase.CreateAsset(PhantomFreezeOff, $"{animFolderForAvatar}/PhantomFreezeOff.anim");
                AssetDatabase.CreateAsset(PhantomFreeze, $"{animFolderForAvatar}/PhantomFreeze.anim");
                AssetDatabase.CreateAsset(GrabOn, $"{animFolderForAvatar}/GrabOn.anim");
                AssetDatabase.CreateAsset(GrabOff, $"{animFolderForAvatar}/GrabOff.anim");
                AssetDatabase.CreateAsset(PositionLockOn, $"{animFolderForAvatar}/PositionLockOn.anim");
                AssetDatabase.CreateAsset(PositionLockOff, $"{animFolderForAvatar}/PositionLockOff.anim");
                AssetDatabase.CreateAsset(PositionLockPrepare, $"{animFolderForAvatar}/PositionLockPrepare.anim");

                // copy reference controller and replace states
                AssetDatabase.CopyAsset(referenceAnimationPath, $"{animFolderForAvatar}/PhantomSystem_FX.controller");
                PhantomController = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{animFolderForAvatar}/PhantomSystem_FX.controller");

                var MainStateMachine = PhantomController.layers[0].stateMachine;
                var PositionLockStateMachine = PhantomController.layers[1].stateMachine;
                var GrabStateMachine = PhantomController.layers[2].stateMachine;

                foreach (var state in MainStateMachine.states)
                {
                    if (state.state.name == "PhantomOFF") state.state.motion = PhantomOFF;
                    else if (state.state.name == "PhantomPrepare") state.state.motion = PhantomPrepare;
                    else if (state.state.name == "PhantomFreezeOff") state.state.motion = PhantomFreezeOff;
                    else if (state.state.name == "PhantomFreeze") state.state.motion = PhantomFreeze;
                }
                
                foreach (var state in PositionLockStateMachine.states)
                {
                    if (state.state.name == "PositionLockOn") state.state.motion = PositionLockOn;
                    else if (state.state.name == "PositionLockOff") state.state.motion = PositionLockOff;
                    else if (state.state.name == "PositionLockPrepare") state.state.motion = PositionLockPrepare;
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
        #endregion 

        #region --- GUI & Validation---
        private void OnEnable()
        {
            // Load saved locale
            currentLocale = (Locale)EditorPrefs.GetInt("MPCCT_PhantomSystem_Locale", (int)Locale.English);
        }

        [MenuItem("MPCCT/PhantomSystemSetup")]
        private static void Init()
        {
            var window = GetWindowWithRect<PhantomSystemSetup>(new Rect(0, 0, 500, 500));
            window.minSize = new Vector2(200, 200);
            window.maxSize = new Vector2(1000, 1000);
            window.Show();
        }

        private void OnGUI()
        {
            // Title
            EditorGUILayout.LabelField("PhantomSystem v0.2.8-alpha Made By MPCCT");

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

            // MA Parameters Exceptions list
            if (IsRenameParameters && PhantomAvatar != null)
            {
                EditorGUI.indentLevel++;
                showExceptions = EditorGUILayout.Foldout(showExceptions, T("Exceptions"), true);
                if (showExceptions)
                {
                    EditorGUILayout.HelpBox(T("ExceptionsMessage"), MessageType.Info);
                    exceptionScrollPosition = EditorGUILayout.BeginScrollView(exceptionScrollPosition, GUILayout.Height(100));
                    for(int i = 0;i < ExceptionParameters.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        ExceptionParameters[i] = (ModularAvatarParameters)EditorGUILayout.ObjectField(ExceptionParameters[i], typeof(ModularAvatarParameters), true);
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            ExceptionParameters.RemoveAt(i);
                            i--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(T("DragMessage"));
                    ModularAvatarParameters newParameter = (ModularAvatarParameters)EditorGUILayout.ObjectField(null, typeof(ModularAvatarParameters), true);
                    if (newParameter != null && !ExceptionParameters.Contains(newParameter) 
                        && newParameter.transform.IsChildOf(PhantomAvatar.transform))
                    {
                        ExceptionParameters.Add(newParameter);
                        Repaint();
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 15);
                    if (GUILayout.Button(T("ChooseAllParameters")))
                    {
                        ExceptionParameters = PhantomAvatar.GetComponentsInChildren<ModularAvatarParameters>(true).ToList();
                        Repaint();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

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

            // Validation
            var validationErrors = ValidateSetup();
            foreach (var error in validationErrors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            // Unsupported components warning
            if (PhantomAvatar != null)
            {
                var inValidComponents = ValidateComponents(PhantomAvatar);
                if (inValidComponents.Count > 0)
                {
                    EditorGUILayout.HelpBox(T("UnsupportedComponentsWarning"), MessageType.Warning);
                    showUnsupportedComponents = EditorGUILayout.Foldout(showUnsupportedComponents, T("ShowUnsupportedComponents"), true);
                    if (showUnsupportedComponents)
                    {
                        EditorGUI.indentLevel++;
                        componentsScrollPosition = EditorGUILayout.BeginScrollView(componentsScrollPosition, GUILayout.Height(100));
                        EditorGUI.BeginDisabledGroup(true);
                        foreach (var compObj in inValidComponents)
                        {
                            EditorGUILayout.ObjectField(compObj.name, compObj, typeof(GameObject), true);
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel--;
                    }
                }    
            }
                
            // Start button
            EditorGUI.BeginDisabledGroup(validationErrors.Count > 0);
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
            EditorGUI.EndDisabledGroup();
        }

        private List<string> ValidateSetup()
        {
            var errors = new List<string>();

            // Basic checks
            if (BaseAvatar == null)
            {
                errors.Add(T("BaseAvatarValidationError"));
            }
            if (PhantomAvatar == null)
            {
                errors.Add(T("PhantomAvatarValidationError"));
            }

            // Animators and humanoid
            if (BaseAvatar != null)
            {
                var baseAnimator = BaseAvatar.GetComponent<Animator>();
                if (baseAnimator == null)
                {
                    errors.Add(T("BaseAvatarAnimatorNotFound"));
                }
                else if (!baseAnimator.isHuman)
                {
                    errors.Add(T("BaseAvatarAnimatorError"));
                }
            }

            if (PhantomAvatar != null)
            {
                var phantomAnimator = PhantomAvatar.GetComponent<Animator>();
                if (phantomAnimator == null)
                {
                    errors.Add(T("PhantomAvatarAnimatorNotFound"));
                }
                else if(!phantomAnimator.isHuman)
                {
                    errors.Add(T("PhantomAvatarAnimatorError"));
                }
            }

            // Reference animation controller validity
            var refController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ReferenceAnimationPath);
            if (refController == null)
            {
                errors.Add(T("ReferenceControllerNotFound"));
            }
            else if (refController.layers == null || refController.layers.Length < 3)
            {
                errors.Add(T("ReferenceControllerError"));
            }

            return errors;
        }

        private List<Component> ValidateComponents(VRCAvatarDescriptor avatar)
        {
            var MAAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "nadena.dev.modular-avatar.core");
            var MAComponentList = MAAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MonoBehaviour))).ToArray();
            var invalidComponents = new List<Component>();
            var allComponents = avatar.GetComponentsInChildren<Component>(true);
            foreach(var component in  allComponents)
            {
                // Check if component is on the arvatar root
                if (component.gameObject != avatar.gameObject)
                {
                    // Check for whitelist
                    if (ComponentsWhiteList.Any(t => t == component.GetType() || t.IsAssignableFrom(component.GetType())))
                    {
                        continue;
                    }
                    // Check for MA components
                    if (MAComponentList.Any(t => t == component.GetType() || t.IsAssignableFrom(component.GetType())))
                    {
                        continue;
                    }
                    invalidComponents.Add(component);
                }
            }
            return invalidComponents;
        }
        #endregion

        #region --- Setup Steps ---
        private void Setup()
        {
            SavedMenuGUID.Clear();
            var ctx = new SetupContext();
            var anim = new SetupAnimation();

            BaseAvatar.gameObject.SetActive(true);
            PhantomAvatar.gameObject.SetActive(true);

            Debug.Log($"[PhantomSystem] Setting up Phantom System for {BaseAvatar.name} using {PhantomAvatar.name} ...");

            // Create unique generated paths
            var AvatarGlobalIdHash = GlobalObjectId.GetGlobalObjectIdSlow(BaseAvatar).GetHashCode();
            var AvatarNameWithId = $"{BaseAvatar.name}_{AvatarGlobalIdHash}";
            GeneratedAnimationPath = $"{GeneratedMainFolder}/{AvatarNameWithId}/Animation";
            GeneratedMenuPath = $"{GeneratedMainFolder}/{AvatarNameWithId}/Menu";

            DeleteExistingPhantomSystem();
            PhantomSystemInit(ctx);
            SetupArmatureConstraint(ctx, anim);
            SetupBoneConstraints(ctx, anim);
            AdaptModularAvatar(ctx);
            SetupGrabRoot(ctx, anim);
            anim.Save(GeneratedAnimationPath, ReferenceAnimationPath);
            AddMAPrefab(ctx, anim);
            if (!IsRemoveViewSystem) SetupViewSystem(ctx);
            if (!IsRemoveOriginalAnimator) MergeOriginalAnimator(ctx);
            if (IsChangePBImmobileType) ChangePhysBoneImmobileType(ctx);
            DeletePhantomAvatarRootComponents(ctx);

            PhantomAvatar.gameObject.SetActive(false);
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
            if (!Directory.Exists(GeneratedMainFolder))
            {
                Directory.CreateDirectory(GeneratedMainFolder);
            }

            tempPrefab = PrefabUtility.SaveAsPrefabAsset(PhantomAvatar.gameObject, $"{GeneratedMainFolder}/{tempPrefabName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var instanceAvatar = PrefabUtility.InstantiatePrefab(tempPrefab, ctx.PhantomSystem.transform) as GameObject;
            ctx.PhantomAvatarRoot = instanceAvatar;
            PrefabUtility.UnpackPrefabInstance(instanceAvatar, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            AssetDatabase.DeleteAsset($"{GeneratedMainFolder}/{tempPrefabName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ctx.PhantomAvatarRoot.transform.position = BaseAvatar.transform.position;
            ctx.PhantomAvatarRoot.transform.rotation = BaseAvatar.transform.rotation;

            ctx.PhantomAvatarRoot.name = "PhantomAvatar";
            ctx.PhantomAvatarRoot.SetActive(false);

            ctx.PhantomAnimator = ctx.PhantomAvatarRoot.GetComponent<Animator>();
            ctx.BaseAnimator = BaseAvatar.GetComponent<Animator>();
        }

        private void SetupArmatureConstraint(SetupContext ctx, SetupAnimation anim)
        {
            ctx.PhantomArmature = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            ctx.BaseArmature = ctx.BaseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;

            GameObject BaseAvatarPosition = new GameObject("BaseAvatarPosition");
            BaseAvatarPosition.transform.parent = ctx.PhantomSystem.transform;
            GameObject ArmatureConstraintTarget = new GameObject("AmatureConstraintTarget");
            ArmatureConstraintTarget.transform.parent = BaseAvatarPosition.transform;

            // Change the Phantom Avatar Armature's name
            ctx.PhantomAmaturePath = GetRelativePath(ctx.PhantomArmature, ctx.PhantomAvatarRoot.transform);
            ctx.PhantomArmature.name = "Armature_phantom";

            // Add constraint to BaseAvatarPosition
            var BaseAvatarPositionConstraint = BaseAvatarPosition.AddComponent<VRCParentConstraint>();
            BaseAvatarPositionConstraint.Locked = true;
            BaseAvatarPositionConstraint.IsActive = true;
            BaseAvatarPositionConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ctx.BaseArmature,
                    Weight = 1f
                }
            };
            BaseAvatarPositionConstraint.enabled = true;
            BaseAvatarPositionConstraint.FreezeToWorld = false;

            // Add constraint to AmatureConstraintTarget
            var ArmatureTargetConstraint = ArmatureConstraintTarget.AddComponent<VRCParentConstraint>();
            ArmatureTargetConstraint.Locked = true;
            ArmatureTargetConstraint.IsActive = true;
            ArmatureTargetConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ctx.BaseArmature,
                    Weight = 1f
                }
            };
            BaseAvatarPositionConstraint.enabled = true;

            // Add constraint to PhantomAvatarRoot
            var PhantomAvatarConstraint = ctx.PhantomAvatarRoot.AddComponent<VRCParentConstraint>();
            PhantomAvatarConstraint.Locked = true;
            PhantomAvatarConstraint.IsActive = true;
            PhantomAvatarConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ctx.BaseArmature,
                    Weight = 1f
                },
                new VRCConstraintSource
                {
                    SourceTransform = ctx.PhantomArmature,
                    Weight = 0f
                }
            };
            PhantomAvatarConstraint.enabled = true;
            PhantomAvatarConstraint.FreezeToWorld = true;

            // Add constraint to armature
            var ArmatureConstraint = ctx.PhantomArmature.gameObject.AddComponent<VRCParentConstraint>();
            ArmatureConstraint.Locked = true;
            ArmatureConstraint.IsActive = true;
            ArmatureConstraint.Sources = new VRCConstraintSourceKeyableList
            {
                new VRCConstraintSource
                {
                    SourceTransform = ArmatureConstraintTarget.transform,
                    Weight = 1f
                }
            };
            ArmatureConstraint.enabled = true;
            ArmatureConstraint.SolveInLocalSpace = true;

            // PhantomOFF: deactivate PhantomAvatar
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 0));
            // PhantomOFF: Root constraint freeze to world; disable the constraint
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomOFF.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 0));

            // PhantomPrepare: activate PhantomAvatar
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomPrepare: Root constraint unfreeze world; enable the constraint
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            anim.PhantomPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreezeOff: activate PhantomAvatar
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreezeOff: Root constraint freeze world; enable the constraint
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomFreezeOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PhantomFreeze: activate PhantomAvatar
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
            // PhantomFreeze: Root constraint freeze world; enable the constraint
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            anim.PhantomFreeze.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "IsActive", AnimationCurve.Constant(0, 0, 1));

            // PositionLockOn: BaseAvatarPosition constraint disable freeze to world
            anim.PositionLockOn.SetCurve(GetRelativePath(BaseAvatarPosition.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            // PositionLockOn: Armature Constraint disable freeze to world
            anim.PositionLockOn.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            // PositionLockOn: PahntomSystem constraint set to 1st source
            anim.PositionLockOn.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source0.Weight", AnimationCurve.Constant(0, 0, 1));
            anim.PositionLockOn.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source1.Weight", AnimationCurve.Constant(0, 0, 0));

            // PositionLockOff: BaseAvatarPosition constraint freeze to world
            anim.PositionLockOff.SetCurve(GetRelativePath(BaseAvatarPosition.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            // PositionLockOff: Armature Constraint disable freeze to world
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            // PositionLockOff: PahntomSystem constraint set to 1st source
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source0.Weight", AnimationCurve.Constant(0, 0, 1));
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source1.Weight", AnimationCurve.Constant(0, 0, 0));


            // PositionLockPrepare: create curves for keyframes
            float dt = 1.0f / 60.0f;

            AnimationCurve freezeToWorldCurve = new AnimationCurve();
            AnimationCurve solveInLocalSpaceCurve = new AnimationCurve();

            freezeToWorldCurve.AddKey(0, 1);
            freezeToWorldCurve.AddKey(dt, 0);
            AnimationUtility.SetKeyRightTangentMode(freezeToWorldCurve, 0, AnimationUtility.TangentMode.Constant);

            solveInLocalSpaceCurve.AddKey(0, 0);
            solveInLocalSpaceCurve.AddKey(2 * dt, 1);
            AnimationUtility.SetKeyRightTangentMode(solveInLocalSpaceCurve, 0, AnimationUtility.TangentMode.Constant);

            // PositionLockPrepare: BaseAvatarPosition constraint and PhantomSystem constraint disable freeze to world
            anim.PositionLockPrepare.SetCurve(GetRelativePath(BaseAvatarPosition.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", freezeToWorldCurve);
            anim.PositionLockPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", freezeToWorldCurve);
            // PositionLockPrepare: Armature Constraint freeze to world
            anim.PositionLockPrepare.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            // PositionLockPrepare: PahntomSystem constraint set to 2nd source
            anim.PositionLockPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source0.Weight", AnimationCurve.Constant(dt, dt, 0));
            anim.PositionLockPrepare.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source1.Weight", AnimationCurve.Constant(dt, dt, 1));
            // PositionLockPrepare: Armature Constraint disable solve in local space
            anim.PositionLockPrepare.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "SolveInLocalSpace", solveInLocalSpaceCurve);
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

            RelinkExceptionParameterList(ctx);

            // MA bone proxy adaption
            foreach (var proxy in MABoneProxy)
            {
                if (proxy.boneReference == HumanBodyBones.LastBone)
                {
                    proxy.subPath = RebasePhantomPath(ctx, proxy.subPath);
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
                if (!Directory.Exists(GeneratedMenuPath))
                {
                    Directory.CreateDirectory(GeneratedMenuPath);
                }
                string[] existingMenus = Directory.GetFiles(GeneratedMenuPath);
                foreach (var existingmenu in existingMenus) AssetDatabase.DeleteAsset(existingmenu);

                foreach (var installer in MAInstaller)
                {
                    if (installer.installTargetMenu == null || installer.installTargetMenu == BaseAvatar.expressionsMenu)
                    {
                        installer.installTargetMenu = PhantomMenu;
                    }
                    else
                    {
                        installer.installTargetMenu = CopyExpressionMenuRecursively(installer.installTargetMenu, GeneratedMenuPath);
                    }

                    if (installer.menuToAppend != null)
                    {
                        installer.menuToAppend = CopyExpressionMenuRecursively(installer.menuToAppend, GeneratedMenuPath);
                    }

                    EditorUtility.SetDirty(installer);
                    if (PrefabUtility.IsPartOfPrefabInstance(installer))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
                }

                foreach (var item in MAMenuItems)
                {
                    if (item.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && item.MenuSource == SubmenuSource.MenuAsset)
                    {
                        item.Control.subMenu = CopyExpressionMenuRecursively(item.Control.subMenu, GeneratedMenuPath);
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
                        // Skip exceptions
                        if (ctx.ExceptionParameters.Contains(parameter)) continue;
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
                PhantomAvatarMAMergeAnimator.layerPriority = 10;
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
                    PhantomAvatarMAMenuInstaller.menuToAppend = CopyExpressionMenuRecursively(PhantomAvatar.expressionsMenu, GeneratedMenuPath);
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
            var ConmponentWhiteList = new Type[]
            {
                typeof(Transform),
                typeof(VRCParentConstraint),
                typeof(ModularAvatarMeshSettings)
            };
            UnityEngine.Component[] PhantomAvatarOldComponents = ctx.PhantomAvatarRoot.GetComponents<UnityEngine.Component>();
            foreach (var component in PhantomAvatarOldComponents)
            {
                // check if the component is in the white list
                if (!ConmponentWhiteList.Contains(component.GetType()))
                {
                    DestroyImmediate(component);
                }
            }
        }
        #endregion

        #region --- Utilities ---
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

            // Check if it's a subAsset
            if (AssetDatabase.IsSubAsset(sourceMenu))
            {
                Debug.LogWarning($"[Phantom System] The menu '{sourceMenu.name}' is a sub-asset. PhanotmSystem will not copy it.");
                return sourceMenu;
            }

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

        private string RebasePhantomPath(SetupContext ctx, string path)
        {
            if (path.StartsWith(ctx.PhantomAmaturePath))
            {
                var NewArmaturePath = GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform);
                return NewArmaturePath + path.Substring(ctx.PhantomAmaturePath.Length);
            }
            else
            {
                return "PhantomSystem/PhantomAvatar/" + path;
            }
        }

        private void RelinkExceptionParameterList(SetupContext ctx)
        {
            ctx.ExceptionParameters = new List<ModularAvatarParameters>();
            for (int i = 0; i < ExceptionParameters.Count; i++)
            {
                var param = ExceptionParameters[i];
                var paramRebasePath = RebasePhantomPath(ctx, GetRelativePath(param.transform, PhantomAvatar.transform));
                if (BaseAvatar.transform.Find(paramRebasePath) == null)
                {
                    Debug.LogWarning($"[Phantom System] Relink parameter exception error. Rebased path '{paramRebasePath}' dose not exsit. This should not happen :(");
                    continue;
                }
                ctx.ExceptionParameters.Add(BaseAvatar.transform.Find(paramRebasePath).gameObject.GetComponent<ModularAvatarParameters>());
            }
        }

        private AvatarObjectReference RebaseAvatarObjectReference(SetupContext ctx, AvatarObjectReference obj)
        {
            string newPath;
            AvatarObjectReference newObj = new AvatarObjectReference();

            newPath = RebasePhantomPath(ctx, obj.referencePath);
            if (BaseAvatar.transform.Find(newPath) == null)
            {
                Debug.LogWarning($"[Phantom System] Cannot rebase the AvatarObjectReference '{obj.referencePath}'.Rebased path '{newPath}' does not exsit.");
                return obj;
            }
            newObj.Set(BaseAvatar.transform.Find(newPath).gameObject);  
            return newObj;
        }
        #endregion
    }
}