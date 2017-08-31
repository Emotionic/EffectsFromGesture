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
    private Gesture _OpenMenu;
    private Gesture _Punch_Left;
    private Gesture _Punch_Right;
    
    private bool _IsAddGesture = false;
    private bool _IsSetEvent = false;
    private readonly string[] _EffectNames = { "StairBroken", "punch" };
    private const int AcquirableBodyNumber = 6;

    private float H = 0f;
    
    // Use this for initialization
    void Start ()
    {
        _GestureDatabase = VisualGestureBuilderDatabase.Create(@"./Gestures/Emotionic.gbd");
        
        foreach (var gesture in _GestureDatabase.AvailableGestures)
        {
            switch (gesture.Name)
            {
                case "Jump2":
                    _Jump = gesture;
                    break;
                case "OpenMenu":
                    _OpenMenu = gesture;
                    break;
                case "Punch_Left":
                    _Punch_Left = gesture;
                    break;
                case "Punch_Right":
                    _Punch_Right = gesture;
                    break;
            }

            _Gestures.Add(gesture);
        }

        // loadEffect
        foreach(var efkName in _EffectNames)
            EffekseerSystem.LoadEffect(efkName);

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
                
                _IsAddGesture = true;
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
                AddEffect(
                    _GestureFrameReadersList[i].CalculateAndAcquireLatestFrame(),
                    _GestureFrameSourcesList[i].TrackingId);
            }
        }
    }
    
    private void AddingTrailRendererToBody(GameObject body)
    {
        GameObject handTipLeft = body.transform.Find(JointType.HandTipRight.ToString()).gameObject;
        GameObject handTipRight = body.transform.Find(JointType.HandTipLeft.ToString()).gameObject;

        GameObject thumbLeft = body.transform.Find(JointType.AnkleRight.ToString()).gameObject;
        GameObject thumbRight = body.transform.Find(JointType.AnkleLeft.ToString()).gameObject;

        if (handTipLeft.GetComponent<TrailRenderer>() != null)
        {
            H += 0.01f;
            if (H > 1f)
                H = 0f;

            Color col = Color.HSVToRGB(H, 1, 1);
            handTipLeft.GetComponent<TrailRenderer>().startColor = col;
            handTipRight.GetComponent<TrailRenderer>().startColor = col;
            thumbLeft.GetComponent<TrailRenderer>().startColor = col;
            thumbRight.GetComponent<TrailRenderer>().startColor = col;

            return;
        }

        TrailRenderer[] hands_tr =
        {
            handTipLeft.AddComponent<TrailRenderer>(),
            handTipRight.AddComponent<TrailRenderer>(),
            thumbLeft.AddComponent<TrailRenderer>(),
            thumbRight.AddComponent<TrailRenderer>()
        };

        foreach (TrailRenderer hand_tr in hands_tr)
        {
            hand_tr.material = TrailMaterial;
            hand_tr.startWidth = 0.2f;
            hand_tr.endWidth = 0.05f;
            hand_tr.startColor = Color.HSVToRGB(H, 255, 255);
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

    private void AddEffect(VisualGestureBuilderFrame gestureFrame, ulong id)
    {
        if (gestureFrame == null || gestureFrame.DiscreteGestureResults == null)
            return;
        _DiscreteGestureResults = gestureFrame.DiscreteGestureResults;

        if (_DiscreteGestureResults == null)
            return;

        foreach (var result in _DiscreteGestureResults)
        {
            if (result.Value == null || !result.Value.Detected)
                continue;

            switch (result.Key.Name)
            {
                case "Jump02":

                    if (result.Value.Confidence < 0.5)
                        continue;

                    Debug.Log("Jump02 Confidence : " + result.Value.Confidence);

                    // Jumpした
                    Vector3 pos =
                        _ColorBodyView.GetBody(id).transform.Find(JointType.SpineMid.ToString()).transform.position;
                    
                    EffekseerSystem.PlayEffect(_EffectNames[0], pos);
                    
                    break;
                case "OpenMenu":

                    if (result.Value.Confidence < 0.5)
                        continue;

                    Debug.Log("OpenMenu Confidence : " + result.Value.Confidence);
                    break;

                case "Punch_Left":
                    Debug.Log("Punch Left" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        continue;

                    EffekseerSystem.PlayEffect(_EffectNames[1], _ColorBodyView.GetBody(id).transform.Find(JointType.HandRight.ToString()).transform.position);
                    break;

                case "Punch_Right":
                    Debug.Log("Punch Right" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        continue;

                    EffekseerSystem.PlayEffect(_EffectNames[1], _ColorBodyView.GetBody(id).transform.Find(JointType.HandLeft.ToString()).transform.position);
                    break;
            }

        }
    }
}
