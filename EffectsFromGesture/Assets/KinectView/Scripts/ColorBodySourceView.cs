using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class ColorBodySourceView : MonoBehaviour 
{
    public bool BodyView;
    public Material BoneMaterial;
    public GameObject BodySourceManager;

    public GameObject[] GetBodies()
    {
        GameObject[] bodies = new GameObject[_Bodies.Count];

        _Bodies.Values.CopyTo(bodies, 0);

        return bodies;
    }

    public GameObject GetBody(ulong id)
    {
        if (_Bodies.ContainsKey(id))
            return _Bodies[id];
        else
            return null;
    }
    
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    public Camera ConvertCamera;
    
    private Kinect.CoordinateMapper _CoordinateMapper;
    private int _KinectWidth = 1920;
    private int _KinectHeight = 1080;
    
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };
    
    void Update () 
    {
        if (BodySourceManager == null)
        {
            return;
        }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }
        
        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }
                
            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
            
            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }

        if(_CoordinateMapper == null)
        {
            _CoordinateMapper = _BodyManager.Sensor.CoordinateMapper;
        }
    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);
        
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = BodyView
                ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                : new GameObject();
            
            if(BodyView)
            {
                LineRenderer lr = jointObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.material = BoneMaterial;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
            }

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }
        
        return body;
    }
    
    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;
            
            if(_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyObject.transform.Find(jt.ToString());
            jointObj.localPosition = GetVector3FromJoint(sourceJoint);

            if (BodyView)
            {
                LineRenderer lr = jointObj.GetComponent<LineRenderer>();

                if (targetJoint.HasValue)
                {
                    lr.SetPosition(0, jointObj.localPosition);
                    lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                    lr.startColor = GetColorForState(sourceJoint.TrackingState);
                    lr.endColor = GetColorForState(targetJoint.Value.TrackingState);
                }
                else
                {
                    lr.enabled = false;
                }
            }
        }
    }
    
    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }
    
    private Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        // 変換用カメラがあり、関節がトラッキング中
        if(ConvertCamera != null || (joint.TrackingState != Kinect.TrackingState.NotTracked))
        {
            // 関節座標をカラー画像座標に変換
            var kinectColorPoint = _CoordinateMapper.MapCameraPointToColorSpace(joint.Position);

            // Kinectカラー座標からUnity座標に変換
            Vector3 unityPoint = new Vector3(kinectColorPoint.X, kinectColorPoint.Y, 0);
            if((0 <= unityPoint.x) && (unityPoint.x < _KinectWidth) &&
               (0 <= unityPoint.y) && (unityPoint.y < _KinectHeight))
            {
                // スクリーンサイズを調整
                unityPoint.x = unityPoint.x * Screen.width / _KinectWidth;
                unityPoint.y = unityPoint.y * Screen.height / _KinectHeight;

                // Unityの座標に変換(同じ大きさに合わせた変換用カメラを使用)
                Vector3 colorPoint3 = ConvertCamera.ScreenToWorldPoint(unityPoint);

                // 向きを合わせる
                colorPoint3.x *= -1;
                colorPoint3.y *= -1;
                colorPoint3.z = -1;

                return colorPoint3;
            }
        }

        // ない場合仕方なく今まで通り
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, -1);
    }
}
