using UnityEngine;

[System.Serializable]
public class CylinderHandle 
{
    [SerializeField]
    private Vector3 _startPoint;
    [SerializeField]
    private Vector3 _endPoint;

    public float angle = 230f;
    public float radious = 0.2f;

    [SerializeField]
    [HideInInspector]
    private Transform _transform;


    public CylinderHandle(Transform transform)
    {
        this._transform = transform;

        _startPoint = Vector3.up * radious;
        _endPoint = Vector3.down * radious;
    }

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
            return this._transform.TransformPoint(_startPoint);
        }
        set
        {
            _startPoint = this._transform.InverseTransformPoint(value);
        }
    }

    public Vector3 EndPoint
    {
        get
        {
            return this._transform.TransformPoint(_endPoint);
        }
        set
        {
            _endPoint = this._transform.InverseTransformPoint(value);
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
                return this._transform.up;
            }
            return dir.normalized;
        }
    }



}
