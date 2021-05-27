using HandPosing.Interaction;
using HandPosing.OVRIntegration;
using HandPosing.OVRIntegration.GrabEngine;
using HandPosing.TrackingData;
using System;
using System.Collections.Generic;
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
        private GameObject leftHandPrefab;
        [SerializeField]
        private GameObject rightHandPrefab;

        [MenuItem("HandPosing/Add HandPosing to OVRRig")]
        private static void CreateWizard()
        {
            HandPosingOVRRigWizard wizard = ScriptableWizard.DisplayWizard<HandPosingOVRRigWizard>("Add HandPosing to OVRRig", "Close", "Add HandPosing");
            wizard.ResetFields();
        }

        private void ResetFields()
        {
            ovrRig = GameObject.FindObjectOfType<OVRCameraRig>(false);
        }

        private void OnWizardCreate() { }

        private void OnWizardOtherButton()
        {
            LinkUpdateNotifier(ovrRig.gameObject);
            LinkHandPosing(ovrRig);
        }


        private void LinkHandPosing(OVRCameraRig rig)
        {
            Transform leftHand = rig.leftHandAnchor;
            Transform rightHand = rig.rightHandAnchor;

            LinkHandData(leftHand.gameObject, Handeness.Left);
            LinkHandData(rightHand.gameObject, Handeness.Right);
        }

        private void LinkUpdateNotifier(GameObject rig)
        {
            UpdateNotifierOVR updateNotifier = AddMissingComponent<UpdateNotifierOVR>(rig);
            SetPrivateValue(updateNotifier, "ovrCameraRig", ovrRig);
        }

        private SkeletonDataProvider LinkHandData(GameObject hand, Handeness handeness)
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

            return skeletonDataCleaner;
        }

        private void CreatePuppetedHand(Handeness handeness, HandTrackingProviders provider)
        {
            GameObject handInstance = GameObject.Instantiate(handeness == Handeness.Left ? leftHandPrefab : rightHandPrefab, ovrRig.transform.parent);

            AnimatedHandOVR animatedHand = AddMissingComponent<AnimatedHandOVR>(handInstance);
            SetPrivateValue(animatedHand, "m_controller", handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
            //TODO: Should go into Reset() ?
            Animator animator = animatedHand.GetComponentInChildren<Animator>();
            SetPrivateValue(animatedHand, "m_animator", animator);
            SetPrivateValue(animatedHand, "m_defaultGrabPose", null); //TODO: set in the wizard?


            LinkPuppet(handInstance, handeness, provider);


            Snapper snapper = AddMissingComponent<Snapper>(handInstance);
            //Snapper values set on Reset
        }


        private HandPuppet LinkPuppet(GameObject handInstance, Handeness handeness, HandTrackingProviders provider)
        {
            HandPuppet puppet = AddMissingComponent<HandPuppet>(handInstance);
            SetPrivateValue(puppet, "skeleton", provider.skeletonDataProvider);
            SetPrivateValue(puppet, "updateNotifier", provider.updateNotifier);
            //SetPrivateValue(puppet, "gripPoint", );
            SetPrivateValue(puppet, "handeness", handeness);
            SetPrivateValue(puppet, "autoAdjustScale", false);
            SetPrivateValue(puppet, "controllerAnchor", handeness == Handeness.Left ? ovrRig.leftControllerAnchor : ovrRig.rightControllerAnchor);
            SetPrivateValue(puppet, "controllerOffset", new HandMap()); //TODO invert for right?
            //TODO what do to with bone maps
            SetPrivateValue(puppet, "boneMaps", new List<BoneMap>());
            //TODO how to link Animator events
            SetPrivateValue(puppet, "onUsingHands", null);
            SetPrivateValue(puppet, "onUsingControllers", null);

            return puppet;
        }


        private List<FlexInterface> AddFlexInterfaces(GameObject handInstance, Handeness handeness, HandTrackingProviders provider)
        {
            ControllerFlex controllerFlex = AddMissingComponent<ControllerFlex>(handInstance);
            SetPrivateValue(controllerFlex, "controller", handeness == Handeness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);

            PinchTriggerFlex pinchFlex = AddMissingComponent<PinchTriggerFlex>(handInstance);
            SetPrivateValue(pinchFlex, "flexHand", provider.ovrHand);

            SphereGrabFlex sphereFlex = AddMissingComponent<SphereGrabFlex>(handInstance);
            SetPrivateValue(sphereFlex, "flexHand", provider.ovrHand);
            SetPrivateValue(sphereFlex, "skeleton", provider.ovrSkeleton);
            //TODO pose volume offset should be the Grip point?


            SphereGrabPinchFlex sphereAndPinchFlex = AddMissingComponent<SphereGrabPinchFlex>(handInstance);
            //populated on Reset()

            return new List<FlexInterface>() { controllerFlex, sphereAndPinchFlex };
        }

        private void AutoAsignBones()
        {
            Regex pattern = new Regex(@"_(\w*\d)");
            foreach (var boneName in Enum.GetNames(typeof(BoneId)))
            {
                string clearName = pattern.Match(boneName)?.Value;
            }
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

        private static void SetPrivateValue(object instance, string variableName, object value)
        {
            Type type = instance.GetType();

            FieldInfo fieldData = type.GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldData.SetValue(instance, value);
        }
        #endregion

        private class HandTrackingProviders
        {
            public OVRHand ovrHand;
            public OVRSkeleton ovrSkeleton;

            public SkeletonDataProvider skeletonDataProvider;
            public AnchorsUpdateNotifier updateNotifier;
        }
    }
}