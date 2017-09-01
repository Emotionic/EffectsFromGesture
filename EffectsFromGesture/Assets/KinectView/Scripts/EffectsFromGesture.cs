using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Effekseer;

using Kinect = Windows.Kinect;


namespace Assets.KinectView.Scripts
{
    public class EffectsFromGesture : MonoBehaviour
    {
        /// <summary>
        /// TrailRendererを表示するためのマテリアル
        /// </summary>
        public Material TrailMaterial;

        public GameObject GestureManager;

        public GameObject BodySourceManager;

        /// <summary>
        /// エフェクト名
        /// </summary>
        private readonly string[] _EffectNames = { "StairBroken", "punch" };

        // 仮 HSVのH
        private float H = 0f;

        private GestureManager _GestureManager;

        private bool _IsRegMethod = false;

        /// <summary>
        /// Kinect画像と取得した関節情報を表示する
        /// </summary>
        private ColorBodySourceView _ColorBodyView;

        // Use this for initialization
        void Start()
        {
            // loadEffect
            foreach (var efkName in _EffectNames)
                EffekseerSystem.LoadEffect(efkName);
            
        }

        // Update is called once per frame
        void Update()
        {
            _GestureManager = GestureManager.GetComponent<GestureManager>();
            _ColorBodyView = BodySourceManager.GetComponent<ColorBodySourceView>();

            if (_GestureManager == null || _ColorBodyView == null)
                return;

            if(!_IsRegMethod)
            {
                Debug.Log("REG");
                _GestureManager.GestureDetected += _GestureManager_GestureDetected;
                _IsRegMethod = true;
            }

            foreach (GameObject body in _ColorBodyView.GetBodies())
            {
                AddingTrailRendererToBody(body);
            }
        }

        private void _GestureManager_GestureDetected(KeyValuePair<Gesture, DiscreteGestureResult> result, ulong id)
        {
            Debug.Log("REC EVNET : " + result.Key.Name + " : " + id);
            switch (result.Key.Name)
            {
                case "Jump02":

                    if (result.Value.Confidence < 0.6)
                        return;

                    Debug.Log("Jump02 Confidence : " + result.Value.Confidence);

                    // Jumpした
                    Vector3 pos =
                        _ColorBodyView.GetBody(id).transform.Find(JointType.SpineMid.ToString()).transform.position;

                    EffekseerSystem.PlayEffect(_EffectNames[0], pos);

                    break;
                case "OpenMenu":

                    if (result.Value.Confidence < 0.5)
                        return;

                    Debug.Log("OpenMenu Confidence : " + result.Value.Confidence);
                    break;

                case "Punch_Left":
                    Debug.Log("Punch Left" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        return;

                    EffekseerSystem.PlayEffect(_EffectNames[1], _ColorBodyView.GetBody(id).transform.Find(JointType.HandRight.ToString()).transform.position);
                    break;

                case "Punch_Right":
                    Debug.Log("Punch Right" + result.Value.Confidence);

                    if (result.Value.Confidence < 0.2)
                        return;

                    EffekseerSystem.PlayEffect(_EffectNames[1], _ColorBodyView.GetBody(id).transform.Find(JointType.HandLeft.ToString()).transform.position);
                    break;
            }
        }
        
        /// <summary>
        /// 両手足にTrailRendererを付ける
        /// </summary>
        /// <param name="body">エフェクトを付けるBody</param>
        private void AddingTrailRendererToBody(GameObject body)
        {
            GameObject handTipLeft = body.transform.Find(JointType.HandTipRight.ToString()).gameObject;
            GameObject handTipRight = body.transform.Find(JointType.HandTipLeft.ToString()).gameObject;

            GameObject thumbLeft = body.transform.Find(JointType.FootRight.ToString()).gameObject;
            GameObject thumbRight = body.transform.Find(JointType.FootLeft.ToString()).gameObject;

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
        
    }
}