using UnityEngine;

public class CylinderHandle : MonoBehaviour
{
    public Vector3 StartPoint
    {
        get
        {
            return this.transform.position + Vector3.up * 0.5f;
        }
    }
    public Vector3 EndPoint
    {
        get
        {
            return this.transform.position - Vector3.up * 0.5f;
        }
    }

    public Vector3 Tangent
    {
        get
        {
            return Vector3.Cross(Direction, Vector3.forward).normalized;
        }
    }
    public float angle = 270f;
    public float radious;

    public Vector3 Direction
    {
        get
        {
            Vector3 dir = (EndPoint - StartPoint);
            if(dir.sqrMagnitude == 0f)
            {
                return this.transform.up;
            }
            return dir.normalized;
        }
    }

    

}
