using QTMRealTimeSDK;
using QTMRealTimeSDK.Data;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class Connector_Controller : MonoBehaviour
{
    public String ipAddress;
    public int streamFrequency = 30;
    public bool print2DToConsole;
    public bool print3DToConsole;
    public bool render3D = true;
    public bool render6D;
    public GameObject mocapGameObject;
    public MLInput.Hand controllerHand = MLInput.Hand.Left;
    public float rotationSpeed = 30.0f;
    public float translationSpeed = 1f;

    private MLInputController _controller;
    private bool bumper = false;
    private RTProtocol rtProtocol = null;
    private GameObject[] balls;
    private GameObject[] cubes;

    void Awake()
    {
        MLInput.Start();
        MLInput.OnControllerButtonDown += OnButtonDown;
        MLInput.OnControllerButtonUp += OnButtonUp;
        _controller = MLInput.GetController(controllerHand);
        Debug.Log("Controller connected: " + _controller.ToString());
    }

    void OnDestroy()
    {
        MLInput.OnControllerButtonDown -= OnButtonDown;
        MLInput.OnControllerButtonUp -= OnButtonUp;
        MLInput.Stop();
    }

    void OnButtonDown(byte controller_id, MLInputControllerButton button)
    {
        if ((button == MLInputControllerButton.HomeTap))
        {
            StartStreaming(ipAddress);
        }
        if ((button == MLInputControllerButton.Bumper))
        {
            bumper = true;
        }
    }

    void OnButtonUp(byte controller_id, MLInputControllerButton button)
    {
        if ((button == MLInputControllerButton.Bumper))
        {
            bumper = false;
        }
    }

    void Update()
    {
        // Rotate
        if (bumper)
        {
            Debug.Log("Rotating holograms...");
            mocapGameObject.transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        // Translate
        else if (_controller.Touch1PosAndForce.z > 0.1f)
        {
            Debug.Log("Translating holograms...");
            float X = _controller.Touch1PosAndForce.x;
            float Y = _controller.Touch1PosAndForce.y;
            if (_controller.TriggerValue > 0.2f)
            {
                Vector3 up = Vector3.Normalize(Vector3.ProjectOnPlane(transform.up, Vector3.forward));
                Vector3 force = Vector3.Normalize(Y * up);
                mocapGameObject.transform.position += force * Time.deltaTime * translationSpeed;
            }
            else
            {
                Vector3 forward = Vector3.Normalize(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
                Vector3 right = Vector3.Normalize(Vector3.ProjectOnPlane(transform.right, Vector3.up));
                Vector3 force = Vector3.Normalize((X * right) + (Y * forward));
                mocapGameObject.transform.position += force * Time.deltaTime * translationSpeed;
            }
        }
        // Do QTM stuff
        if (rtProtocol != null && rtProtocol.IsConnected())
        {
            // Get RTPacket from stream
            PacketType packetType;
            rtProtocol.ReceiveRTPacket(out packetType, false);

            // Handle data packet
            if (packetType == PacketType.PacketData)
            {
                // Get 2D
                var twoDData = rtProtocol.GetRTPacket().Get2DMarkerData();
                // Print 2D
                if (twoDData != null && twoDData.Count > 0)
                {
                    var twoDForCamera0 = twoDData[0];
                    Debug.LogFormat("Frame:{0:D5} Markers:{1} Status:{2}",
                                        rtProtocol.GetRTPacket().Frame,
                                        twoDForCamera0.MarkerCount,
                                        twoDForCamera0.StatusFlags);
                }
                // Get 3D
                var threeDData = rtProtocol.GetRTPacket().Get3DMarkerResidualData();
                // Print 3D
                if (print3DToConsole)
                {
                    if (threeDData != null && threeDData.Count > 0)
                    {
                        for (int i = 0; i < threeDData.Count; i++)
                        {
                            var m = threeDData[i];
                            if (!Double.IsNaN(m.Position.X))
                            {
                                Debug.LogFormat("Frame:{0:D5} Name:{1,16} X:{2,7:F1} Y:{3,7:F1} Z:{4,7:F1} Residual:{5,5:F1}",
                                        rtProtocol.GetRTPacket().Frame, rtProtocol.Settings3D.Labels[i].Name,
                                        m.Position.X, m.Position.Y, m.Position.Z,
                                        m.Residual);
                            }
                            else
                            {
                                Debug.LogFormat("Frame:{0:D5} Name:{1,20} -----------------------------------",
                                        rtProtocol.GetRTPacket().Frame, rtProtocol.Settings3D.Labels[i].Name);
                            }
                        }
                    }
                }
                // Render 3D
                if (render3D)
                {
                    if (threeDData != null && threeDData.Count > 0)
                    {
                        for (int i = 0; i < threeDData.Count; i++)
                        {
                            var m = threeDData[i];
                            if (!Double.IsNaN(m.Position.X))
                            {
                                float scale = 0.001f;
                                Vector3 ballPosition = new Vector3(m.Position.X, m.Position.Z, m.Position.Y) * scale;
                                balls[i].transform.localPosition = ballPosition;
                            }
                        }
                    }
                }
                // Get 6D
                var sixDData = rtProtocol.GetRTPacket().Get6DOFResidualData();
                // Render 3D
                if (render6D)
                {
                    if (sixDData != null && sixDData.Count > 0)
                    {
                        for (int i = 0; i < sixDData.Count; i++)
                        {
                            var b = sixDData[i];
                            if (!Double.IsNaN(b.Position.X))
                            {
                                float scale = 0.001f;
                                Vector3 cubePosition = new Vector3(b.Position.X, b.Position.Z, b.Position.Y) * scale;
                                cubes[i].transform.localPosition = cubePosition;
                            }
                        }
                    }
                }
            }
        }
    }

    void StartStreaming(string ipAddress)
    {
        Debug.LogFormat("Starting stream from {0}...", ipAddress);

        // Init protocol
        rtProtocol = new RTProtocol();

        if (!rtProtocol.IsConnected())
        {
            // Check if connection to QTM is possible
            if (!rtProtocol.IsConnected())
            {
                if (!rtProtocol.Connect(ipAddress))
                {
                    Debug.Log("QTM: Trying to connect...");
                    return;
                }
                Debug.Log("QTM: Connected!");
            }

            // Get settings and start stream
            if (rtProtocol.GeneralSettings == null)
            {
                if (!rtProtocol.GetGeneralSettings())
                {
                    Debug.Log("QTM: Trying to get General settings...");
                    return;
                }
                Debug.Log("QTM: General settings available.");

                // Print camera settings to console
                Debug.LogFormat("QTM: Frequency: {0}", rtProtocol.GeneralSettings.CaptureFrequency);
                Debug.Log("QTM: Cameras:");
                foreach (var camera in rtProtocol.GeneralSettings.CameraSettings)
                {
                    Debug.LogFormat("\t{0}", camera.Model);
                }

                // Reset mocap gameobject
                foreach (Transform child in mocapGameObject.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }

                // Init components to stream 
                List<QTMRealTimeSDK.Data.ComponentType> componentsToStream = new List<QTMRealTimeSDK.Data.ComponentType>();

                // Start 2D data stream and print to console
                if (print2DToConsole)
                {
                    Debug.Log("QTM: Starting to stream 2D data, printing to console.");
                    componentsToStream.Add(QTMRealTimeSDK.Data.ComponentType.Component2d);
                }

                // Start 3D data stream...
                if (print3DToConsole || render3D)
                {
                    Debug.Log("QTM: Starting to stream 3D data.");
                    componentsToStream.Add(QTMRealTimeSDK.Data.ComponentType.Component3dResidual);
                    // ...and print to console...
                    if (print3DToConsole)
                    {
                        Debug.Log("QTM: 3D data stream will print to console.");
                    }
                    // ...and/or render as balls
                    if (render3D)
                    {
                        Debug.Log("QTM: 3D data stream will render in world.");
                        rtProtocol.Get3dSettings();
                        balls = new GameObject[rtProtocol.Settings3D.Labels.Count];
                        for (int i = 0; i < balls.Length; i++)
                        {
                            balls[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            balls[i].transform.parent = mocapGameObject.transform;
                            balls[i].transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                        }
                    }
                }

                // Start 6D data stream and render
                if (render6D)
                {
                    rtProtocol.Get6dSettings();
                    List<QTMRealTimeSDK.Settings.Settings6DOF> qtmBodies = rtProtocol.Settings6DOF.Bodies;
                    foreach (QTMRealTimeSDK.Settings.Settings6DOF body in qtmBodies)
                    {
                        Debug.LogFormat("QTM: Found 6DOF body: {0}", body.Name);
                    }
                    cubes = new GameObject[qtmBodies.Count];
                    for (int i = 0; i < cubes.Length; i++)
                    {
                        cubes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cubes[i].transform.parent = mocapGameObject.transform;
                        cubes[i].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    }
                    Debug.LogFormat("QTM: Starting to stream 6D data, rendering in world.");
                    componentsToStream.Add(QTMRealTimeSDK.Data.ComponentType.Component6dResidual);
                }
                rtProtocol.StreamFrames(StreamRate.RateFrequency, streamFrequency, componentsToStream);
            }
        }
    }
}