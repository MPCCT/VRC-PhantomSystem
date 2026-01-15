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
        private bool IsRemoveScaleControl;
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
        private HashSet<string> MenuCopyErrorKeys = new HashSet<string>();

        private string GeneratedAnimationPath;
        private string GeneratedMenuPath;

        #endregion

        #region --- Paths & Texts ---

        private static readonly List<string> PhantomSystemAssetDataKeys = new List<string>
        {
            "MainFolder",
            "MAPrefab",
            "MAPrefab_NoPhantomMenu",
            "ReferenceAnimation",
            "PhantomMenu",
            "ScaleControlPrefab",
            "ViewSystemPrefab",
            "ViewSystemPrefab_NoPhantomMenu",
            "GrabPrefab",
            "MainMenu",
            "LocalMainMenu_zh",
            "LocalMainMenu_jp",
            "SubMenu",
            "LocalSubMenu_zh",
            "LocalSubMenu_jp",
            "MainMenu_NoPhantomMenu",
            "LocalMainMenu_NoPhantomMenu_zh",
            "LocalMainMenu_NoPhantomMenu_jp",
            "SubMenu_NoPhantomMenu",
            "LocalSubMenu_NoPhantomMenu_zh",
            "LocalSubMenu_NoPhantomMenu_jp",
            "LocalScaleControlMenu_zh",
            "LocalScaleControlMenu_jp",
            "LocalViewSysMenu_zh",
            "LocalViewSysMenu_jp"
        };

        private static string MainFolder => ResolveAssetPath("MainFolder");
        private static string GeneratedMainFolder => $"{MainFolder}/~Generated";

        private static string MAPrefabPath => ResolveAssetPath("MAPrefab");
        private static string MAPrefabPath_NoPhantomMenu => ResolveAssetPath("MAPrefab_NoPhantomMenu");
        private static string ReferenceAnimationPath => ResolveAssetPath("ReferenceAnimation");
        private static string PhantomMenuPath => ResolveAssetPath("PhantomMenu");
        private static string ScaleControlPrefabPath => ResolveAssetPath("ScaleControlPrefab");
        private static string ViewSystemPrefabPath => ResolveAssetPath("ViewSystemPrefab");
        private static string ViewSystemPrefabPath_NoPhantomMenu => ResolveAssetPath("ViewSystemPrefab_NoPhantomMenu");
        private static string GrabPrefabPath => ResolveAssetPath("GrabPrefab");

        private static string MainMenuPath => ResolveAssetPath("MainMenu");
        private static string LocalMainMenuPath_zh => ResolveAssetPath("LocalMainMenu_zh");
        private static string LocalMainMenuPath_jp => ResolveAssetPath("LocalMainMenu_jp");
        private static string SubMenuPath => ResolveAssetPath("SubMenu");
        private static string LocalSubMenuPath_zh => ResolveAssetPath("LocalSubMenu_zh");
        private static string LocalSubMenuPath_jp => ResolveAssetPath("LocalSubMenu_jp");

        private static string MainMenu_NoPhantomMenuPath => ResolveAssetPath("MainMenu_NoPhantomMenu");
        private static string LocalMainMenu_NoPhantomMenuPath_zh => ResolveAssetPath("LocalMainMenu_NoPhantomMenu_zh");
        private static string LocalMainMenu_NoPhantomMenuPath_jp => ResolveAssetPath("LocalMainMenu_NoPhantomMenu_jp");
        private static string SubMenu_NoPhantomMenuPath => ResolveAssetPath("SubMenu_NoPhantomMenu");
        private static string LocalSubMenu_NoPhantomMenuPath_zh => ResolveAssetPath("LocalSubMenu_NoPhantomMenu_zh");
        private static string LocalSubMenu_NoPhantomMenuPath_jp => ResolveAssetPath("LocalSubMenu_NoPhantomMenu_jp");

        private static string LocalScaleControlMenuPath_zh => ResolveAssetPath("LocalScaleControlMenu_zh");
        private static string LocalScaleControlMenuPath_jp => ResolveAssetPath("LocalScaleControlMenu_jp");
        private static string LocalViewSysMenuPath_zh => ResolveAssetPath("LocalViewSysMenu_zh");
        private static string LocalViewSysMenuPath_jp => ResolveAssetPath("LocalViewSysMenu_jp");

        private string T(string key)
        {
            return PhantomSystemLocalizationData.Text(key);
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

            public GameObject PhantomMA;

            public Transform SpawnPosition;
            public Dictionary<HumanBodyBones, string> PhantomBonePaths = new Dictionary<HumanBodyBones, string>();
            public List<ModularAvatarParameters> ExceptionParameters = new List<ModularAvatarParameters>();
            public string PhantomAmaturePath;

            public int BaseAvatarAnimatorMaxPriority = 0;
            public int PhantomAvatarAnimatorMaxPriority = 0;
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
            PhantomSystemLocalizationData.currentLocale = (PhantomSystemLocalizationData.Locale)EditorPrefs.GetInt("MPCCT_PhantomSystem_Locale", (int)PhantomSystemLocalizationData.Locale.English);
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
            EditorGUILayout.LabelField("PhantomSystem v1.1.0 Made By MPCCT");

            // Language selection
            string[] localeOptions = new[] { "English", "中文", "日本語" };
            int newLocale = EditorGUILayout.Popup(T("LanguageLabel"), (int)PhantomSystemLocalizationData.currentLocale, localeOptions);
            if (newLocale != (int)PhantomSystemLocalizationData.currentLocale)
            {
                PhantomSystemLocalizationData.currentLocale = (PhantomSystemLocalizationData.Locale)newLocale;
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
            IsRemoveScaleControl = EditorGUILayout.ToggleLeft(T("RemoveScaleControl"), IsRemoveScaleControl);

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

            var SpawnPosition = FindSpawnPositionIfAlreadySetup(BaseAvatar);
            if (SpawnPosition != null)
            {
                // Add a Botton that will select PhantomSpawnPosition in scene
                if (GUILayout.Button(T("AdjustSpawnPosition")))
                {
                    // Select and ping the spawn object in the hierarchy
                    Selection.activeGameObject = SpawnPosition.gameObject;
                    EditorGUIUtility.PingObject(SpawnPosition.gameObject);

                    // Focus scene view and frame selection if possible
                    FocusWindowIfItsOpen<SceneView>();
                    var sv = SceneView.lastActiveSceneView;
                    if (sv != null)
                    {
                        sv.FrameSelected();
                    }
                }

            }
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

            // Asset existence
            foreach (var key in PhantomSystemAssetDataKeys)
            {
                var path = PhantomSystemAssetData.ResolvePath(key);
                if (string.IsNullOrEmpty(path))
                {
                    errors.Add(key + T("AssetsNotFound"));
                }
            }

            // Reference animation controller validity
            var refController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ReferenceAnimationPath);
            if (refController == null || refController.layers == null || refController.layers.Length < 3)
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
                if (component == null) continue; // skip missing scripts
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

        private Transform FindSpawnPositionIfAlreadySetup(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return null;
            var phantomSystem = avatar.transform.Find("PhantomSystem");
            if (phantomSystem == null) return null;
            for (int i = 0;i< phantomSystem.childCount; i++)
            {
                var child = phantomSystem.GetChild(i);
                if (child.name == "PhantomSpawnPosition")
                {
                    return child;
                }
            }
            return null;
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
            if (!IsRemoveScaleControl) SetupScaleControl(ctx);
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
            CalMAMergeAnimatorPriority(ctx);

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

            // Add PhantomSpawnPosition
            GameObject spawnPosition = new GameObject("PhantomSpawnPosition");
            spawnPosition.transform.parent = ctx.PhantomSystem.transform;
            ctx.SpawnPosition = spawnPosition.transform;
        }
        private void CalMAMergeAnimatorPriority(SetupContext ctx)
        {
            var BaseAvatarMAMergeAnimators = BaseAvatar.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);
            var PhantomAvatarMAMergeAnimators = PhantomAvatar.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);

            var BaseAvatarLayerPriorities = BaseAvatarMAMergeAnimators.Select(anim => anim.layerPriority).ToList();
            var PhantomAvatarLayerPriorities = PhantomAvatarMAMergeAnimators.Select(anim => anim.layerPriority).ToList();
            ctx.BaseAvatarAnimatorMaxPriority = BaseAvatarLayerPriorities.Count > 0 ? BaseAvatarLayerPriorities.Max() : 0;
            ctx.PhantomAvatarAnimatorMaxPriority = PhantomAvatarLayerPriorities.Count > 0 ? PhantomAvatarLayerPriorities.Max() : 0;
        }
        private void SetupArmatureConstraint(SetupContext ctx, SetupAnimation anim)
        {
            ctx.PhantomArmature = ctx.PhantomAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            ctx.BaseArmature = ctx.BaseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent;
            ctx.SpawnPosition.position = ctx.BaseArmature.position;
            ctx.SpawnPosition.rotation = ctx.BaseArmature.rotation;

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
                    SourceTransform = ctx.SpawnPosition,
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
                    SourceTransform = ctx.SpawnPosition,
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
                    SourceTransform = ctx.SpawnPosition,
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
            ArmatureConstraint.FreezeToWorld = true;
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
            // PositionLockOn: Armature Constraint solve in local space
            anim.PositionLockOn.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "SolveInLocalSpace", AnimationCurve.Constant(0, 0, 1));

            // PositionLockOff: BaseAvatarPosition constraint freeze to world
            anim.PositionLockOff.SetCurve(GetRelativePath(BaseAvatarPosition.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 1));
            // PositionLockOff: Armature Constraint disable freeze to world
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "FreezeToWorld", AnimationCurve.Constant(0, 0, 0));
            // PositionLockOff: PahntomSystem constraint set to 1st source
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source0.Weight", AnimationCurve.Constant(0, 0, 1));
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomAvatarRoot.transform, BaseAvatar.transform), typeof(VRCParentConstraint), "Sources.source1.Weight", AnimationCurve.Constant(0, 0, 0));
            // PositionLockOff: Armature Constraint solve in local space
            anim.PositionLockOff.SetCurve(GetRelativePath(ctx.PhantomArmature, BaseAvatar.transform), typeof(VRCParentConstraint), "SolveInLocalSpace", AnimationCurve.Constant(0, 0, 1));

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
                        // Add parent constranit to Hips bone even if using Rotation Constraint
                        if (!IsUseRotationConstraint || bone == HumanBodyBones.Hips)
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
                // Change MAMergeAnimator Priority
                foreach (var animator in MAMergeAnimator)
                {
                    animator.layerPriority = animator.layerPriority + ctx.BaseAvatarAnimatorMaxPriority + 2;
                    EditorUtility.SetDirty(animator);
                    if (PrefabUtility.IsPartOfPrefabInstance(animator))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                }

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
                        installer.installTargetMenu = CopyExpressionMenuRecursively(
                            installer.installTargetMenu,
                            GeneratedMenuPath,
                            $"ModularAvatarMenuInstaller (installTargetMenu) at {GetComponentPath(installer)}");
                    }

                    if (installer.menuToAppend != null)
                    {
                        installer.menuToAppend = CopyExpressionMenuRecursively(
                            installer.menuToAppend,
                            GeneratedMenuPath,
                            $"ModularAvatarMenuInstaller (menuToAppend) at {GetComponentPath(installer)}");
                    }

                    EditorUtility.SetDirty(installer);
                    if (PrefabUtility.IsPartOfPrefabInstance(installer))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
                }

                foreach (var item in MAMenuItems)
                {
                    if (item.Control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && item.MenuSource == SubmenuSource.MenuAsset)
                    {
                        item.Control.subMenu = CopyExpressionMenuRecursively(
                            item.Control.subMenu,
                            GeneratedMenuPath,
                            $"ModularAvatarMenuItem (subMenu) at {GetComponentPath(item)}");
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
            ctx.PhantomMA = MAPrefabInstance;

            var MAPrefabAnimator = MAPrefabInstance.GetComponent<ModularAvatarMergeAnimator>();
            MAPrefabAnimator.animator = anim.PhantomController;
            MAPrefabAnimator.layerPriority = ctx.PhantomAvatarAnimatorMaxPriority + ctx.BaseAvatarAnimatorMaxPriority + 3;

            // Localize Menu
            switch (PhantomSystemLocalizationData.currentLocale)
            {
                case PhantomSystemLocalizationData.Locale.Chinese:
                    {
                        MAPrefabInstance.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = IsRemovePhantomMenu ? 
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenu_NoPhantomMenuPath_zh) : 
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenuPath_zh); ;
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.English:
                    {
                        MAPrefabInstance.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(MainMenu_NoPhantomMenuPath) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(MainMenuPath); ;
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.Japanese:
                    {
                        MAPrefabInstance.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = IsRemovePhantomMenu ? 
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenu_NoPhantomMenuPath_jp) : 
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalMainMenuPath_jp); ;
                        break;
                    }
            }
        }

        private void SetupScaleControl(SetupContext ctx)
        {
            GameObject ScaleControlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScaleControlPrefabPath);
            GameObject ScaleControl = (GameObject)PrefabUtility.InstantiatePrefab(ScaleControlPrefab, ctx.PhantomMA.transform);

            var ScaleControlAnimator = ScaleControl.GetComponent<ModularAvatarMergeAnimator>();
            ScaleControlAnimator.layerPriority = ctx.PhantomAvatarAnimatorMaxPriority + ctx.BaseAvatarAnimatorMaxPriority + 4;

            // Localize Menu
            switch (PhantomSystemLocalizationData.currentLocale)
            {
                case PhantomSystemLocalizationData.Locale.Chinese:
                    {
                        VRCExpressionsMenu ScaleMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalScaleControlMenuPath_zh);
                        ScaleControl.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = ScaleMenu;
                        ScaleControl.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenu_NoPhantomMenuPath_zh) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenuPath_zh);
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.English:
                    {
                        ScaleControl.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(SubMenu_NoPhantomMenuPath) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(SubMenuPath);
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.Japanese:
                    {
                        VRCExpressionsMenu ScaleMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalScaleControlMenuPath_jp);
                        ScaleControl.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = ScaleMenu;
                        ScaleControl.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenu_NoPhantomMenuPath_jp) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenuPath_jp);
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

            var ViewSystemAnimator = ViewSystem.GetComponent<ModularAvatarMergeAnimator>();
            ViewSystemAnimator.layerPriority = ctx.PhantomAvatarAnimatorMaxPriority + ctx.BaseAvatarAnimatorMaxPriority + 4;

            // Localize Menu
            switch (PhantomSystemLocalizationData.currentLocale)
            {
                case PhantomSystemLocalizationData.Locale.Chinese:
                    {
                        VRCExpressionsMenu ViewMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalViewSysMenuPath_zh);
                        ViewSystem.GetComponent<ModularAvatarMenuInstaller>().menuToAppend = ViewMenu;
                        ViewSystem.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenu_NoPhantomMenuPath_zh) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(LocalSubMenuPath_zh);
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.English:
                    {
                        ViewSystem.GetComponent<ModularAvatarMenuInstaller>().installTargetMenu = IsRemovePhantomMenu ?
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(SubMenu_NoPhantomMenuPath) :
                            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(SubMenuPath);
                        break;
                    }
                case PhantomSystemLocalizationData.Locale.Japanese:
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
                PhantomAvatarMAMergeAnimator.layerPriority = ctx.BaseAvatarAnimatorMaxPriority + 1;
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
                    PhantomAvatarMAMenuInstaller.menuToAppend = CopyExpressionMenuRecursively(
                        PhantomAvatar.expressionsMenu,
                        GeneratedMenuPath,
                        $"PhantomOriginalFX_MA MenuInstaller (menuToAppend) at {GetComponentPath(PhantomAvatarMAMenuInstaller)}");
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
                EditorUtility.SetDirty(PB);
                if (PrefabUtility.IsPartOfPrefabInstance(PB))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(PB);
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

        private string GetComponentPath(Component component)
        {
            if (component == null)
            {
                return "<null>";
            }

            var root = BaseAvatar != null ? BaseAvatar.transform : (PhantomAvatar != null ? PhantomAvatar.transform : null);
            if (component.transform == null)
            {
                return component.name;
            }

            if (root != null && component.transform.IsChildOf(root))
            {
                var rel = GetRelativePath(component.transform, root);
                return string.IsNullOrEmpty(rel) ? root.name : $"{root.name}/{rel}";
            }

            return component.transform.name;
        }

        private void ShowMenuCopyError(string context, VRCExpressionsMenu sourceMenu, string path, string reason, Exception ex = null)
        {
            string menuName = sourceMenu != null ? sourceMenu.name : "<null>";
            string assetPath = sourceMenu != null ? AssetDatabase.GetAssetPath(sourceMenu) : "<null>";
            string message =
                "Copy Expressions Menu failed.\n" +
                $"Context: {context}\n" +
                $"Menu: {menuName}\n" +
                $"Asset: {assetPath}\n" +
                $"Target: {path}\n" +
                $"Reason: {reason}";

            if (ex != null)
            {
                message += $"\nException: {ex.GetType().Name}: {ex.Message}";
            }

            string key = $"{context}|{assetPath}|{reason}";
            if (!MenuCopyErrorKeys.Contains(key))
            {
                MenuCopyErrorKeys.Add(key);
                EditorUtility.DisplayDialog("Phantom System - Menu Copy Error", message, "OK");
            }

            Debug.LogError($"[Phantom System] {message}");
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

        private VRCExpressionsMenu CopyExpressionMenuRecursively(VRCExpressionsMenu sourceMenu, string path, string context)
        {
            string safeContext = string.IsNullOrEmpty(context) ? "<unknown>" : context;
            try
            {
                if (sourceMenu == null)
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Source menu is null.");
                    return null;
                }

                string sourcePath = AssetDatabase.GetAssetPath(sourceMenu);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Source menu has no asset path (likely not an asset).");
                    return sourceMenu;
                }

                // Create unique menu name
                // use GUID
                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (string.IsNullOrEmpty(guid))
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Failed to resolve GUID for source menu.");
                    return sourceMenu;
                }

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
                    Debug.LogWarning($"[Phantom System] The menu '{sourceMenu.name}' is a sub-asset. PhantomSystem will not copy it.");
                    return sourceMenu;
                }

                // copy menu
                if (!AssetDatabase.CopyAsset(sourcePath, $"{path}/{MenuRename}.asset"))
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "AssetDatabase.CopyAsset failed.");
                    return sourceMenu;
                }

                var copiedMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>($"{path}/{MenuRename}.asset");
                if (copiedMenu == null)
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Copied menu asset could not be loaded.");
                    return sourceMenu;
                }

                if (sourceMenu.controls == null)
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Source menu controls is null.");
                    return copiedMenu;
                }

                if (copiedMenu.controls == null)
                {
                    ShowMenuCopyError(safeContext, sourceMenu, path, "Copied menu controls is null.");
                    return copiedMenu;
                }

                // trverse sub-menus
                var controls = copiedMenu.controls;
                int count = Math.Min(sourceMenu.controls.Count, controls.Count);
                if (sourceMenu.controls.Count != controls.Count)
                {
                    ShowMenuCopyError(
                        safeContext,
                        sourceMenu,
                        path,
                        $"Controls count mismatch. Source: {sourceMenu.controls.Count}, Copied: {controls.Count}.");
                }

                for (int i = 0; i < count; i++)
                {
                    var c = controls[i];
                    if (c != null && c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null)
                    {
                        var newSubMenu = CopyExpressionMenuRecursively(c.subMenu, path, $"{safeContext} -> SubMenu[{i}] '{c.name}'");
                        c.subMenu = newSubMenu;
                        controls[i] = c; // write back
                    }
                }

                EditorUtility.SetDirty(copiedMenu);
                AssetDatabase.SaveAssetIfDirty(copiedMenu);
                return copiedMenu;
            }
            catch (Exception ex)
            {
                ShowMenuCopyError(safeContext, sourceMenu, path, "Exception thrown while copying menu.", ex);
                return sourceMenu;
            }
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

        // Resolve asset paths via PhantomSystemAssetData.
        private static string ResolveAssetPath(string key)
        {
            var p = PhantomSystemAssetData.ResolvePath(key);
            if (!string.IsNullOrEmpty(p)) return p;
            return null;
        }
        #endregion
    }
}
