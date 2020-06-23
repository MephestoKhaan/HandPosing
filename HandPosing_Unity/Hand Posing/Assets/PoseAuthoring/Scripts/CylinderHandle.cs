using UnityEngine;

public class CylinderHandle : MonoBehaviour
{
    [SerializeField]
    private Vector3 _startPoint;
    [SerializeField]
    private Vector3 _endPoint;

    public float angle = 230f;
    public float radious = 0.2f;

    public Vector3 Tangent
    {
        get
        {
            return Vector3.Cross(Direction, Vector3.forward).normalized;
        }
    }

    public Vector3 StartPoint
    {
        get
        {
            return this.transform.TransformPoint(_startPoint);
        }
        set
        {
            _startPoint = this.transform.InverseTransformPoint(value);
        }
    }

    public Vector3 EndPoint
    {
        get
        {
            return this.transform.TransformPoint(_endPoint);
        }
        set
        {
            _endPoint = this.transform.InverseTransformPoint(value);
        }
    }

    public float Height
    {
        get
        {
            return (EndPoint - StartPoint).magnitude;
        }
    }

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

    private void Reset()
    {
        _startPoint =  Vector3.up * radious;
        _endPoint =   Vector3.down * radious;
    }



}
