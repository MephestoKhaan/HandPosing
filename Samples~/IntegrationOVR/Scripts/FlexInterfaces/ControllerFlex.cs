using HandPosing.OVRIntegration.GrabEngine;
using UnityEngine;

public class ControllerFlex : MonoBehaviour, FlexInterface
{
    [SerializeField]
    private OVRInput.Controller controller;


    [SerializeField]
    [Tooltip("Grab threshold, hand controller")]
    private Vector2 grabThresold = new Vector2(0.35f, 0.95f);

    private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
    private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

    public FlexType InterfaceFlexType => FlexType.Controller;

    public bool IsValid => true;

    public float? GrabStrength
    {
        get
        {
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        }
    }

    public Vector2 GrabThresold
    {
        get => grabThresold;
    }

    public Vector2 FailGrabThresold
    {
        get
        {
            Vector2 failThresold = GrabThresold;
            failThresold.x *= ALMOST_PINCH_LOWER_PERCENT;
            failThresold.y *= ALMOST_PINCH_UPPER_PERCENT;
            return failThresold;
        }
    }

    public float AlmostGrabRelease
    {
        get => GrabThresold.x;
    }
}
