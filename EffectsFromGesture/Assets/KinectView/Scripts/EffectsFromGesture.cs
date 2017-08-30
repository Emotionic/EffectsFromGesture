﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Effekseer;

using Kinect = Windows.Kinect;

public class EffectsFromGesture : MonoBehaviour
{
    public GameObject BodySourceManager;
    public Material TrailMaterial;

    private BodySourceManager _BodyManager;
    private ColorBodySourceView _ColorBodyView;
    private VisualGestureBuilderDatabase _GestureDatabase;
    private List<VisualGestureBuilderFrameSource> _GestureFrameSourcesList;
    private List<VisualGestureBuilderFrameReader> _GestureFrameReadersList;
    private List<ulong> _TrackedIds;
    
    private Dictionary<Gesture, DiscreteGestureResult> _DiscreteGestureResults;
    private List<Gesture> _Gestures = new List<Gesture>();
    private Gesture _Jump;
    
    private bool _IsAddGesture = false;
    private bool _IsSetEvent = false;
    private const string _EffectName = "StairBroken";
    private const int AcquirableBodyNumber = 6;

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

        _GestureFrameSourcesList = new List<VisualGestureBuilderFrameSource>(AcquirableBodyNumber);
        _GestureFrameReadersList = new List<VisualGestureBuilderFrameReader>(AcquirableBodyNumber);
        _TrackedIds = new List<ulong>();
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
            Gesture[] gestures = _Gestures.ToArray();

            for (int i = 0; i < AcquirableBodyNumber; i++)
            {
                _GestureFrameSourcesList.Add(VisualGestureBuilderFrameSource.Create(_BodyManager.Sensor, 0));

                if (_GestureFrameSourcesList[i] == null)
                    continue;

                _GestureFrameReadersList.Add(_GestureFrameSourcesList[i].OpenReader());

                _GestureFrameSourcesList[i].AddGestures(gestures);

                _GestureFrameReadersList[i].IsPaused = true;
                // _GestureFrameReadersList[i].FrameArrived += _GestureFrameReader_FrameArrived;
                
                _IsAddGesture = true;
                Debug.Log("ADDED");
            }

        }

        FindValidBodys();
        
        foreach (GameObject body in _ColorBodyView.GetBodies())
        {
            AddingTrailRendererToBody(body);
        }

        for(int i = 0;i < AcquirableBodyNumber;i++)
        {
            if(_GestureFrameSourcesList[i].IsTrackingIdValid)
            {
                var gestureFrame = _GestureFrameReadersList[i].CalculateAndAcquireLatestFrame();

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
                            if (result.Confidence > 0.75)
                            {
                                Debug.Log("Confidence : " + result.Confidence);

                                // Jumpした
                                Vector3 pos = _ColorBodyView.GetBody
                                    (
                                        _GestureFrameSourcesList[i].TrackingId
                                    )
                                    .transform.Find(JointType.SpineMid.ToString()).transform.position;
                                
                                Debug.Log(pos);
                                EffekseerSystem.PlayEffect(_EffectName, pos);
                            }
                        }
                    }
                }
            }
        }
    }

    private void AddingTrailRendererToBody(GameObject body)
    {
        GameObject handTipLeft = body.transform.Find(JointType.HandTipRight.ToString()).gameObject;
        GameObject handTipRight = body.transform.Find(JointType.HandTipLeft.ToString()).gameObject;

        if (handTipLeft.GetComponent<TrailRenderer>() != null)
            return;

        TrailRenderer[] hands_tr =
        {
            handTipLeft.AddComponent<TrailRenderer>(),
            handTipRight.AddComponent<TrailRenderer>()
        };

        foreach (TrailRenderer hand_tr in hands_tr)
        {
            hand_tr.material = TrailMaterial;
            hand_tr.startWidth = 0.2f;
            hand_tr.endWidth = 0.05f;
            hand_tr.startColor = Color.red;
            hand_tr.endColor = new Color(255, 255, 255, 0);
            hand_tr.time = 0.5f;
        }
    }

    private void FindValidBodys()
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
                    if (_TrackedIds.Contains(body.TrackingId))
                        continue;

                    for (int i = 0; i < AcquirableBodyNumber; i++)
                    {
                        if (_GestureFrameSourcesList[i] == null || _GestureFrameReadersList[i] == null)
                            continue;
                        
                        if (!_GestureFrameSourcesList[i].IsTrackingIdValid)
                        {
                            SetBody(body.TrackingId, i);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void SetBody(ulong id, int i)
    {
        Debug.Log("List[" + i + "] : " + _GestureFrameSourcesList[i].TrackingId);
        if (_GestureFrameSourcesList[i].TrackingId > 0)
        {
            _TrackedIds.Remove(_GestureFrameSourcesList[i].TrackingId);
            Debug.Log("Resetbody : " + i + " : " + _GestureFrameSourcesList[i].TrackingId);
            _GestureFrameSourcesList[i].TrackingId = 0;
            _GestureFrameReadersList[i].IsPaused = true;
        }

        Debug.Log("Setbody : " + i + " : " + id);
        _GestureFrameSourcesList[i].TrackingId = id;
        _GestureFrameReadersList[i].IsPaused = false;
        _TrackedIds.Add(id);
    }
    
}
