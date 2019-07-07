//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// 


using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DigitalRubyShared
{
    public class FingersScript : MonoBehaviour
    {
        [Tooltip("True to treat the mouse as a finger, false otherwise. Left, middle and right mouse buttons can be used as individual fingers and will all have the same location.")]
        public bool TreatMousePointerAsFinger = true;

        [Tooltip("Whether to treat touches as mouse pointer? This needs to be set before the script Awake method is called.")]
        public bool SimulateMouseWithTouches;

        [Tooltip("Whether the control key is required for mouse zoom. If true, control pluse mouse wheel zooms. If false, mouse wheel alone will zoom.")]
        public bool RequireControlKeyForMouseZoom = true;

        [Tooltip("Objects that should pass gestures through. By default, some UI components block gestures, such as Panel, Button, Dropdown, etc. See the SetupDefaultPassThroughComponents method for " +
            "the full list of defaults.")]
        public List<GameObject> PassThroughObjects;


        [Tooltip("The default DPI to use if the DPI cannot be determined")]
        public int DefaultDPI = 200;

        [Tooltip("Allows resetting state (keeps the gestures, just resets them) or clearing all gestures when a level is unloaded.")]
        public GestureLevelUnloadOption LevelUnloadOption = GestureLevelUnloadOption.ClearAllGestures;

        public enum GestureLevelUnloadOption
        {
            Nothing,
            ResetGestureState,
            ClearAllGestures
        }

        private const int mousePointerId1 = int.MaxValue - 2;
        private const int mousePointerId2 = int.MaxValue - 3;
        private const int mousePointerId3 = int.MaxValue - 4;

        private readonly List<GestureRecognizer> gestures = new List<GestureRecognizer>();
        private readonly List<GestureRecognizer> gesturesTemp = new List<GestureRecognizer>();
        private readonly List<GestureTouch> touchesBegan = new List<GestureTouch>();
        private readonly List<GestureTouch> touchesMoved = new List<GestureTouch>();
        private readonly List<GestureTouch> touchesEnded = new List<GestureTouch>();
        private readonly Dictionary<int, List<GameObject>> gameObjectsForTouch = new Dictionary<int, List<GameObject>>();
        private readonly List<RaycastResult> captureRaycastResults = new List<RaycastResult>();
        private readonly List<GestureTouch> filteredTouches = new List<GestureTouch>();
        private readonly List<GestureTouch> touches = new List<GestureTouch>();
        private readonly Dictionary<int, Vector2> previousTouchPositions = new Dictionary<int, Vector2>();
        private readonly List<Component> components = new List<Component>();
        private readonly HashSet<System.Type> componentTypesToDenyPassThrough = new HashSet<System.Type>();
        private readonly HashSet<System.Type> componentTypesToIgnorePassThrough = new HashSet<System.Type>();
        private readonly Collider2D[] hackResults = new Collider2D[128];
        private readonly List<GestureTouch> previousTouches = new List<GestureTouch>();
        private readonly List<GestureTouch> currentTouches = new List<GestureTouch>();
        private readonly HashSet<GestureTouch> tempTouches = new HashSet<GestureTouch>();

        private float rotateAngle;
        private float pinchScale = 1.0f;
        private GestureTouch rotatePinch1;
        private GestureTouch rotatePinch2;
        private System.DateTime lastMouseWheelTime;

        private static FingersScript singleton;

        private enum CaptureResult
        {
            /// <summary>
            /// Force the gesture to pass through
            /// </summary>
            ForcePassThrough,

            /// <summary>
            /// Force the gesture to be denied unless the platform specific view matches
            /// </summary>
            ForceDenyPassThrough,

            /// <summary>
            /// Do not force or deny the pass through
            /// </summary>
            Default,

            /// <summary>
            /// Pretend this object doesn't exist
            /// </summary>
            Ignore
        }

        private IEnumerator MainThreadCallback(float delay, System.Action action)
        {
            if (action != null)
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                yield return null;
                while ((float)timer.Elapsed.TotalSeconds < delay)
                {
                    yield return null;
                }
                action();
            }
        }

        private CaptureResult ShouldCaptureGesture(GameObject obj)
        {
            if (obj == null)
            {
                return CaptureResult.Default;
            }

            // if we have a capture gesture handler, perform a check to see if the user has custom pass through logic
            else if (CaptureGestureHandler != null)
            {
                bool? tmp = CaptureGestureHandler(obj);
                if (tmp != null)
                {
                    // user has decided on pass through, stop the loop
                    return (tmp.Value ? CaptureResult.ForceDenyPassThrough : CaptureResult.ForcePassThrough);
                }
            }

            // check pass through objects, these always pass the gesture through
            if (PassThroughObjects.Contains(obj))
            {
                // allow the gesture to pass through, do not capture it and stop the loop
                return CaptureResult.ForcePassThrough;
            }
            else
            {
                // if any gesture has a platform specific view that matches the object, use default behavior
                foreach (GestureRecognizer gesture in gestures)
                {
                    if (object.ReferenceEquals(gesture.PlatformSpecificView, obj))
                    {
                        return CaptureResult.Default;
                    }
                }
            }

            obj.GetComponents<Component>(components);

            try
            {
                System.Type type;
                foreach (Component c in components)
                {
                    type = c.GetType();
                    if (componentTypesToDenyPassThrough.Contains(type))
                    {
                        return CaptureResult.ForceDenyPassThrough;
                    }
                    else if (componentTypesToIgnorePassThrough.Contains(type))
                    {
                        return CaptureResult.Ignore;
                    }
                }
            }
            finally
            {
                components.Clear();
            }

            // default is for input UI elements (elements that normally block touches) to not pass through
            return CaptureResult.Default;
        }

        private void PopulateGameObjectsForTouch(int pointerId, float x, float y)
        {
            // Find a game object for a touch id
            if (EventSystem.current == null)
            {
                return;
            }

            List<GameObject> list;
            if (gameObjectsForTouch.TryGetValue(pointerId, out list))
            {
                list.Clear();
            }
            else
            {
                list = new List<GameObject>();
                gameObjectsForTouch[pointerId] = list;
            }

            captureRaycastResults.Clear();
            PointerEventData p = new PointerEventData(EventSystem.current);
            p.Reset();
            p.position = new Vector2(x, y);
            p.clickCount = 1;
            EventSystem.current.RaycastAll(p, captureRaycastResults);

            // HACK: Unity doesn't get collider2d on UI element, get those now
            int hackCount = Physics2D.OverlapPointNonAlloc(p.position, hackResults);
            for (int i = 0; i < hackCount; i++)
            {
                RaycastResult result = new RaycastResult { gameObject = hackResults[i].gameObject };
                if (captureRaycastResults.FindIndex((cmp) =>
                {
                    return cmp.gameObject == result.gameObject;
                }) < 0)
                {
                    captureRaycastResults.Add(result);
                }
            }
            System.Array.Clear(hackResults, 0, hackCount);

            if (captureRaycastResults.Count == 0)
            {
                captureRaycastResults.Add(new RaycastResult());
            }

            // determine what game object, if any should capture the gesture
            foreach (RaycastResult r in captureRaycastResults)
            {
                switch (ShouldCaptureGesture(r.gameObject))
                {
                    case CaptureResult.ForcePassThrough:
                        list.Clear();
                        return;

                    case CaptureResult.ForceDenyPassThrough:
                        // unless a platform specific view matches, deny the gesture
                        list.Add(r.gameObject);
                        return;

                    case CaptureResult.Default:
                        list.Add(r.gameObject);
                        break;

                    default:
                        break;
                }
            }
        }

        private GestureTouch GestureTouchFromTouch(ref Touch t)
        {
            // convert Unity touch to Gesture touch
            Vector2 prev;
            if (!previousTouchPositions.TryGetValue(t.fingerId, out prev))
            {
                prev.x = t.position.x;
                prev.y = t.position.y;
            }
            TouchPhase phase;
            switch (t.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    phase = TouchPhase.Began;
                    break;

                case UnityEngine.TouchPhase.Canceled:
                    phase = TouchPhase.Cancelled;
                    break;

                case UnityEngine.TouchPhase.Ended:
                    phase = TouchPhase.Ended;
                    break;

                case UnityEngine.TouchPhase.Moved:
                    phase = TouchPhase.Moved;
                    break;

                case UnityEngine.TouchPhase.Stationary:
                    phase = TouchPhase.Stationary;
                    break;

                default:
                    phase = TouchPhase.Unknown;
                    break;
            }
            GestureTouch touch = new GestureTouch(t.fingerId, t.position.x, t.position.y, prev.x, prev.y, t.pressure, t, phase);
            prev.x = t.position.x;
            prev.y = t.position.y;
            previousTouchPositions[t.fingerId] = prev;
            return touch;
        }

        private void FingersBeginTouch(ref GestureTouch g)
        {
            if (!previousTouches.Contains(g))
            {
                previousTouches.Add(g);
            }
            touchesBegan.Add(g);
            previousTouchPositions[g.Id] = new Vector2(g.X, g.Y);
        }

        private void FingersContinueTouch(ref GestureTouch g)
        {
            touchesMoved.Add(g);
            previousTouchPositions[g.Id] = new Vector2(g.X, g.Y);
        }

        private void FingersEndTouch(ref GestureTouch g, bool lost = false)
        {
            if (!lost)
            {
                touchesEnded.Add(g);
            }
            previousTouchPositions.Remove(g.Id);
            previousTouches.Remove(g);
        }

        private void FingersProcessTouch(ref GestureTouch g)
        {
            currentTouches.Add(g);

            // do our own touch up / down tracking, the user can reset touch state so that touches can begin again without a finger being lifted
            if (g.TouchPhase == TouchPhase.Moved || g.TouchPhase == TouchPhase.Stationary)
            {
                FingersContinueTouch(ref g);
            }
            else if (g.TouchPhase == TouchPhase.Began)
            {
                FingersBeginTouch(ref g);
            }
            else
            {
                FingersEndTouch(ref g);
            }

            // string d = string.Format ("Touch: {0} {1}", t.position, t.phase);
            // Debug.Log (d);
        }

        private void AddMouseTouch(int index, int pointerId, float x, float y)
        {
            TouchPhase phase;
            if (Input.GetMouseButtonDown(index))
            {
                phase = TouchPhase.Began;
            }
            else if (Input.GetMouseButtonUp(index))
            {
                phase = TouchPhase.Ended;
            }
            else if (Input.GetMouseButton(index))
            {
                phase = TouchPhase.Moved;
            }
            else
            {
                return;
            }

            Vector2 prev;
            if (!previousTouchPositions.TryGetValue(pointerId, out prev))
            {
                prev.x = x;
                prev.y = y;
            }
            GestureTouch g = new GestureTouch(pointerId, x, y, prev.x, prev.y, 0.0f, index, phase);
            FingersProcessTouch(ref g);
            prev.x = x;
            prev.y = y;
            previousTouchPositions[pointerId] = prev;
        }

        private void ProcessTouches()
        {
            // process each touch in the Unity list of touches
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                GestureTouch g = GestureTouchFromTouch(ref t);
                FingersProcessTouch(ref g);
            }
        }

        private void RotateAroundPoint(ref float rotX, ref float rotY, float anchorX, float anchorY, float angleRadians)
        {
            // rotate around a point in 2D space
            float cosTheta = Mathf.Cos(angleRadians);
            float sinTheta = Mathf.Sin(angleRadians);
            float x = rotX - anchorX;
            float y = rotY - anchorY;
            rotX = ((cosTheta * x) - (sinTheta * y)) + anchorX;
            rotY = ((sinTheta * x) + (cosTheta * y)) + anchorY;
        }

        private void ProcessMouseButtons()
        {
            // if not using the mouse, bail
            if (!Input.mousePresent || !TreatMousePointerAsFinger)
            {
                return;
            }

            // add touches based on each mouse button
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            AddMouseTouch(0, mousePointerId1, x, y);
            AddMouseTouch(1, mousePointerId2, x, y);
            AddMouseTouch(2, mousePointerId3, x, y);
        }

        private void ProcessMouseWheel()
        {
            // if the mouse is not setup or the user doesn't want the mouse treated as touches, return right away
            if (!Input.mousePresent || !TreatMousePointerAsFinger)
            {
                return;
            }

            // the mouse wheel will act as a rotate and pinch / zoom
            const float threshold = 50.0f;
            const float deltaModifier = 0.025f;
            Vector2 delta = Input.mouseScrollDelta;
            float scrollDelta = (delta.y == 0.0f ? delta.x : delta.y) * deltaModifier;

            // add type 1 = moved, 2 = begin, 3 = ended, 4 = none
            int addType1 = 4;
            int addType2 = 4;

            // left or right control initial down means begin
            if (!RequireControlKeyForMouseZoom)
            {
                if (delta == Vector2.zero)
                {
                    if (lastMouseWheelTime != System.DateTime.MinValue)
                    {
                        if ((System.DateTime.UtcNow - lastMouseWheelTime).TotalSeconds < 1.0f)
                        {
                            // continue zooming
                            pinchScale = Mathf.Max(0.35f, pinchScale + scrollDelta);
                            addType1 = 1;
                        }
                        else
                        {
                            // stop zooming
                            lastMouseWheelTime = System.DateTime.MinValue;
                            addType1 = 3;
                        }
                    }
                }
                else if (lastMouseWheelTime == System.DateTime.MinValue)
                {
                    // start zooming
                    addType1 = 2;
                    lastMouseWheelTime = System.DateTime.UtcNow;
                }
                else
                {
                    // continue zooming
                    pinchScale = Mathf.Max(0.35f, pinchScale + scrollDelta);
                    addType1 = 1;
                    lastMouseWheelTime = System.DateTime.UtcNow;
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                // initial start of scale
                addType1 = 2;
            }
            // left or right control still down means move
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                pinchScale = Mathf.Max(0.35f, pinchScale + scrollDelta);
                addType1 = 1;
            }
            // left or right control initial up means end
            else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            {
                addType1 = 3;
            }

            // left or right shift initial down means begin
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                addType2 = 2;
            }
            // left or right shift still down means move
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                rotateAngle += scrollDelta;
                addType2 = 1;
            }
            // left or right shift initial up means end
            else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
            {
                addType2 = 3;
            }

            // use the minimum add type so that moves are preferred over begins and begins are preferred over ends
            int addType = Mathf.Min(addType1, addType2);

            // no begins, moves or ends, set defaults and end
            if (addType == 4)
            {
                pinchScale = 1.0f;
                rotateAngle = 0.0f;
                return;
            }

            // calculate rotation
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            float xRot1 = x - threshold;
            float yRot1 = y;
            float xRot2 = x + threshold;
            float yRot2 = y;
            float distance = threshold * pinchScale;
            xRot1 = x - distance;
            yRot1 = y;
            xRot2 = x + distance;
            yRot2 = y;
            RotateAroundPoint(ref xRot1, ref yRot1, x, y, rotateAngle);
            RotateAroundPoint(ref xRot2, ref yRot2, x, y, rotateAngle);

#if DEBUG

            if (scrollDelta != 0.0f)
            {
                //Debug.LogFormat("Mouse delta: {0}", scrollDelta);
            }

#endif

            // calculate rotation and zoom based on mouse values
            if (addType == 1)
            {
                // moved
                rotatePinch1 = new GestureTouch(int.MaxValue - 5, xRot1, yRot1, rotatePinch1.X, rotatePinch1.Y, 0.0f, null, TouchPhase.Moved);
                rotatePinch2 = new GestureTouch(int.MaxValue - 6, xRot2, yRot2, rotatePinch2.X, rotatePinch2.Y, 0.0f, null, TouchPhase.Moved);
                FingersProcessTouch(ref rotatePinch1);
                FingersProcessTouch(ref rotatePinch2);
            }
            else if (addType == 2)
            {
                // begin
                rotatePinch1 = new GestureTouch(int.MaxValue - 5, xRot1, yRot1, xRot1, yRot1, 0.0f, null, TouchPhase.Began);
                rotatePinch2 = new GestureTouch(int.MaxValue - 6, xRot2, yRot2, xRot2, yRot2, 0.0f, null, TouchPhase.Began);
                FingersProcessTouch(ref rotatePinch1);
                FingersProcessTouch(ref rotatePinch2);
            }
            else if (addType == 3)
            {
                // end
                rotatePinch1 = new GestureTouch(int.MaxValue - 5, xRot1, yRot1, xRot1, yRot1, 0.0f, null, TouchPhase.Ended);
                rotatePinch2 = new GestureTouch(int.MaxValue - 6, xRot2, yRot2, xRot2, yRot2, 0.0f, null, TouchPhase.Ended);
                FingersProcessTouch(ref rotatePinch1);
                FingersProcessTouch(ref rotatePinch2);
            }
        }

        private void ProcessLostTouches()
        {
            // handle lost touches due to Unity bugs, Unity can not send touch end states
            //  and it appears that even the id's of touches can change in WebGL
            foreach (GestureTouch t in previousTouches)
            {
                if (!currentTouches.Contains(t))
                {
                    tempTouches.Add(t);
                }
            }
            foreach (GestureRecognizer g in gestures)
            {
                bool reset = false;
                foreach (GestureTouch t in g.CurrentTrackedTouches)
                {
                    if (!currentTouches.Contains(t))
                    {
                        tempTouches.Add(t);
                        reset = true;
                    }
                }
                if (reset)
                {
                    g.Reset();
                }
            }
            foreach (GestureTouch t in tempTouches)
            {
                // only end touch here, as end touch removes from previousTouches list
                GestureTouch tmp = t;
                FingersEndTouch(ref tmp, true);
                previousTouches.Remove(tmp);
            }

            tempTouches.Clear();
        }

        private bool GameObjectMatchesPlatformSpecificView(List<GameObject> list, GestureRecognizer r)
        {
            GameObject platformSpecificView = r.PlatformSpecificView as GameObject;

            if ((platformSpecificView == null && EventSystem.current == null) ||
                // HACK: If the platform specific view is a Canvas, always match
                (platformSpecificView != null && platformSpecificView.GetComponent<Canvas>() != null))
            {
                return true;
            }
            else if (list.Count == 0)
            {
                return (platformSpecificView == null);
            }
            foreach (GameObject obj in list)
            {
                if (obj == platformSpecificView)
                {
                    return true;
                }
                else
                {
                    // if we have a collider and no platform specific view, count as a match
                    bool hasCollider = (obj != null && (obj.GetComponent<Collider2D>() != null || obj.GetComponent<Collider>() != null));
                    if (hasCollider && platformSpecificView == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ICollection<GestureTouch> FilterTouchesBegan(List<GestureTouch> touches, GestureRecognizer r)
        {
            // in order to begin, touches must match the platform specific view
            List<GameObject> gameObjects;
            filteredTouches.Clear();
            foreach (GestureTouch t in touches)
            {
                if (!gameObjectsForTouch.TryGetValue(t.Id, out gameObjects) || GameObjectMatchesPlatformSpecificView(gameObjects, r))
                {
                    filteredTouches.Add(t);
                }
            }
            return filteredTouches;
        }

        private void CleanupPassThroughObjects()
        {
            if (PassThroughObjects == null)
            {
                PassThroughObjects = new List<GameObject>();
            }
            for (int i = PassThroughObjects.Count - 1; i >= 0; i--)
            {
                if (PassThroughObjects[i] == null)
                {
                    PassThroughObjects.RemoveAt(i);
                }
            }
        }

        private void SetupDefaultPassThroughComponents()
        {
            componentTypesToDenyPassThrough.Add(typeof(Scrollbar));
            componentTypesToDenyPassThrough.Add(typeof(Button));
            componentTypesToDenyPassThrough.Add(typeof(Dropdown));
            componentTypesToDenyPassThrough.Add(typeof(Toggle));
            componentTypesToDenyPassThrough.Add(typeof(Slider));
            componentTypesToDenyPassThrough.Add(typeof(InputField));

            componentTypesToIgnorePassThrough.Add(typeof(Text));
        }

        private void SceneManagerSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            switch (LevelUnloadOption)
            {
                case GestureLevelUnloadOption.ResetGestureState:
                    ResetState(false);
                    break;

                case GestureLevelUnloadOption.ClearAllGestures:
                    ResetState(true);
                    break;
            }
        }

        private void Awake()
        {
            singleton = this;

            // setup DPI, using a default value if it cannot be determined
            DeviceInfo.PixelsPerInch = (int)Screen.dpi;
            if (DeviceInfo.PixelsPerInch > 0)
            {
                DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch;
            }
            else
            {
                // pick a sensible dpi since we don't know the actual DPI
                DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch = DefaultDPI;
            }

            // set the main thread callback so gestures can callback after a delay
            GestureRecognizer.MainThreadCallback = (float delay, System.Action callback) =>
            {
                StartCoroutine(MainThreadCallback(delay, callback));
            };

            ResetState(false);
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneManagerSceneUnloaded;
            if (!Input.multiTouchEnabled)
            {
                Input.multiTouchEnabled = true;
            }
            Input.simulateMouseWithTouches = SimulateMouseWithTouches;
            SetupDefaultPassThroughComponents();

#if UNITY_EDITOR

            if (EventSystem.current == null)
            {
                Debug.LogWarning("An event system is required if you want to use platform specific views for 2d and 3d collider game objects.");
            }

#endif

        }

        private void Update()
        {
            // cleanup pass through objects
            CleanupPassThroughObjects();

            // clear out all touches for each phase
            currentTouches.Clear();
            touchesBegan.Clear();
            touchesMoved.Clear();
            touchesEnded.Clear();

            // process touches and mouse
            ProcessTouches();
            ProcessMouseButtons();
            ProcessMouseWheel();
            ProcessLostTouches();

            // Debug.LogFormat("B: {0}, M: {1}, E: {2}", touchesBegan.Count, touchesMoved.Count, touchesEnded.Count);

            // keep track of game objects and touches
            foreach (GestureTouch t in touchesBegan)
            {
                PopulateGameObjectsForTouch(t.Id, t.X, t.Y);
            }

            // for each gesture, process the touches
            // copy to temp list in case gestures are added during the callbacks
            gesturesTemp.AddRange(gestures);
            foreach (GestureRecognizer gesture in gesturesTemp)
            {
                gesture.ProcessTouchesBegan(FilterTouchesBegan(touchesBegan, gesture));
                gesture.ProcessTouchesMoved(touchesMoved);
                gesture.ProcessTouchesEnded(touchesEnded);
            }
            gesturesTemp.Clear();

            // remove any game objects that are no longer being touched
            foreach (GestureTouch t in touchesEnded)
            {
                gameObjectsForTouch.Remove(t.Id);
            }

            // clear touches
            touches.Clear();

            // add all the touches
            touches.AddRange(touchesBegan);
            touches.AddRange(touchesMoved);
            touches.AddRange(touchesEnded);
        }

        private void OnDestroy()
        {
            if (singleton == this)
            {
                singleton = null;
            }
        }

        /// <summary>
        /// Add a gesture to the fingers script. This gesture will give callbacks when it changes state.
        /// </summary>
        /// <param name="gesture">Gesture to add</param>
        /// <return>True if the gesture was added, false if the gesture was already added</return>
        public bool AddGesture(GestureRecognizer gesture)
        {
            if (gesture == null || gestures.Contains(gesture))
            {
                return false;
            }
            gestures.Add(gesture);
            return true;
        }

        /// <summary>
        /// Remove a gesture from the script. The gesture will no longer give callbacks.
        /// </summary>
        /// <param name="gesture">Gesture to remove</param>
        /// <returns>True if the gesture was removed, false if it was not in the script</returns>
        public bool RemoveGesture(GestureRecognizer gesture)
        {
            if (gesture != null)
            {
                return gestures.Remove(gesture);
            }
            return false;
        }

        /// <summary>
        /// Reset state - all touches and tracking are cleared
        /// </summary>
        /// <param name="clearGestures">True to clear out all gestures, false otherwise</param>
        public void ResetState(bool clearGestures)
        {
            for (int i = gestures.Count - 1; i >= 0; i--)
            {
                if (gestures[i] == null)
                {
                    gestures.RemoveAt(i);
                }
                else
                {
                    gestures[i].Reset();
                }
            }
            if (clearGestures)
            {
                gestures.Clear();
            }
            currentTouches.Clear();
            previousTouches.Clear();
            touchesBegan.Clear();
            touchesMoved.Clear();
            touchesEnded.Clear();
            gameObjectsForTouch.Clear();
            captureRaycastResults.Clear();
            filteredTouches.Clear();
            touches.Clear();
            previousTouchPositions.Clear();
            rotateAngle = 0.0f;
            pinchScale = 1.0f;
            rotatePinch1 = new GestureTouch();
            rotatePinch2 = new GestureTouch();
            lastMouseWheelTime = System.DateTime.MinValue;

            if (PassThroughObjects != null)
            {
                // cleanup deleted pass through objects
                for (int i = PassThroughObjects.Count - 1; i >= 0; i--)
                {
                    if (PassThroughObjects[i] == null)
                    {
                        PassThroughObjects.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Convert rect transform to screen space
        /// </summary>
        /// <param name="transform">Rect transform</param>
        /// <returns>Screen space rect</returns>
        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            float x = transform.position.x - (size.x * 0.5f);
            float y = transform.position.y - (size.y * 0.5f);
            return new Rect(x, y, size.x, size.y);
        }

        /// <summary>
        /// Gets a collection of the current touches
        /// </summary>
        public ICollection<GestureTouch> Touches { get { return touches; } }

        /// <summary>
        /// Optional handler to determine whether a game object will pass through or not.
        /// Null handler gets default gesture capture handling.
        /// Non-null handler that returns null gets default handling.
        /// Non-null handler that returns true captures the gesture.
        /// Non-null handler that returns false passes the gesture through.
        /// </summary>
        public System.Func<GameObject, bool?> CaptureGestureHandler;

        /// <summary>
        /// A set of component types that will stop the gesture from passing through. By default includes UI components like Button, Dropdown, etc.
        /// You can add additional component types if you like, but you should not remove items from this set or clear the set.
        /// </summary>
        public HashSet<System.Type> ComponentTypesToDenyPassThrough { get { return componentTypesToDenyPassThrough; } }

        /// <summary>
        /// A set of component types that will be ignored for purposes of pass through checking. By default includes the Text UI component.
        /// You can add additional component types if you like, but you should not remove items from this set or clear the set.
        /// </summary>
        public HashSet<System.Type> ComponentTypesToIgnorePassThrough { get { return componentTypesToIgnorePassThrough; } }

        /// <summary>
        /// Current touch objects begin tracked
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<GestureTouch> CurrentTouches { get { return currentTouches.AsReadOnly(); } }

        /// <summary>
        /// All gestures added to the script
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<GestureRecognizer> Gestures { get { return gestures.AsReadOnly(); } }

        /// <summary>
        /// Shared static instance of fingers script that lives forever - the prefab MUST exist in a resources folder!
        /// Note that you can still add the prefab to your scene if you prefer to do it that way, in which case the
        /// singleton will be re-created each time a scene loads.
        /// </summary>
        public static FingersScript Instance
        {
            get { return singleton; }
        }

        /// <summary>
        /// Check whether Instance is not null without it actually creating a prefab if needed
        /// </summary>
        public static bool HasInstance { get { return singleton != null; } }
    }
}
