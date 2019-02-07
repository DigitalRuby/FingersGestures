Fingers Lite, by Jeff Johnson
Fingers Lite (c) 2018 Digital Ruby, LLC
http://www.digitalruby.com/Unity-Plugins

Version 2.6.0.

See ChangeLog.txt for history. Source code available on github: https://github.com/DigitalRuby/FingersGestures.

Upgrade to Fingers full version today at https://www.assetstore.unity3d.com/en/#!/content/41076?aid=1011lGnL

License: Source code may be used in personal and professional projects. Source code may NOT be redistributed or sold. See License.txt for full details.

--------------------
Fingers Lite is a free version of Fingers - Touch Gestures for Unity.

Upgrade to the full Fingers Gestures today and get:
- Dozens of component scripts + prefabs, including:
- Camera orbit
- Camera pan
- Camera rotate
- Drag & drop
- Platformer move, jump, drop down below platform
- DPad
- Joystick
- UIScrollView in Unity using pure C#
- Image and shape recogniztion
- Tons of demo scenes
- Email support, even Skype if things get really bad :)
- Show touch circles
- + MORE!

Upgrade to Fingers full version today at https://www.assetstore.unity3d.com/en/#!/content/41076?aid=1011lGnL
--------------------

Introduction
--------------------
Fingers is an advanced gesture recognizer system for Unity. All your favorite gestures (tap, double tap, pinch, rotate, etc.) are available! Building your own gestures is a snap.

If you've used UIGestureRecognizer on iOS, you should feel right at home using Fingers. In fact, Apple's documentation is very relevant to fingers: https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIGestureRecognizer_Class/

Please watch this video for a complete overview: https://youtu.be/97tJz0y52Fw

Instructions
--------------------
To get started, perform the following:
- Drag FingersScript from project view into an object in the hierarchy.
- Create and use some gestures!
- See the DemoScene (DemoScript.cs) for an example of all the gestures.

Fingers script has these properties:
- Treat mouse as pointer (default is true, useful for testing in the player for some gestures). Disable this if you are using Unity Remote or are running on a touch screen like Surface Pro.
- Simulate mouse with touch - whether to send mouse events for touches. Default is false. You don't need this unless you have legacy code relying on mouse events.
- RequireControlKeyForMouseZoom - whether the control key is required to scale with mouse wheel.
- Pass through objects. Any object in this list will always allow the gesture to execute and will not block it.
- Default DPI. In the event that Unity can't figure out the DPI of the device, use this default value.
- Clear gestures on level load. Default is true. This clears out all gestures when a new scene is loaded.

Event System
--------------------
The gestures can work with the Unity event system. Gestures over certain UI elements in a Canvas will be blocked, such as Button, Dropdown, etc. Text elements are always ignored and never block the gesture.

You can add physics raycasters to allow objects not on the Unity UI to also be part of the gesture pass through system. This requires an event system. Collider and Collider2D components will not block gestures unless the PlatformSpecificView property on the gesture is not null and does not match the game object with the collider.

Any object in the pass through list of FingersScript will always pass the gesture through and allow it to execute.

Options for allowing gestures on UI elements:
- You can set the PlatformSpecificView on your gesture that is the game object that you want to allow gestures on. If the gesture then executes over this game object, the gesture is always allowed. See DemoScriptPlatformSpecificView.cs.
- You can populate the PassThroughObjects property of FingersScript. Any game object in this list will always pass the gesture through.
- You can use the CaptureGestureHandler callback on the fingers script to run custom logic to determine whether the gesture can pass through a UI element. See DemoScript.cs, CaptureGestureHandler function.
- You can use the ComponentTypesToDenyPassThrough and ComponentTypesToIgnorePassThrough properties of FingersScript to customize pass through behavior by adding additional component types.

Fingers touch gestures supports canvas elements that are in screen space overlay mode along with 2D and 3D colliders.

See the DemoScript.cs file for more details and examples.

Standard Gestures:
--------------------
Once you've added the script, you will need to add some gestures. You can do this through the component menu in the editor or your own custom script. Remember to add "using DigitalRubyShared;" to your scripts.

Each gesture has public properties that can configure things such as thresholds for movement, rotation, etc. The defaults should work well for most cases. Fingers works in inches by default since most devices use DPI.

Use control + mouse wheel to scale, control + shift to rotate.

Please review the Start method of DemoScript.cs to see how gestures are created and added to the finger script. Also watch the tutorial video (link at top of this file) if you get lost, it will be very helpful.

Troubleshooting / FAQ:
--------------------
Q: My gestures aren't working.
A: Did you add a physics and/or physics2d ray caster to your camera? What about pass through objects, have you set those up properly?

Q: Simultaneous gestures are not working.
A: Ensure you call the allow simultaneous method on one of the gestures. Also consider trying ClearTrackedTouchesOnEndOrFail = true.

Q: My gesture is always failing.
A: Most likely you need to set the platform specific view on the gesture. You can set this to the game object of the canvas, or another game object with the UI element or game object collider you want the gesture to execute on.

Thank you.

Upgrade to Fingers full version today at https://www.assetstore.unity3d.com/en/#!/content/41076?aid=1011lGnL

- Jeff Johnson, creator of Fingers - Gestures for Unity
http://www.digitalruby.com/Unity-Plugins

