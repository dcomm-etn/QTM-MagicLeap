# Qualisys Tracking Manager / Magic Leap Integration

This project implements the [Qualisys Realtime SDK for .NET](https://github.com/qualisys/RTClientSDK.Net) on the [Magic Leap](https://www.magicleap.com/) headset, enabling augmented reality experiences driven by motion capture data.

## Requirements

- Unity version: [2018.1.9f2-MLTP10](https://unity3d.com/partners/magicleap)
- Magic Leap's Lumin SDK v0.19 (download from [Magic Leap Creator Portal](https://creator.magicleap.com))

## Setup

[Unity Hub](https://docs.unity3d.com/Manual/GettingStartedUnityHub.html) is recommended.

After cloning and opening the Unity project:

- Import the Magic Leap Unity Package. 
- In *Build Settings > Lumin OS*, make sure that the *Lumin SDK Location* is pointing to the correct directory, and that Lumin OS is set as the build platform.
- In *Publishing Settings*, make sure that *ML Certificate* is set to point to your Magic Leap certificate.
- Set up the *Connector_Controller* script attached to the *Control and Connection* GameObject:
    - *IP Address* should point to the computer from which QTM is streaming data.
    - *Print...* and *Render...* options allow for various ways of displaying the incoming data. (Printing to console is slow, printing a high-frequency data stream will congestion.)
    - *Controller Hand* may need to be changed depending on how your device is set up.
    - *Rotation Speed* and *Translation Speed* affect responses when moving the rendering around using the controller.

A release where the QTM IP address and other settings can be configured interactively within the application is in the works.
