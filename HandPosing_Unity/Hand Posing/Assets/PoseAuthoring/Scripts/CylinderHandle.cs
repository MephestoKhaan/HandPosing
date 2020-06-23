using System;
using UnityEngine;

[System.Serializable]
public class CylinderHandle 
{
    [SerializeField]
    private Vector3 _startPoint;
    [SerializeField]
    private Vector3 _endPoint;
    [SerializeField]
    private float _angle = 230f;

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

    public Vector3 StartAngleDir
    {
        get
        {
            return Vector3.Cross(Direction, Vector3.forward).normalized;
        }
    }
    public Vector3 EndAngleDir
    {
        get
        {
            return Quaternion.AngleAxis(Angle, Direction) * StartAngleDir;
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

    public float Angle
    {
        get
        {
            return _angle;
        }
        set
        {
            _angle = Mathf.Repeat(value, 360f);
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

    public void MakeSinglePoint()
    {
        _startPoint = _endPoint = Vector3.zero;
        radious = 0f;
        Angle = 0f;
    }

    public Vector3 NearestPointInSurface(Vector3 targetPosition)
    {
        Vector3 start = StartPoint;
        Vector3 dir = Direction;
        Vector3 projectedVector = Vector3.Project(targetPosition - start, dir);
        
        if(projectedVector.magnitude > Height)
        {
            projectedVector = projectedVector.normalized * Height;
        }
        if (Vector3.Dot(projectedVector, dir) < 0f)
        {
            projectedVector = Vector3.zero;
        }

        Vector3 projectedPoint = StartPoint + projectedVector;
        Vector3 targetDirection = Vector3.ProjectOnPlane((targetPosition - projectedPoint), dir).normalized;
        //clamp of the surface
        float desiredAngle = Mathf.Repeat(Vector3.SignedAngle(StartAngleDir, targetDirection, dir),360f);
        if(desiredAngle > Angle)
        {
            if(Mathf.Abs(desiredAngle - Angle) >= Mathf.Abs(360f - desiredAngle))
            {
                targetDirection = StartAngleDir;
            }
            else
            {
                targetDirection = EndAngleDir;
            }
        }

        Vector3 surfacePoint = projectedPoint + targetDirection * radious;
        return surfacePoint;
    }


}
