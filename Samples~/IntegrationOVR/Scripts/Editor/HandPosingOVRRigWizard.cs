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
        [SerializeField]
        private OVRCameraRig ovrRig;

        [SerializeField]
        private bool controllerSupport;

        [Header("Optional")]
        [SerializeField]
        private GameObject leftHandModel;
        [SerializeField]
        private GameObject rightHandModel;

        [MenuItem("HandPosing/Add HandPosing to OVRRig")]
        private static void CreateWizard()
        {
            HandPosingOVRRigWizard wizard = ScriptableWizard.DisplayWizard<HandPosingOVRRigWizard>("Add HandPosing to OVRRig", "Close", "Add HandPosing");
            wizard.ResetFields();
        }

        private void ResetFields()
        {
            ovrRig = GameObject.FindObjectOfType<OVRCameraRig>(false);

            leftHandModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus/VR/Meshes/HandTracking/OculusHand_L.fbx");
            rightHandModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus/VR/Meshes/HandTracking/OculusHand_R.fbx");
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
                if(rigPrefab != null)
                {
                    GameObject rigGameObject = GameObject.Instantiate(rigPrefab);
                    rigGameObject.TryGetComponent<OVRCameraRig>(out ovrRig);
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
            OVRHand.Hand ovrHandeness = handeness == Handeness.Left ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight;
            OVRHand ovrHand = AddMissingComponent<OVRHand>(hand);
            SetPrivateValue(ovrHand, "HandType", ovrHandeness);

            OVRSkeleton.SkeletonType skeletonHandeness = handeness == Handeness.Left ? OVRSkeleton.SkeletonType.HandLeft : OVRSkeleton.SkeletonType.HandRight;
            OVRSkeleton ovrSkeleton = AddMissingComponent<OVRSkeleton>(hand);
            SetPrivateValue(ovrSkeleton, "_skeletonType", skeletonHandeness);

            SkeletonDataProviderOVR skeletonDataProvider = AddMissingComponent<SkeletonDataProviderOVR>(hand);
            SetPrivateValue(skeletonDataProvider, "ovrHand", ovrHand);
            SetPrivateValue(skeletonDataProvider, "ovrSkeleton", ovrSkeleton);

            ExtrapolationTrackingCleaner skeletonDataCleaner = AddMissingComponent<ExtrapolationTrackingCleaner>(hand);
            SetPrivateValue(skeletonDataCleaner, "wrapee", skeletonDataProvider);

            return new HandTrackingProviders()
            {
                handeness = handeness,
                ovrHand = ovrHand,
                ovrSkeleton = ovrSkeleton,
                skeletonDataProvider = skeletonDataCleaner
            };
        }

        private void CreatePuppetedHand(HandTrackingProviders provider)
        {
            GameObject handInstance;
            GameObject handPrefab = provider.handeness == Handeness.Left ? leftHandModel : rightHandModel;
            if (handPrefab != null)
            {
                handInstance = GameObject.Instantiate(handPrefab, ovrRig.transform.parent);
            }
            else
            {
                handInstance = new GameObject($"Puppeted_Hand_{provider.handeness}");
                handInstance.transform.SetParent(ovrRig.transform.parent);
            }

            provider.gripPoint = AttachGripPoint(handInstance);
            HandPuppet puppet = AttachPuppet(handInstance, provider);
            AttachGrabber(handInstance, provider);
            AttachSnapper(handInstance);

            if (controllerSupport)
            {
                AttachAnimator(handInstance, puppet, provider);
            }

        }

        private Transform AttachGripPoint(GameObject handInstance)
        {
            Transform gripPoint = DeepFindChildContaining(handInstance.transform, "grip");
            if(gripPoint == null)
            {
                Transform marker = DeepFindChildContaining(handInstance.transform, "palm_center_marker");
                if(marker != null)
                {
                    gripPoint = new GameObject("GripPoint").transform;
                    gripPoint.SetParent(handInstance.transform);
                    gripPoint.SetPositionAndRotation(marker.position, marker.rotation);
                }
            }

            return gripPoint;
        }

        private HandPuppet AttachPuppet(GameObject handInstance, HandTrackingProviders provider)
        {
            HandPuppet puppet = AddMissingComponent<HandPuppet>(handInstance);
            SetPrivateValue(puppet, "skeleton", provider.skeletonDataProvider);
            SetPrivateValue(puppet, "updateNotifier", provider.updateNotifier);
            SetPrivateValue(puppet, "gripPoint", provider.gripPoint);
            SetPrivateValue(puppet, "handeness", provider.handeness);
            SetPrivateValue(puppet, "autoAdjustScale", false);
            SetPrivateValue(puppet, "controllerAnchor", provider.handeness == Handeness.Left ? ovrRig.leftControllerAnchor : ovrRig.rightControllerAnchor);


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
            GrabberHybridOVR grabber = AddMissingComponent<GrabberHybridOVR>(handInstance);
            SetPrivateValue(grabber, "gripTransform", provider.gripPoint);

            List<FlexInterface> flexInterfaces = AddFlexInterfaces(handInstance, provider);
            Component[] serializableFlexInterfaces = flexInterfaces.Select(fi => (Component)fi).ToArray();
            SetPrivateValue(grabber, "flexInterfaces", serializableFlexInterfaces);
            SetPrivateValue(grabber, "updateNotifier", provider.updateNotifier);
        }

        private Snapper AttachSnapper(GameObject handInstance)
        {
            Snapper snapper = AddMissingComponent<Snapper>(handInstance);
            return snapper;
        }


        private AnimatedHandOVR AttachAnimator(GameObject handInstance, HandPuppet puppet, HandTrackingProviders provider)
        {
            AnimatedHandOVR animatedHand = AddMissingComponent<AnimatedHandOVR>(handInstance);
            SetPrivateValue(animatedHand, "m_controller", provider.handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
            //TODO: Should go into Reset() ?
            Animator animator = animatedHand.GetComponentInChildren<Animator>();
            SetPrivateValue(animatedHand, "m_animator", animator);
            SetPrivateValue(animatedHand, "m_defaultGrabPose", null); //TODO: set in the wizard?


            var onUsingHands = GetPrivateValue<UnityEngine.Events.UnityEvent>(puppet, "OnUsingHands");
            onUsingHands?.AddListener(() => animatedHand.enabled = false);
            var onUsingControllers = GetPrivateValue<UnityEngine.Events.UnityEvent>(puppet, "OnUsingControllers");
            onUsingHands?.AddListener(() => animatedHand.enabled = true);
            return animatedHand;
        }


        private List<FlexInterface> AddFlexInterfaces(GameObject handInstance, HandTrackingProviders provider)
        {
            List<FlexInterface> flexInterfaces = new List<FlexInterface>();
            if (controllerSupport)
            {
                ControllerFlex controllerFlex = AddMissingComponent<ControllerFlex>(handInstance);
                SetPrivateValue(controllerFlex, "controller", provider.handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
                flexInterfaces.Add(controllerFlex);
            }

            PinchTriggerFlex pinchFlex = AddMissingComponent<PinchTriggerFlex>(handInstance);
            SetPrivateValue(pinchFlex, "flexHand", provider.ovrHand);

            SphereGrabFlex sphereFlex = AddMissingComponent<SphereGrabFlex>(handInstance);
            SetPrivateValue(sphereFlex, "flexHand", provider.ovrHand);
            SetPrivateValue(sphereFlex, "skeleton", provider.ovrSkeleton);
            sphereFlex.SetVolumeOffset(provider.gripPoint);
            //TODO pose volume offset should be the Grip point?


            SphereGrabPinchFlex sphereAndPinchFlex = AddMissingComponent<SphereGrabPinchFlex>(handInstance);
            //populated on Reset()

            flexInterfaces.Add(sphereAndPinchFlex);

            return flexInterfaces;
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
                    Transform skinnedBone = DeepFindChildContaining(root, "col", "{0}{1}", boneName, boneNumber);

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
        private static T AddMissingComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out T comp))
            {
                return comp;
            }
            else
            {
                return go.AddComponent<T>();
            }
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

        private Transform DeepFindChildContaining(Transform root, string name)
        {
            return DeepFindChildContaining(root, null,null, name);
        }

        private Transform DeepFindChildContaining(Transform root, string ignorePattern, string format, params string[] args)
        {
            if(root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                string childName = child.name.ToLower();

                bool shouldCheck = string.IsNullOrEmpty(ignorePattern) || !childName.Contains(ignorePattern);
                if(shouldCheck)
                {
                    Transform result = null;
                    if (string.IsNullOrEmpty(format))
                    {
                        result = childName.Contains(args[0]) ? child : DeepFindChildContaining(child, ignorePattern, format, args);
                    }
                    else
                    {
                        if (childName.Contains(args[0]))
                        {
                            string fullName = string.Format(format, args);
                            result = childName.Contains(fullName) ? child : DeepFindChildContaining(child, ignorePattern, format, args);
                        }
                    }
                    if(result != null)
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