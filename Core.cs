using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

[assembly: MelonInfo(typeof(BluePrinceCompass.Core), "BluePrinceCompass", "1.0.0", "ComplexSimple")]
[assembly: MelonGame("Dogubomb", "BLUE PRINCE")]

namespace BluePrinceCompass
{
    public class Core : MelonMod
    {

        private const String PreferencesCategoryName = "BluePrinceCompass";
        private MelonPreferences_Category _category;

        public MelonPreferences_Entry<float> _compassPositionX;
        public MelonPreferences_Entry<float> _compassPositionY;
        public MelonPreferences_Entry<float> _compassScale;
        public MelonPreferences_Entry<bool> _invertCompassRotation;
        
        private GameObject _compassObject;
        private Transform _compassNeedleTransform;
        private Transform _playerTransform;
        private GameObject _cullingReference; 

        private const string HudGameObjectPath = "__SYSTEM/HUD";
        private const string CullingReferenceRelativePath = "Steps/Steps Icon";
        private const string PlayerGameObjectPath = "__SYSTEM/FPS Home/FPSController - Prince";
        private const string CompassBundlePathSuffix = "BluePrinceCompass/assets/compass.bundle";
        private const string CompassPrefabName = "Compass Mod HUD";
        private const string CompassNeedleName = "Compass Needle";

        private const float CompassZPosition = 27.46f;

        public override void OnInitializeMelon()
        {
            InitPreferences();
            LoggerInstance.Msg("Initialized Blue Prince Compass Mod.");
        }

        // Initializes the preferences for the compass mod, including position, scale, and
        // rotation inversion.
        private void InitPreferences()
        {
            _category = MelonPreferences.CreateCategory(PreferencesCategoryName);

            _compassPositionX = MelonPreferences.CreateEntry<float>(PreferencesCategoryName, "CompassPositionX", 0.0f, null, "The x position of the compass relative to the HUD GameObject.");
            _compassPositionY = MelonPreferences.CreateEntry<float>(PreferencesCategoryName, "CompassPositionY", -986.0f, null, "The y position of the compass relative to the HUD GameObject.");
            _compassScale = MelonPreferences.CreateEntry<float>(PreferencesCategoryName, "CompassScale", 1.0f, null, "The size of the compass relative to its default size.");
            _invertCompassRotation = MelonPreferences.CreateEntry<bool>(PreferencesCategoryName, "InvertCompassRotation", false, null, "If enabled, the compass will rotate in the opposite direction.");
            LoggerInstance.Msg($"Compass Preferences: {Path.Combine(MelonEnvironment.UserDataDirectory, $"{PreferencesCategoryName}.cfg")}");
            _category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, $"{PreferencesCategoryName}.cfg")); 
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameObject hud = GameObject.Find(HudGameObjectPath);
            if (hud == null)
            {
                LoggerInstance.Msg($"HUD GameObject \"{HudGameObjectPath}\" not found in the scene, skipping compass creation.");
                return;
            }

            _playerTransform = GetPlayerTransform();
            _cullingReference = GetCullingReference(hud);
            _compassObject = InitCompass(hud);
            _compassNeedleTransform = GetCompassNeedle(_compassObject);
        }

        // Retrieves the player transform by finding the GameObject with the specified path.
        private Transform GetPlayerTransform()
        {
            GameObject player = GameObject.Find(PlayerGameObjectPath);
            if (player == null)
            {
                LoggerInstance.Error($"Could not find the Player GameObject \"{PlayerGameObjectPath}\"");
                return null;
            }
            return player.transform;
        }

        // Retrieves the culling reference GameObject from the HUD GameObject using the
        // specified relative path.
        private GameObject GetCullingReference(GameObject hud)
        {
            Transform cullingReference = hud.transform.Find(CullingReferenceRelativePath);
            if (cullingReference == null)
            {
                LoggerInstance.Error($"Could not find culling reference Transform \"{HudGameObjectPath}/{CullingReferenceRelativePath}\"");
                return null;
            }
            return cullingReference.gameObject;
        }

        // Initializes the compass by loading the prefab from the asset bundle, instantiating
        // it, and setting it up for culling.
        private GameObject InitCompass(GameObject hud)
        {
            GameObject compassObject = LoadPrefab(hud);
            if (compassObject == null)
            {
                LoggerInstance.Error($"Could not load the compass prefab from the asset bundle. Please ensure the bundle is correctly placed in the mods directory at BluePrinceCompass\\assets\\compass.bundle.");
                return null;
            }
            // Initially disable the compass so it doesn't appear in the opening cutscene.
            compassObject.SetActive(false);
            InitCulling(compassObject, hud);

            LoggerInstance.Msg($"Compass Object instantiated and parented to \"{HudGameObjectPath}\".");
            return compassObject;
        }

        // Retrieves the compass needle Transform from the compass GameObject.
        private Transform GetCompassNeedle(GameObject compassObject)
        {
            if (compassObject == null)
            {
                LoggerInstance.Error("Compass Object is null, cannot find the compass needle.");
                return null;
            }
            Transform needle = compassObject.transform.Find(CompassNeedleName);
            if (needle == null)
            {
                LoggerInstance.Error($"Could not find the compass needle Transform \"{CompassNeedleName}\" in the compass object.");
            }
            return needle;
        }
        
        // Loads the compass prefab from the asset bundle and instantiates it at the specified
        // position and scale.
        private GameObject LoadPrefab(GameObject hud)
        {
            string bundlePath = Path.Combine(MelonEnvironment.ModsDirectory, CompassBundlePathSuffix);
            LoggerInstance.Msg($"Bundle Path: {bundlePath}");
            AssetBundle compassBundle = AssetBundle.LoadFromFile(bundlePath);
            if (compassBundle == null)
            {
                LoggerInstance.Error($"Failed to load asset bundle from path: {bundlePath}");
                return null;
            }

            GameObject prefab = compassBundle.LoadAsset<GameObject>(CompassPrefabName);
            if (prefab == null)
            {
                LoggerInstance.Error($"Failed to load prefab \"{CompassPrefabName}\" from asset bundle.");
                return null;
            }

            Vector3 compassPosition = new Vector3(_compassPositionX.Value, _compassPositionY.Value, CompassZPosition);
            GameObject compassObject = GameObject.Instantiate(prefab, compassPosition, Quaternion.identity, hud.transform);
            compassObject.transform.localScale = new Vector3(_compassScale.Value, _compassScale.Value, 1);

            compassBundle.Unload(false);
            return compassObject;
        }

        // Sets up compass culling so it will only be rendered when steps, gems and keys are
        // rendered.
        private void InitCulling(GameObject obj, GameObject hud)
        {
            Il2Cpp.Culler hudCuller = hud.GetComponent<Il2Cpp.Culler>();
            if (hudCuller == null)
            {
                LoggerInstance.Error($"HUD GameObject with name \"{HudGameObjectPath}\" does not have a Culler component. The compass will not be added to the culler.");
                return;
            }

            // This (like this whole codebase) is incredibly overengineered, but I have an
            // irrational fear of breaking forward compatibility if I use hardcoded values.
            bool referenceInEnabledList = true;
            if (_cullingReference == null)
            {
                LoggerInstance.Error("The culling reference is null, so the rendering layer couldn't be determined.");
            }
            else
            {
                SetLayerForAllDescendants(obj, _cullingReference.gameObject.layer);
                Renderer referenceRenderer = _cullingReference.GetComponent<Renderer>();
                referenceInEnabledList = (referenceRenderer != null &&
                    hudCuller._childRenderersEnabled.Contains(referenceRenderer));
            }

            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            // I wasn't sure how to convert from the array to an Il2CPP enumerable, so I just
            // add the MeshRenderers one by one.
            foreach (MeshRenderer renderer in renderers)
            {
                // I don't like using these private fields, but I don't know how to add the
                // compass to the culler otherwise.
                hudCuller._childRenderers.Add(renderer);
                if (referenceInEnabledList)
                {
                    hudCuller._childRenderersEnabled.Add(renderer);
                }
                else
                {
                    hudCuller._childRenderersDisabled.Add(renderer);
                }
            }
        }

        // Sets the layer for all descendants of the given GameObject to the specified layer.
        private void SetLayerForAllDescendants(GameObject obj, int layer)
        {
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }
        }

        public override void OnUpdate()
        {
            if (_compassObject == null || _compassNeedleTransform == null || _playerTransform == null)
            {
                return;
            }
            // I'm not sure how the HUD objects get enabled after the opening cutscene, so I
            // just enable the compass if the culling reference is active or if there is no
            // culling reference.
            _compassObject.SetActive(_cullingReference == null || _cullingReference.activeInHierarchy);
            AlignCompass();
        }

        // Aligns the compass needle's rotation with the player's forward direction.
        private void AlignCompass()
        {
            if (_playerTransform == null || _compassObject == null)
            {
                return;
            }
            Vector3 fpsForward = _playerTransform.forward;
            Vector3 compassHeading = new Vector3(fpsForward.x, fpsForward.z, 0).normalized;
            float compassAngle = Vector3.SignedAngle(compassHeading, Vector3.up, Vector3.forward);
            if (_invertCompassRotation.Value)
            {
                compassAngle = -compassAngle;
            }
            _compassNeedleTransform.localRotation = Quaternion.Euler(0, 0, compassAngle);
        }
    }
}
