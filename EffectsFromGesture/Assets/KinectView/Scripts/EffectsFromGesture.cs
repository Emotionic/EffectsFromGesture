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
    private VisualGestureBuilderDatabase _GestureDatabase;
    private VisualGestureBuilderFrameSource _GestureFrameSource;
    private VisualGestureBuilderFrameReader _GestureFrameReader;

    private Dictionary<Gesture, DiscreteGestureResult> _DiscreteGestureResults;
    private List<Gesture> _Gestures = new List<Gesture>();
    private Gesture _Jump;
    private Gesture _JumpProgress;

    private bool _IsAddGesture = false;
    private int _InitTime = 200;
    private int _InitCount = 0;

    // Use this for initialization
    void Start ()
    {
        _GestureDatabase = VisualGestureBuilderDatabase.Create(@"./Gestures/Jump.gbd");
        
        foreach (var gesture in _GestureDatabase.AvailableGestures)
        {
            switch (gesture.Name)
            {
                case "Jump":
                    _Jump = gesture;
                    break;
                case "JumpProgress":
                    _JumpProgress = gesture;
                    break;
            }

            _Gestures.Add(gesture);
        }

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (BodySourceManager == null)
            return;

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();

        if (_BodyManager == null)
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
            FindValidBody();
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
            _GestureFrameSource.TrackingId = 0;
            _GestureFrameReader.IsPaused = true;
        }
    }


    private void _GestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
    {
        if (_InitCount < _InitTime)
        {
            Debug.Log(_InitCount);
            _InitCount++;
            return;
        }

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
                        // Jumpした
                        GameObject body = GameObject.Find("Body:" + _GestureFrameSource.TrackingId);
                        Vector3 pos = body.transform.Find(JointType.SpineBase.ToString()).transform.position;
                        EffekseerSystem.PlayEffect("MagicArea", pos);
                        _InitCount = 0;
                    }
                }
            }

        }

    }
}
