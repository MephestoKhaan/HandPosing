using HandPosing.Interaction;
using HandPosing.OVRIntegration;
using HandPosing.OVRIntegration.GrabEngine;
using HandPosing.TrackingData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HandPosing.Editor
{
    public class HandPosingOVRRigWizard : ScriptableWizard
    {
        [Tooltip("The camera rig in the scene to attach the puppets to. If none is present it will try to create a new one.")]
        [SerializeField]
        private OVRCameraRig ovrRig;

        [Tooltip("Set to True if you want to add an Animator to the hand to be used with a Controller. Check HandPosing/HandAnimatorGenerator if you need to create the animations.")]
        [SerializeField]
        private bool controllerSupport = true;
        [Tooltip("Set to True if you want to add a smoothing layer for the Hand-Tracking data")]
        [SerializeField]
        private bool addSmoothSkeletonData = true;
        [Tooltip("Set to True if you want to manually update the fields if the neccesary components are *already present* in the hand (HandPuppet,SkeletonDataProvider, etc) ")]
        [SerializeField]
        private bool overridePresentValues = false;

        [Header("Set to the Hands in the scene, or prefab/model if none present already.")]
        [SerializeField]
        private GameObject leftHand;
        [SerializeField]
        private GameObject rightHand;

        [MenuItem("HandPosing/Add HandPosing to OVRRig")]
        private static void CreateWizard()
        {
            HandPosingOVRRigWizard wizard = DisplayWizard<HandPosingOVRRigWizard>("Add HandPosing to OVRRig", "Close", "Add HandPosing");
            wizard.ResetFields();
        }

        private void ResetFields()
        {
            ovrRig = GameObject.FindObjectOfType<OVRCameraRig>(false);
            HandPuppet leftPuppet = null;
            HandPuppet rightPuppet = null;
            HandPuppet[] puppets = GameObject.FindObjectsOfType<HandPuppet>();
            if(puppets != null && puppets.Length > 0)
            {
                leftPuppet = puppets.First(p => GetPrivateValue<Handeness>(p, "handeness") == Handeness.Left);
                rightPuppet = puppets.First(p => GetPrivateValue<Handeness>(p, "handeness") == Handeness.Right);
            }
            leftHand = leftPuppet != null ? leftPuppet.gameObject : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus/VR/Meshes/HandTracking/OculusHand_L.fbx");
            rightHand = rightPuppet != null ? rightPuppet.gameObject : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus/VR/Meshes/HandTracking/OculusHand_R.fbx");
        }

        private void OnWizardCreate() { }

        private void OnWizardOtherButton()
        {
            FindOVRRig();
            LinkHandPosing(ovrRig);
        }

        private void FindOVRRig()
        {
            if (ovrRig == null)
            {
                GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus/VR/Prefabs/OVRCameraRig.prefab");
                if (rigPrefab != null)
                {
                    GameObject rigGameObject = GameObject.Instantiate(rigPrefab);
                    rigGameObject.TryGetComponent(out ovrRig);
                }
            }

            Debug.Assert(ovrRig != null, "OVRCameraRig not found!");
        }

        private void LinkHandPosing(OVRCameraRig rig)
        {
            UpdateNotifierOVR updateNotifier = LinkUpdateNotifier(ovrRig.gameObject);

            Transform leftHand = rig.leftHandAnchor;
            Transform rightHand = rig.rightHandAnchor;

            HandTrackingProviders leftDataProvider = AttachTrackers(leftHand.gameObject, Handeness.Left);
            HandTrackingProviders rightDataProvider = AttachTrackers(rightHand.gameObject, Handeness.Right);

            leftDataProvider.updateNotifier = rightDataProvider.updateNotifier = updateNotifier;

            CreatePuppetedHand(leftDataProvider);
            CreatePuppetedHand(rightDataProvider);
        }

        private UpdateNotifierOVR LinkUpdateNotifier(GameObject rig)
        {
            UpdateNotifierOVR updateNotifier = AddMissingComponent<UpdateNotifierOVR>(rig);
            SetPrivateValue(updateNotifier, "ovrCameraRig", ovrRig);
            return updateNotifier;
        }

        private HandTrackingProviders AttachTrackers(GameObject hand, Handeness handeness)
        {
            if (!TryAddMissingComponent(hand, out OVRHand ovrHand)
                || overridePresentValues)
            {
                OVRHand.Hand ovrHandeness = handeness == Handeness.Left ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight;
                SetPrivateValue(ovrHand, "HandType", ovrHandeness);
            }

            if (!TryAddMissingComponent(hand, out OVRSkeleton ovrSkeleton)
                || overridePresentValues)
            {
                OVRSkeleton.SkeletonType skeletonHandeness = handeness == Handeness.Left ? OVRSkeleton.SkeletonType.HandLeft : OVRSkeleton.SkeletonType.HandRight;
                SetPrivateValue(ovrSkeleton, "_skeletonType", skeletonHandeness);
            }

            SkeletonDataProvider skeletonData = null;
            if (!TryAddMissingComponent(hand, out SkeletonDataProviderOVR skeletonDataProvider)
                || overridePresentValues)
            {
                SetPrivateValue(skeletonDataProvider, "ovrHand", ovrHand);
                SetPrivateValue(skeletonDataProvider, "ovrSkeleton", ovrSkeleton);
                skeletonData = skeletonDataProvider;
            }

            if (addSmoothSkeletonData)
            {
                if (!TryAddMissingComponent(hand, out ExtrapolationTrackingCleaner skeletonDataCleaner)
                    || overridePresentValues)
                {
                    SetPrivateValue(skeletonDataCleaner, "wrapee", skeletonDataProvider);
                }
                skeletonData = skeletonDataCleaner;
            }

            return new HandTrackingProviders()
            {
                handeness = handeness,
                ovrHand = ovrHand,
                ovrSkeleton = ovrSkeleton,
                skeletonDataProvider = skeletonData
            };
        }

        private void CreatePuppetedHand(HandTrackingProviders provider)
        {
            GameObject handInstance;
            GameObject handModel = provider.handeness == Handeness.Left ? leftHand : rightHand;
            if (PrefabUtility.IsPartOfPrefabAsset(handModel))
            {
                if (handModel != null)
                {
                    handInstance = GameObject.Instantiate(handModel, ovrRig.transform.parent);
                }
                else
                {
                    handInstance = new GameObject($"Puppeted_Hand_{provider.handeness}");
                    handInstance.transform.SetParent(ovrRig.transform.parent);
                }
            }
            else
            {
                handInstance = handModel;
            }


            AttachGripPoint(handInstance, ref provider);
            HandPuppet puppet = AttachPuppet(handInstance, provider);
            AttachGrabber(handInstance, provider);
            AttachSnapper(handInstance);

            if (controllerSupport)
            {
                AttachAnimator(handInstance, puppet, provider);
            }

        }

        private Transform AttachGripPoint(GameObject handInstance, ref HandTrackingProviders provider)
        {
            Transform gripPoint = DeepFindChildContaining(handInstance.transform, null, "grip");
            if (gripPoint == null)
            {
                Transform centre = DeepFindChildContaining(handInstance.transform, null, "palm", "center");
                if (centre != null)
                {
                    gripPoint = new GameObject("GripPoint").transform;
                    gripPoint.SetParent(handInstance.transform);
                    gripPoint.position = centre.position;

                    Transform middleTip = DeepFindChildContaining(handInstance.transform, null, "middle", "tip");
                    if (middleTip != null)
                    {
                        Vector3 palmUp = provider.handeness == Handeness.Right ? Vector3.up : Vector3.down;
                        gripPoint.rotation = Quaternion.LookRotation((middleTip.position - gripPoint.position).normalized, palmUp);
                    }
                    else
                    {
                        gripPoint.rotation = centre.rotation;
                    }
                }
            }

            provider.gripPoint = gripPoint;

            if (gripPoint == null)
            {
                Debug.LogError($"Grip Transform not found under {handInstance.name}. Please add one.", handInstance);
            }

            return gripPoint;
        }

        private HandPuppet AttachPuppet(GameObject handInstance, HandTrackingProviders provider)
        {
            bool puppetPresent = TryAddMissingComponent(handInstance, out HandPuppet puppet);

            SetPrivateValue(puppet, "skeleton", provider.skeletonDataProvider);
            SetPrivateValue(puppet, "updateNotifier", provider.updateNotifier);
            SetPrivateValue(puppet, "controllerAnchor", provider.handeness == Handeness.Left ? ovrRig.leftControllerAnchor : ovrRig.rightControllerAnchor);
            SetPrivateValue(puppet, "handeness", provider.handeness);

            if (puppetPresent && !overridePresentValues)
            {
                Debug.Log("HandPuppet found, skipping AttachPuppet.");
                return puppet;
            }

            SetPrivateValue(puppet, "gripPoint", provider.gripPoint);
            SetPrivateValue(puppet, "autoAdjustScale", false);


            HandMap handMap = new HandMap();
            if (provider.handeness == Handeness.Right)
            {
                handMap.rotationOffset = -handMap.rotationOffset;
            }
            SetPrivateValue(puppet, "controllerOffset", handMap);
            SetPrivateValue(puppet, "boneMaps", AutoAsignBones(handInstance));

            return puppet;
        }

        private void AttachGrabber(GameObject handInstance, HandTrackingProviders provider)
        {
            if (!overridePresentValues)
            {
                if (handInstance.GetComponentInChildren<IGrabNotifier>() != null)
                {
                    Debug.Log("Grabber implementing IGrabNotifier found, skipping AttachGrabber.");
                    BaseGrabber baseGrabber = handInstance.GetComponentInChildren<BaseGrabber>();
                    if (baseGrabber != null)
                    {
                        SetPrivateValue(baseGrabber, "updateNotifier", provider.updateNotifier);
                    }
                    if (baseGrabber is GrabberHybridOVR)
                    {
                        UpdateFlexInterfaces(baseGrabber as GrabberHybridOVR, provider, false);
                    }
                    return;
                }
                else
                {
                    Debug.LogWarning("No Grabber implementing IGrabNotifier present, adding one");
                }
            }

            TryAddMissingComponent(handInstance, out GrabberHybridOVR grabber);
            SetPrivateValue(grabber, "updateNotifier", provider.updateNotifier);
            SetPrivateValue(grabber, "gripTransform", provider.gripPoint);

            AttachCapsuleTriggers(grabber, provider);
            UpdateFlexInterfaces(grabber, provider, true);
        }

        private Snapper AttachSnapper(GameObject handInstance)
        {
            TryAddMissingComponent<Snapper>(handInstance, out Snapper snapper);
            return snapper;
        }


        private AnimatedHandOVR AttachAnimator(GameObject handInstance, HandPuppet puppet, HandTrackingProviders provider)
        {
            bool animatedHandPresent = TryAddMissingComponent(handInstance, out AnimatedHandOVR animatedHand);
            if (animatedHandPresent && !overridePresentValues)
            {
                Debug.Log("AnimatedHandOVR found, skipping AttachAnimator.");
                return animatedHand;
            }

            SetPrivateValue(animatedHand, "m_controller", provider.handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);

            Animator animator = animatedHand.GetComponentInChildren<Animator>();
            SetPrivateValue(animatedHand, "m_animator", animator);
            SetPrivateValue(animatedHand, "m_defaultGrabPose", null);


            var onUsingHands = GetPrivateValue<UnityEngine.Events.UnityEvent>(puppet, "OnUsingHands");
            onUsingHands?.AddListener(() => animatedHand.enabled = false);
            var onUsingControllers = GetPrivateValue<UnityEngine.Events.UnityEvent>(puppet, "OnUsingControllers");
            onUsingHands?.AddListener(() => animatedHand.enabled = true);
            return animatedHand;
        }

        private void AttachCapsuleTriggers(BaseGrabber grabber, HandTrackingProviders provider)
        {
            Collider[] colliders = GetPrivateValue<Collider[]>(grabber, "grabVolumes");
            if (colliders == null
                || colliders.Length == 0
                || colliders.All(c => c == null))
            {
                Debug.LogWarning($"No grab colliders found on {grabber.name}, adding some but please adjust manually if needed.", grabber.gameObject);
                GameObject grabVolume = new GameObject("GrabVolume");
                grabVolume.transform.SetParent(grabber.transform, false);
                if (provider.gripPoint != null)
                {
                    grabVolume.transform.SetPositionAndRotation(provider.gripPoint.position, provider.gripPoint.rotation);
                }
                else
                {
                    Debug.LogError($"No Grip Transform present when adding Grab Volumes on {grabber.name}, please adjust manually.", grabber.gameObject);
                }

                CapsuleCollider capsule = grabVolume.AddComponent<CapsuleCollider>();
                float capsuleSize = 0.04f;
                capsule.direction = 0;
                capsule.center = new Vector3(0f, -capsuleSize, 0f);
                capsule.height = capsuleSize * 3;
                capsule.radius = capsuleSize;
                capsule.isTrigger = true;
                SetPrivateValue(grabber, "grabVolumes", new Collider[] { capsule });
            }
        }


        private void UpdateFlexInterfaces(GrabberHybridOVR grabber, HandTrackingProviders provider, bool addMissing)
        {
            List<FlexInterface> flexInterfaces = new List<FlexInterface>();
            if (controllerSupport && addMissing)
            {
                TryAddMissingComponent(grabber.gameObject, out ControllerFlex controllerFlex);
                SetPrivateValue(controllerFlex, "controller", provider.handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
                flexInterfaces.Add(controllerFlex);
            }

            PinchTriggerFlex pinchFlex = null;
            if (addMissing)
            {
                TryAddMissingComponent(grabber.gameObject, out pinchFlex);
            }
            else
            {
                pinchFlex = grabber.GetComponentInChildren<PinchTriggerFlex>();
            }
            if (pinchFlex != null)
            {
                SetPrivateValue(pinchFlex, "flexHand", provider.ovrHand);
            }

            SphereGrabFlex sphereFlex = null;
            if (addMissing)
            {
                if (!TryAddMissingComponent(grabber.gameObject, out sphereFlex))
                {
                    sphereFlex.SetVolumeOffset(provider.gripPoint);
                }
            }
            else
            {
                sphereFlex = grabber.GetComponentInChildren<SphereGrabFlex>();
            }

            if (sphereFlex != null)
            {
                SetPrivateValue(sphereFlex, "flexHand", provider.ovrHand);
                SetPrivateValue(sphereFlex, "skeleton", provider.ovrSkeleton);
            }

            if (addMissing)
            {
                TryAddMissingComponent(grabber.gameObject, out SphereGrabPinchFlex sphereAndPinchFlex);
                flexInterfaces.Add(sphereAndPinchFlex);
            }

            if (addMissing)
            {
                Component[] serializableFlexInterfaces = flexInterfaces.Select(fi => (Component)fi).ToArray();
                SetPrivateValue(grabber, "flexInterfaces", serializableFlexInterfaces);
            }
        }

        private List<BoneMap> AutoAsignBones(GameObject handInstance)
        {
            SkinnedMeshRenderer skinnedHand = handInstance.GetComponentInChildren<SkinnedMeshRenderer>();

            if (skinnedHand == null)
            {
                return null;
            }

            List<BoneMap> maps = new List<BoneMap>();
            Transform root = skinnedHand.rootBone;
            Regex regEx = new Regex(@"_(\w*)(\d)");

            foreach (var bone in Enum.GetValues(typeof(BoneId)))
            {
                Match match = regEx.Match(bone.ToString());
                if (match != Match.Empty)
                {
                    string boneName = match.Groups[1].Value.ToLower();
                    string boneNumber = match.Groups[2].Value;
                    Transform skinnedBone = DeepFindChildContaining(root, "col", boneName, boneNumber);

                    if (skinnedBone != null)
                    {
                        maps.Add(new BoneMap()
                        {
                            id = (BoneId)bone,
                            transform = skinnedBone,
                            rotationOffset = Vector3.zero
                        });
                    }
                }
            }
            return maps;
        }

        #region utilities

        private static bool TryAddMissingComponent<T>(GameObject go, out T comp) where T : Component
        {
            comp = go.GetComponentInChildren<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
                return false;
            }
            return true;

        }

        private static T AddMissingComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponentInChildren<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private static void SetPrivateValue(object instance, string fieldName, object value)
        {
            FieldInfo fieldData = GetPrivateField(instance, fieldName);
            fieldData.SetValue(instance, value);
        }

        private static T GetPrivateValue<T>(object instance, string fieldName)
        {
            FieldInfo fieldData = GetPrivateField(instance, fieldName);
            return (T)fieldData.GetValue(instance);
        }

        private static FieldInfo GetPrivateField(object instance, string fieldName)
        {
            Type type = instance.GetType();

            FieldInfo fieldData = null;
            while (type != null
                && fieldData == null)
            {
                fieldData = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            return fieldData;
        }

        private Transform DeepFindChildContaining(Transform root, string ignorePattern, params string[] args)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                string childName = child.name.ToLower();

                bool shouldCheck = string.IsNullOrEmpty(ignorePattern) || !childName.Contains(ignorePattern);
                if (shouldCheck)
                {
                    bool containsAllArgs = args.All(a => childName.Contains(a));
                    Transform result = containsAllArgs ? child : DeepFindChildContaining(child, ignorePattern, args);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
        #endregion

        private class HandTrackingProviders
        {
            public Handeness handeness;
            public Transform gripPoint;

            public OVRHand ovrHand;
            public OVRSkeleton ovrSkeleton;

            public SkeletonDataProvider skeletonDataProvider;
            public AnchorsUpdateNotifier updateNotifier;
        }
    }
}