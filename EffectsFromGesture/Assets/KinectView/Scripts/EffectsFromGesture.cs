using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Effekseer;

using Kinect = Windows.Kinect;

public class EffectsFromGesture : MonoBehaviour
{
    public GameObject BodySourceManager;

    private BodySourceManager _BodyManager;
    private ColorBodySourceView _ColorBodyView;
    private VisualGestureBuilderDatabase _GestureDatabase;
    private VisualGestureBuilderFrameSource _GestureFrameSource;
    private VisualGestureBuilderFrameReader _GestureFrameReader;

    private Dictionary<Gesture, DiscreteGestureResult> _DiscreteGestureResults;
    private List<Gesture> _Gestures = new List<Gesture>();
    private Gesture _Jump;
    private GameObject _BodyObj;
    private Material _TrailMaterial;
    
    private bool _IsAddGesture = false;
    private bool _IsSetTrailRenderer = false;
    private const string _EffectName = "StairBroken";
    
    // Use this for initialization
    void Start ()
    {
        _GestureDatabase = VisualGestureBuilderDatabase.Create(@"./Gestures/SingleJump.gbd");
        
        foreach (var gesture in _GestureDatabase.AvailableGestures)
        {
            switch (gesture.Name)
            {
                case "Jump":
                    _Jump = gesture;
                    break;
            }

            _Gestures.Add(gesture);
        }

        // loadEffect
        EffekseerSystem.LoadEffect(_EffectName);

        // set material
        _TrailMaterial = new Material(Shader.Find("Sprites/Default"));
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (BodySourceManager == null)
            return;

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();

        if (_BodyManager == null || _ColorBodyView == null)
            return;

        if (!_IsAddGesture)
        {
            _GestureFrameSource = VisualGestureBuilderFrameSource.Create(_BodyManager.Sensor, 0);

            if (_GestureFrameSource == null)
                return;

            _GestureFrameReader = _GestureFrameSource.OpenReader();

            Gesture[] gestures = _Gestures.ToArray();
            _GestureFrameSource.AddGestures(gestures);
            
            _GestureFrameReader.IsPaused = true;
            _GestureFrameReader.FrameArrived += _GestureFrameReader_FrameArrived;
            
            _IsAddGesture = true;
            Debug.Log("ADDED");
        }

        if (_GestureFrameSource == null || _GestureFrameReader == null)
            return;

        if (!_GestureFrameSource.IsTrackingIdValid)
        {
            FindValidBody();
            _IsSetTrailRenderer = false;
        }
        else
        {
            Debug.Log(_GestureFrameSource.TrackingId);
            _BodyObj = GameObject.Find("Body:" + _GestureFrameSource.TrackingId);
            Debug.Log(_BodyObj.name);
        }

        if (!_IsSetTrailRenderer)
        {
            SetTrailRenderer();
        }
    }

    private void FindValidBody()
    {
        if(_BodyManager != null)
        {
            Kinect.Body[] bodies = _BodyManager.GetData();
            if (bodies == null)
                return;

            foreach(Body body in bodies)
            {
                if(body.IsTracked)
                {
                    SetBody(body.TrackingId);
                    break;
                }
            }
        }
    }

    private void SetBody(ulong id)
    {
        if(id > 0)
        {
            _GestureFrameSource.TrackingId = id;
            _GestureFrameReader.IsPaused = false;
        }
        else
        {
            _BodyObj = null;
            _GestureFrameSource.TrackingId = 0;
            _GestureFrameReader.IsPaused = true;
        }
    }

    private void SetTrailRenderer()
    {
        if (_BodyObj == null)
            return;

        TrailRenderer[] hands_tr =
        {
            _BodyObj.transform.Find(JointType.HandTipRight.ToString()).gameObject.AddComponent<TrailRenderer>(),
            _BodyObj.transform.Find(JointType.HandTipLeft.ToString()).gameObject.AddComponent<TrailRenderer>()
        };

        foreach (TrailRenderer hand_tr in hands_tr)
        {
            hand_tr.material = _TrailMaterial;
            hand_tr.startWidth = 0.1f;
            hand_tr.endWidth = 0.1f;
            hand_tr.startColor = Color.red;
            hand_tr.endColor = new Color(255, 255, 255, 0);
            hand_tr.time = 0.5f;
        }

        _IsSetTrailRenderer = true;
    }


    private void _GestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
    {
        if (!_GestureFrameSource.IsTrackingIdValid)
            return;

        using (var gestureFrame = e.FrameReference.AcquireFrame())
        {
            if (gestureFrame != null && gestureFrame.DiscreteGestureResults != null)
            {
                _DiscreteGestureResults = gestureFrame.DiscreteGestureResults;
                
                if (_DiscreteGestureResults != null && _DiscreteGestureResults.ContainsKey(_Jump))
                {
                    var result = _DiscreteGestureResults[_Jump];

                    if (result == null)
                        return;

                    if (result.Detected == true)
                    {
                        Debug.Log("Confidence : "+  result.Confidence);
                        if(result.Confidence > 0.75)
                        {
                            // Jumpした
                            Vector3 pos = _BodyObj.transform.Find(JointType.SpineMid.ToString()).transform.position;
                            Debug.Log(pos);
                            EffekseerSystem.PlayEffect(_EffectName, pos);
                        }
                    }
                }
            }

        }

    }
}
