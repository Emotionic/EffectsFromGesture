using UnityEngine;
using System.Collections;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;

using Kinect = Windows.Kinect;

namespace Assets.KinectView.Scripts
{
    class EffectFromGesture
    {
        public GameObject BodySourceManager;

        private BodySourceManager _BodyManager;
        private VisualGestureBuilderDatabase _GestureDatabase;
        private VisualGestureBuilderFrameSource _GestureFrameSource;
        private VisualGestureBuilderFrameReader _GestureFrameReader;

        private Gesture _Jump;
        private Gesture _JumpProgress;

        private void Start()
        {
            _GestureDatabase = VisualGestureBuilderDatabase.Create(@"../../Gestures/Jump.gdb");
            _GestureFrameSource = VisualGestureBuilderFrameSource.Create(_BodyManager.Sensor, 0);

            foreach(var gesture in _GestureDatabase.AvailableGestures)
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

                _GestureFrameSource.AddGesture(gesture);
            }

            _GestureFrameReader = _GestureFrameSource.OpenReader();
            _GestureFrameReader.IsPaused = true;
            _GestureFrameReader.FrameArrived += _GestureFrameReader_FrameArrived;
        }

        private void Update()
        {
            if (BodySourceManager == null)
                return;

            _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();

            if (_BodyManager == null)
                return;

            Kinect.Body[] bodies = _BodyManager.GetData();

            if(!_GestureFrameSource.IsTrackingIdValid)
            {
                foreach(var body in bodies)
                {
                    if(body != null && body.IsTracked)
                    {
                        _GestureFrameSource.TrackingId = body.TrackingId;
                        _GestureFrameReader.IsPaused = false;
                    }
                }
            }
        }


        private void _GestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (var gestureFrame = e.FrameReference.AcquireFrame())
            {
                if(gestureFrame != null && gestureFrame.DiscreteGestureResults != null)
                {
                    var result = gestureFrame.DiscreteGestureResults[_Jump];

                    if(result.Detected)
                    {
                        // Jumpした

                    }
                }
            }
        }

    }
}
