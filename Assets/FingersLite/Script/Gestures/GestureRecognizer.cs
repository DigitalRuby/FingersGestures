//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// 


using System;
using System.Collections.Generic;

namespace DigitalRubyShared
{
    /// <summary>
    /// Gesture recognizer states
    /// </summary>
    public enum GestureRecognizerState
    {
        /// <summary>
        /// Gesture is possible
        /// </summary>
        Possible = 1,

        /// <summary>
        /// Gesture has started
        /// </summary>
        Began = 2,

        /// <summary>
        /// Gesture is executing
        /// </summary>
        Executing = 4,

        /// <summary>
        /// Gesture has ended
        /// </summary>
        Ended = 8,

        /// <summary>
        /// End is pending, if the dependant gesture fails
        /// </summary>
        EndPending = 16,

        /// <summary>
        /// Gesture has failed
        /// </summary>
        Failed = 32
    }

    /// <summary>
    /// Touch phases
    /// </summary>
    public enum TouchPhase
    {
        /// <summary>
        /// Unknown phase
        /// </summary>
        Unknown,

        /// <summary>
        /// Touch began
        /// </summary>
        Began,

        /// <summary>
        /// Touch stationary
        /// </summary>
        Stationary,

        /// <summary>
        /// Touch moved
        /// </summary>
        Moved,

        /// <summary>
        /// Touch eded
        /// </summary>
        Ended,

        /// <summary>
        /// Touch cancel
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Contains a touch event
    /// </summary>
    public struct GestureTouch : IComparable<GestureTouch>
    {
        private readonly int id;
        private readonly float previousX;
        private readonly float previousY;
        private readonly float pressure;
        private readonly float screenX;
        private readonly float screenY;
        private readonly object platformSpecificTouch;
        private readonly TouchPhase touchPhase;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="platformSpecificId">A unique id for this touch (for virtual touches please use range -1000 to -2000</param>
        /// <param name="screenX">Screen x in pixels</param>
        /// <param name="screenY">Screen y in pixels</param>
        /// <param name="previousX">Previous screen x in pixels</param>
        /// <param name="previousY">Previous screen y in pixels</param>
        /// <param name="pressure">Pressure if known (0 to 1)</param>
        /// <param name="platformSpecificTouch"></param>
        /// <param name="touchPhase"></param>
        public GestureTouch(int platformSpecificId, float screenX, float screenY, float previousX, float previousY, float pressure, object platformSpecificTouch = null, TouchPhase touchPhase = TouchPhase.Unknown)
        {
            this.id = platformSpecificId;
            this.screenX = screenX;
            this.screenY = screenY;
            this.previousX = previousX;
            this.previousY = previousY;
            this.pressure = pressure;
            this.platformSpecificTouch = platformSpecificTouch;
            this.touchPhase = touchPhase;
        }

        /// <summary>
        /// Compare to another touch by id
        /// </summary>
        /// <param name="other">Other touch</param>
        /// <returns>CompareTo result</returns>
        public int CompareTo(GestureTouch other)
        {
            return this.id.CompareTo(other.id);
        }

        /// <summary>
        /// Returns a hash code for this GestureTouch
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return Id;
        }

        /// <summary>
        /// Checks if this GestureTouch equals another GestureTouch
        /// </summary>
        /// <param name="obj">The object to compare against</param>
        /// <returns>True if equal to obj, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is GestureTouch)
            {
                return ((GestureTouch)obj).Id == Id;
            }
            return false;
        }

        /// <summary>
        /// Unique id for the touch
        /// </summary>
        /// <value>The platform specific identifier</value>
        public int Id { get { return id; } }

        /// <summary>
        /// X value in screen pixels
        /// </summary>
        /// <value>The screen x value</value>
        public float ScreenX { get { return screenX; } }

        /// <summary>
        /// Same as ScreenX
        /// </summary>
        public float X { get { return ScreenX; } }

        /// <summary>
        /// Y value in screen pixels
        /// </summary>
        /// <value>The screen y value</value>
        public float ScreenY { get { return screenY; } }

        /// <summary>
        /// Same as ScreenY
        /// </summary>
        public float Y { get { return ScreenY; } }

        /// <summary>
        /// Previous screen x value in pixels
        /// </summary>
        /// <value>The previous x value</value>
        public float PreviousX { get { return previousX; } }

        /// <summary>
        /// Previous screen Y value in pixels
        /// </summary>
        /// <value>The previous y value</value>
        public float PreviousY { get { return previousY; } }

        /// <summary>
        /// Pressure, 0 if unknown (0 - 1)
        /// </summary>
        /// <value>The pressure of the touch</value>
        public float Pressure { get { return pressure; } }

        /// <summary>
        /// Change in x value
        /// </summary>
        /// <value>The delta x</value>
        public float DeltaX { get { return screenX - previousX; } }

        /// <summary>
        /// Change in y value
        /// </summary>
        /// <value>The delta y</value>
        public float DeltaY { get { return screenY - previousY; } }

        /// <summary>
        /// Platform specific touch information (null if none)
        /// </summary>
        public object PlatformSpecificTouch { get { return platformSpecificTouch; } }

        /// <summary>
        /// The touch phase
        /// </summary>
        public TouchPhase TouchPhase { get { return touchPhase; } }
    }

    /// <summary>
    /// Gesture recognizer state change event - gesture.CurrentTrackedTouches contains all of the current touches
    /// </summary>
    public delegate void GestureRecognizerStateUpdatedDelegate(DigitalRubyShared.GestureRecognizer gesture);

    /// <summary>
    /// Obsolete, use GestureRecognizerStateUpdatedDelegate
    /// </summary>
    /// <param name="gesture"></param>
    /// <param name="touches"></param>
    public delegate void GestureRecognizerUpdated(DigitalRubyShared.GestureRecognizer gesture, ICollection<GestureTouch> touches);

    /// <summary>
    /// Tracks and calculates velocity for gestures
    /// </summary>
    public class GestureVelocityTracker
    {
        private struct VelocityHistory
        {
            public float VelocityX;
            public float VelocityY;
            public float Seconds;
        }

        private const int maxHistory = 8;

        private readonly System.Collections.Generic.Queue<VelocityHistory> history = new System.Collections.Generic.Queue<VelocityHistory>();
        private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private float previousX;
        private float previousY;

        private void AddItem(float velocityX, float velocityY, float elapsed)
        {
            VelocityHistory item = new VelocityHistory
            {
                VelocityX = velocityX,
                VelocityY = velocityY,
                Seconds = elapsed
            };
            history.Enqueue(item);
            if (history.Count > maxHistory)
            {
                history.Dequeue();
            }
            float totalSeconds = 0.0f;
            VelocityX = VelocityY = 0.0f;
            foreach (VelocityHistory h in history)
            {
                totalSeconds += h.Seconds;
            }
            foreach (VelocityHistory h in history)
            {
                float weight = h.Seconds / totalSeconds;
                VelocityX += (h.VelocityX * weight);
                VelocityY += (h.VelocityY * weight);
            }
            timer.Reset();
            timer.Start();
        }

        public void Reset()
        {
            timer.Reset();
            VelocityX = VelocityY = 0.0f;
            history.Clear();
        }

        public void Restart()
        {
            Restart(float.MinValue, float.MinValue);
        }

        public void Restart(float previousX, float previousY)
        {
            this.previousX = previousX;
            this.previousY = previousY;
            Reset();
            timer.Start();
        }

        public void Update(float x, float y)
        {
            float elapsed = ElapsedSeconds;
            if (previousX != float.MinValue)
            {
                float px = previousX;
                float py = previousY;
                float velocityX = (x - px) / elapsed;
                float velocityY = (y - py) / elapsed;
                AddItem(velocityX, velocityY, elapsed);
            }
            previousX = x;
            previousY = y;
        }

        public float ElapsedSeconds { get { return (float)timer.Elapsed.TotalSeconds; } }
        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }
        public float Speed { get { return (float)Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY); } }
    }

    /// <summary>
    /// A gesture recognizer allows handling gestures as well as ensuring that different gestures
    /// do not execute at the same time. Platform specific code is required to create GestureTouch
    /// sets and pass them to the appropriate gesture recognizer(s). Creating extension methods
    /// on the DigitalRubyShared.GestureRecognizer class is a good way.
    /// </summary>
    public class GestureRecognizer : IDisposable
    {
        private static readonly DigitalRubyShared.GestureRecognizer allGesturesReference = new DigitalRubyShared.GestureRecognizer();
        private GestureRecognizerState state = GestureRecognizerState.Possible;
        private readonly List<GestureTouch> currentTrackedTouches = new List<GestureTouch>();
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<GestureTouch> currentTrackedTouchesReadOnly;
        private readonly HashSet<DigitalRubyShared.GestureRecognizer> requireGestureRecognizersToFail = new HashSet<DigitalRubyShared.GestureRecognizer>();
        private readonly HashSet<GestureRecognizer> requireGestureRecognizersToFailThatHaveFailed = new HashSet<GestureRecognizer>();
        private readonly HashSet<DigitalRubyShared.GestureRecognizer> failGestures = new HashSet<DigitalRubyShared.GestureRecognizer>();
        private readonly List<DigitalRubyShared.GestureRecognizer> simultaneousGestures = new List<DigitalRubyShared.GestureRecognizer>();
        private readonly GestureVelocityTracker velocityTracker = new GestureVelocityTracker();
        private readonly List<KeyValuePair<float, float>> touchStartLocations = new List<KeyValuePair<float, float>>();
        private readonly HashSet<int> ignoreTouchIds = new HashSet<int>();
        private readonly List<GestureTouch> tempTouches = new List<GestureTouch>();

        private int minimumNumberOfTouchesToTrack = 1;
        private int maximumNumberOfTouchesToTrack = 1;
        private bool justFailed;
        private bool justEnded;
        private bool isRestarting;
        private int lastTrackTouchCount;
        private bool enabled = true;

        protected float PrevFocusX { get; private set; }
        protected float PrevFocusY { get; private set; }

        internal static readonly HashSet<DigitalRubyShared.GestureRecognizer> ActiveGestures = new HashSet<DigitalRubyShared.GestureRecognizer>();

        private void UpdateTouchState(bool executing)
        {
            if (executing && lastTrackTouchCount != CurrentTrackedTouches.Count)
            {
                ReceivedAdditionalTouches = true;
                lastTrackTouchCount = CurrentTrackedTouches.Count;
            }
            else
            {
                ReceivedAdditionalTouches = false;
            }
        }

        private void EndGesture()
        {
            state = GestureRecognizerState.Ended;
            ReceivedAdditionalTouches = false;
            lastTrackTouchCount = 0;
            StateChanged();
            if (ResetOnEnd)
            {
                ResetInternal(ClearTrackedTouchesOnEndOrFail);
            }
            else
            {
                SetState(GestureRecognizerState.Possible);
                touchStartLocations.Clear();
                RemoveFromActiveGestures();
                requireGestureRecognizersToFailThatHaveFailed.Clear();
            }

            // if this gesture is a fail gesture for another gesture, reset that gesture as this gesture has ended and not failed
            foreach (DigitalRubyShared.GestureRecognizer gesture in failGestures)
            {
                gesture.FailGestureNow();
            }
        }

        private void RemoveFromActiveGestures()
        {
            ActiveGestures.Remove(this);
        }

        private bool CanExecuteGestureWithOtherGesturesOrFail(GestureRecognizerState value)
        {
            // if we are trying to execute from a non-executing state and there are gestures already executing,
            // we need to make sure we are allowed to execute simultaneously
            if (ActiveGestures.Count != 0 &&
            (
                value == GestureRecognizerState.Began ||
                value == GestureRecognizerState.Executing ||
                value == GestureRecognizerState.Ended
            ) && state != GestureRecognizerState.Began && state != GestureRecognizerState.Executing)
            {
                // check all the active gestures and if any are not allowed to simultaneously
                // execute with this gesture, fail this gesture immediately
                foreach (DigitalRubyShared.GestureRecognizer gesture in ActiveGestures)
                {
                    if (gesture != this &&
                        (!AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent || gesture.PlatformSpecificView == PlatformSpecificView) &&
                        !simultaneousGestures.Contains(gesture) &&
                        !gesture.simultaneousGestures.Contains(this) &&
                        !simultaneousGestures.Contains(allGesturesReference) &&
                        !gesture.simultaneousGestures.Contains(allGesturesReference))
                    {
                        FailGestureNow();
                        return false;
                    }
                }
            }
            return true;
        }

        private void FailGestureNow()
        {
            state = GestureRecognizerState.Failed;
            RemoveFromActiveGestures();
            StateChanged();
            foreach (DigitalRubyShared.GestureRecognizer gesture in failGestures)
            {
                gesture.requireGestureRecognizersToFailThatHaveFailed.Add(this);
                if (gesture.state == GestureRecognizerState.EndPending)
                {
                    if (gesture.HasAllRequiredFailGesturesToEndFromEndPending())
                    {
                        gesture.SetState(GestureRecognizerState.Ended);
                    }
                }
            }
            ResetInternal(ClearTrackedTouchesOnEndOrFail);
            justFailed = true;
            lastTrackTouchCount = 0;
            ReceivedAdditionalTouches = false;
        }

        private bool TouchesIntersect(IEnumerable<GestureTouch> collection, List<GestureTouch> list)
        {
            foreach (GestureTouch t in collection)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == t.Id)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateTrackedTouches(IEnumerable<GestureTouch> touches)
        {
            int count = 0;
            foreach (GestureTouch touch in touches)
            {
                for (int i = 0; i < currentTrackedTouches.Count; i++)
                {
                    if (currentTrackedTouches[i].Id == touch.Id)
                    {
                        currentTrackedTouches[i] = touch;
                        count++;
                        break;
                    }
                }
            }
            if (count != 0)
            {
                currentTrackedTouches.Sort();
            }
        }

        private int TrackTouchesInternal(IEnumerable<GestureTouch> touches)
        {
            int count = 0;
            foreach (GestureTouch touch in touches)
            {
                // always track all touches in possible state, allows failing gesture if too many touches
                // do not track higher than the max touch count if in another state
                if ((State == GestureRecognizerState.Possible || currentTrackedTouches.Count < MaximumNumberOfTouchesToTrack) &&
                    !currentTrackedTouches.Contains(touch))
                {
                    currentTrackedTouches.Add(touch);
                    count++;
                }
            }
            if (currentTrackedTouches.Count > 1)
            {
                currentTrackedTouches.Sort();
            }
            return count;
        }

        /// <summary>
        /// Stops tracking the specified touch ids
        /// </summary>
        /// <param name="touches">Touches to stop tracking</param>
        /// <returns>The number of touches that stopped tracking</returns>
        private int StopTrackingTouches(ICollection<GestureTouch> touches)
        {
            if (touches == null || touches.Count == 0)
            {
                return 0;
            }
            int count = 0;
            foreach (GestureTouch t in touches)
            {
                for (int i = 0; i < currentTrackedTouches.Count; i++)
                {
                    if (currentTrackedTouches[i].Id == t.Id)
                    {
                        currentTrackedTouches.RemoveAt(i);
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        private void ResetInternal(bool clearCurrentTrackedTouches)
        {
            if (clearCurrentTrackedTouches)
            {
                currentTrackedTouches.Clear();
            }
            requireGestureRecognizersToFailThatHaveFailed.Clear();
            touchStartLocations.Clear();
            StartFocusX = PrevFocusX = StartFocusY = PrevFocusY = float.MinValue;
            FocusX = FocusY = DeltaX = DeltaY = DistanceX = DistanceY = 0.0f;
            Pressure = 0.0f;
            velocityTracker.Reset();
            RemoveFromActiveGestures();
            SetState(GestureRecognizerState.Possible);
        }

        private

#if PCL || PORTABLE || HAS_TASKS

        async

#endif

        static void RunActionAfterDelayInternal(float seconds, Action action)
        {
            if (action == null)
            {
                return;
            }

#if PCL || PORTABLE || HAS_TASKS

            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(seconds));

            action();

#else

            MainThreadCallback(seconds, action);

#endif

        }

        /// <summary>
        /// Ignore a touch until it is released
        /// </summary>
        /// <param name="id">Touch id to ignore</param>
        /// <returns>True if added to ignore list, false if already in ignore list</returns>
        protected bool IgnoreTouch(int id)
        {
            return ignoreTouchIds.Add(id);
        }

        /// <summary>
        /// Track all touches in CurrentTrackedTouches start locations
        /// </summary>
        protected void TrackCurrentTrackedTouchesStartLocations()
        {
            // add start touch locations
            foreach (GestureTouch touch in CurrentTrackedTouches)
            {
                touchStartLocations.Add(new KeyValuePair<float, float>(touch.X, touch.Y));
            }
        }

        /// <summary>
        /// Determines whether any tracked touches are within the distance of the starting point of each tracked touch.
        /// </summary>
        /// <param name="thresholdUnits">Threshold in units</param>
        /// <returns>True if all touches are within thresholdUnits from their start position, false otherwise</returns>
        protected bool AreTrackedTouchesWithinDistance(float thresholdUnits)
        {
            if (CurrentTrackedTouches.Count == 0 || touchStartLocations.Count == 0)
            {
                // this.Log("Distance fail, no current or start locations");
                return false;
            }
            foreach (GestureTouch touch in CurrentTrackedTouches)
            {
                bool withinDistance = false;
                for (int i = touchStartLocations.Count - 1; i >= 0; i--)
                {
                    if (PointsAreWithinDistance(touch.X, touch.Y, touchStartLocations[i].Key, touchStartLocations[i].Value, thresholdUnits))
                    {
                        withinDistance = true;
                        break;
                    }
                    // this.Log("Distance fail: " + touch.X + ", " + touch.Y + ", " + touchStartLocations[i].Key + ", " + touchStartLocations[i].Value + ", " + thresholdUnits);
                }
                if (!withinDistance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculate the focus of the gesture
        /// </summary>
        /// <param name="touches">Touches</param>
        /// <returns>True if this was the first focus calculation, false otherwise</returns>
        protected bool CalculateFocus(ICollection<GestureTouch> touches)
        {
            return CalculateFocus(touches, false);
        }

        /// <summary>
        /// Calculate the focus of the gesture
        /// </summary>
        /// <param name="touches">Touches</param>
        /// <param name="resetFocus">True to force reset of the start focus, false otherwise</param>
        /// <returns>True if this was the first focus calculation, false otherwise</returns>
        protected bool CalculateFocus(ICollection<GestureTouch> touches, bool resetFocus)
        {
            bool first = resetFocus || (StartFocusX == float.MinValue || StartFocusY == float.MinValue);

            FocusX = 0.0f;
            FocusY = 0.0f;
            Pressure = 0.0f;

            foreach (GestureTouch t in touches)
            {
                FocusX += t.X;
                FocusY += t.Y;
                Pressure += t.Pressure;
            }

            float invTouchCount = 1.0f / (float)touches.Count;
            FocusX *= invTouchCount;
            FocusY *= invTouchCount;
            Pressure *= invTouchCount;

            if (first)
            {
                StartFocusX = FocusX;
                StartFocusY = FocusY;
                DeltaX = 0.0f;
                DeltaY = 0.0f;
                velocityTracker.Restart();
            }
            else
            {
                DeltaX = FocusX - PrevFocusX;
                DeltaY = FocusY - PrevFocusY;
            }

            velocityTracker.Update(FocusX, FocusY);

            DistanceX = FocusX - StartFocusX;
            DistanceY = FocusY - StartFocusY;

            PrevFocusX = FocusX;
            PrevFocusY = FocusY;

            return first;
        }

        /// <summary>
        /// Called when state changes
        /// </summary>
        protected virtual void StateChanged()
        {
            // TODO: Remove Updated property
#pragma warning disable 0618

            if (Updated != null)
            {
                Updated(this, currentTrackedTouches);
            }

#pragma warning restore 0618

            if (StateUpdated != null)
            {
                StateUpdated(this);
            }
        }

        /// <summary>
        /// Sets the state of the gesture. Continous gestures should set the executing state every time they change.
        /// </summary>
        /// <param name="value">True if state set successfully, false if the gesture was forced to fail or the state is pending a require gesture recognizer to fail state change</param>
        protected bool SetState(GestureRecognizerState value)
        {
            // this.Log("To state: " + value + ": " + this.ToString());

            if (value == GestureRecognizerState.Failed)
            {
                FailGestureNow();
                return true;
            }
            // if we are trying to execute from a non-executing state and there are gestures already executing,
            // we need to make sure we are allowed to execute simultaneously
            else if (!CanExecuteGestureWithOtherGesturesOrFail(value))
            {
                //this.Log("Failed to execute simultaneously");
                return false;
            }
            else if
            (
                value == GestureRecognizerState.Ended && RequiredGesturesToFailAllowsEndPending()
            )
            {
                // this.Log("END PENDING: " + this);

                // the other gesture will end the state when it fails, or fail this gesture if it executes
                state = GestureRecognizerState.EndPending;
                return false;
            }
            else
            {
                if (value == GestureRecognizerState.Began || value == GestureRecognizerState.Executing)
                {
                    state = value;
                    ActiveGestures.Add(this);
                    UpdateTouchState(value == GestureRecognizerState.Executing);
                    StateChanged();
                }
                else if (value == GestureRecognizerState.Ended)
                {
                    EndGesture();

                    // end after a one frame delay, this allows multiple gestures to properly
                    // fail if no simulatenous execution allowed and there were multiple ending at the same frame
                    ActiveGestures.Add(this);
                    RunActionAfterDelay(0.001f, RemoveFromActiveGestures);
                }
                else
                {
                    state = value;
                    StateChanged();
                }
            }

            return true;
        }

        private bool RequiredGesturesToFailAllowsEndPending()
        {
            if (requireGestureRecognizersToFail.Count > 0)
            {
                using (HashSet<DigitalRubyShared.GestureRecognizer>.Enumerator gestureToFailEnumerator = requireGestureRecognizersToFail.GetEnumerator())
                {
                    while (gestureToFailEnumerator.MoveNext())
                    {
                        // if the require fail gesture is possible and
                        // the require fail gesture has touches or just ended and
                        // the require fail gesture has not jus failed
                        // then requre end pending state for failed gesture check
                        bool isPossible = gestureToFailEnumerator.Current.State == GestureRecognizerState.Possible ||
                            gestureToFailEnumerator.Current.State == GestureRecognizerState.Began ||
                            gestureToFailEnumerator.Current.State == GestureRecognizerState.Executing;
                        bool isTrackingTouches = gestureToFailEnumerator.Current.CurrentTrackedTouches.Count != 0;
                        bool justEnded = gestureToFailEnumerator.Current.justEnded;
                        bool justFailed = gestureToFailEnumerator.Current.justFailed;
                        bool requireEndPending = isPossible && (isTrackingTouches || justEnded) && !justFailed;
                        if (requireEndPending)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasAllRequiredFailGesturesToEndFromEndPending()
        {
            return requireGestureRecognizersToFail.SetEquals(requireGestureRecognizersToFailThatHaveFailed);
        }

        /// <summary>
        /// Call with the touches that began, child class should override
        /// </summary>
        /// <param name="touches">Touches that began</param>
        protected virtual void TouchesBegan(IEnumerable<GestureTouch> touches)
        {

        }

        /// <summary>
        /// Call with the touches that moved, child class should override
        /// </summary>
        /// <param name="touches">Touches that moved</param>
        protected virtual void TouchesMoved()
        {

        }

        /// <summary>
        /// Call with the touches that ended, child class should override
        /// </summary>
        /// <param name="touches">Touches that ended</param>
        protected virtual void TouchesEnded()
        {

        }

        /// <summary>
        /// Begin tracking the specified touch ids
        /// </summary>
        /// <param name="touches">Touches to track</param>
        /// <returns>The number of tracked touches</returns>
        protected int TrackTouches(IEnumerable<GestureTouch> touches)
        {
            return TrackTouchesInternal(touches);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GestureRecognizer()
        {
            state = GestureRecognizerState.Possible;
            PlatformSpecificViewScale = 1.0f;
            StartFocusX = StartFocusY = float.MinValue;
            currentTrackedTouchesReadOnly = new System.Collections.ObjectModel.ReadOnlyCollection<GestureTouch>(currentTrackedTouches);
            AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent = true;
        }

        /// <summary>
        /// Simulate a gesture
        /// </summary>
        /// <param name="xy">List of xy coordinates, repeating</param>
        /// <returns>True if success, false if xy array invalid</returns>
        public bool Simulate(params float[] xy)
        {
            if (xy == null || xy.Length < 2 || xy.Length % 2 != 0)
            {
                return false;
            }
            else if (xy.Length > 3)
            {
                ProcessTouchesBegan(new GestureTouch[] { new GestureTouch(0, xy[2], xy[3], xy[0], xy[1], 1.0f) });
            }
            else
            {
                ProcessTouchesBegan(new GestureTouch[] { new GestureTouch(0, xy[0], xy[1], xy[0], xy[1], 1.0f) });
            }

            for (int i = 2; i < xy.Length - 2; i += 2)
            {
                ProcessTouchesMoved(new GestureTouch[] { new GestureTouch(0, xy[i - 2], xy[i - 1], xy[i], xy[i + 1], 1.0f) });
            }

            if (xy.Length > 3)
            {
                ProcessTouchesEnded(new GestureTouch[] { new GestureTouch(0, xy[xy.Length - 2], xy[xy.Length - 1], xy[xy.Length - 4], xy[xy.Length - 3], 1.0f) });
            }
            else
            {
                ProcessTouchesEnded(new GestureTouch[] { new GestureTouch(0, xy[xy.Length - 2], xy[xy.Length - 1], xy[xy.Length - 2], xy[xy.Length - 1], 1.0f) });
            }

            return true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~GestureRecognizer()
        {
            Dispose();
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return GetType().Name + ": Tracking " + CurrentTrackedTouches.Count + " touches, state = " + State;
        }

        /// <summary>
        /// Reset all internal state  for the gesture recognizer
        /// </summary>
        public virtual void Reset()
        {
            ResetInternal(true);
        }

        /// <summary>
        /// Allows the gesture to restart even if the touches are not lifted. This is only valid when called from the "Ended" state.
        /// </summary>
        /// <returns>True if restart was able to begin, false otherwise</returns>
        public bool BeginGestureRestart()
        {
            if (State == GestureRecognizerState.Ended)
            {
                isRestarting = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restart the gesture with a set of begin touches
        /// </summary>
        /// <param name="touches">Touches</param>
        public bool EndGestureRestart(ICollection<GestureTouch> touches)
        {
            if (isRestarting)
            {
                foreach (GestureTouch touch in touches)
                {
                    if (CurrentTrackedTouches.Contains(touch))
                    {
                        tempTouches.Add(touch);
                    }
                }
                currentTrackedTouches.Clear();
                ProcessTouchesBegan(tempTouches);
                tempTouches.Clear();
                isRestarting = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call with the touches that began
        /// </summary>
        /// <param name="touches">Touches that began</param>
        public void ProcessTouchesBegan(ICollection<GestureTouch> touches)
        {
            justFailed = false;
            justEnded = false;

            if (!Enabled || touches == null || touches.Count == 0)
            {
                return;
            }
            // if the gesture is possible (hasn't started executing) try to track the touches
            else if ((State == GestureRecognizerState.Possible || State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing) && TrackTouches(touches) > 0)
            {
                if (CurrentTrackedTouches.Count > MaximumNumberOfTouchesToTrack)
                {
                    SetState(GestureRecognizerState.Failed);
                }
                else
                {
                    TouchesBegan(touches);
                }
            }
        }

        /// <summary>
        /// Call with the touches that moved
        /// </summary>
        /// <param name="touches">Touches that moved</param>
        public void ProcessTouchesMoved(ICollection<GestureTouch> touches)
        {
            if (!Enabled || touches == null || touches.Count == 0 || !TouchesIntersect(touches, currentTrackedTouches))
            {
                return;
            }
            else if (CurrentTrackedTouches.Count > MaximumNumberOfTouchesToTrack ||
                (State != GestureRecognizerState.Possible && State != GestureRecognizerState.Began && State != GestureRecognizerState.Executing))
            {
                SetState(GestureRecognizerState.Failed);
            }
            else if (!EndGestureRestart(touches))
            {
                UpdateTrackedTouches(touches);
                TouchesMoved();
            }
        }

        /// <summary>
        /// Call with the touches that ended
        /// </summary>
        /// <param name="touches">Touches that ended</param>
        public void ProcessTouchesEnded(ICollection<GestureTouch> touches)
        {
            if (!Enabled || touches == null || touches.Count == 0)
            {
                return;
            }

            try
            {
                foreach (GestureTouch touch in touches)
                {
                    ignoreTouchIds.Remove(touch.Id);
                }

                // if we have the wrong number of tracked touches or haven't started the gesture, fail
                if (!TrackedTouchCountIsWithinRange ||
                    (State != GestureRecognizerState.Possible && State != GestureRecognizerState.Began && State != GestureRecognizerState.Executing))
                {
                    FailGestureNow();
                }
                // if we don have touches we care about, process the end touches
                else if (TouchesIntersect(touches, currentTrackedTouches))
                {
                    UpdateTrackedTouches(touches);
                    TouchesEnded();
                }
            }
            finally
            {
                StopTrackingTouches(touches);
                justEnded = true;
            }
        }

        /// <summary>
        /// Process cancelled touches
        /// </summary>
        /// <param name="touches">Touches</param>
        public void ProcessTouchesCancelled(ICollection<GestureTouch> touches)
        {
            if (!Enabled || touches == null || touches.Count == 0 || !TouchesIntersect(touches, currentTrackedTouches))
            {
                return;
            }

            try
            {
                foreach (GestureTouch t in touches)
                {
                    if (currentTrackedTouches.Contains(t))
                    {
                        SetState(GestureRecognizerState.Failed);
                        return;
                    }
                }
            }
            finally
            {
                StopTrackingTouches(touches);
                justEnded = true;
            }
        }

        /// <summary>
        /// Determines whether two points are within a specified distance
        /// </summary>
        /// <returns>True if within distance false otherwise</returns>
        /// <param name="x1">The first x value in pixels.</param>
        /// <param name="y1">The first y value in pixels.</param>
        /// <param name="x2">The second x value in pixels.</param>
        /// <param name="y2">The second y value in pixels.</param>
        /// <param name="d">Distance in units</param>
        public bool PointsAreWithinDistance(float x1, float y1, float x2, float y2, float d)
        {
            return (DistanceBetweenPoints(x1, y1, x2, y2) <= d);
        }

        /// <summary>
        /// Gets the distance between two points, in units
        /// </summary>
        /// <returns>The distance between the two points in units.</returns>
        /// <param name="x1">The first x value in pixels.</param>
        /// <param name="y1">The first y value in pixels.</param>
        /// <param name="x2">The second x value in pixels.</param>
        /// <param name="y2">The second y value in pixels.</param>
        public float DistanceBetweenPoints(float x1, float y1, float x2, float y2)
        {
            float a = (float)(x2 - x1);
            float b = (float)(y2 - y1);
            float d = (float)Math.Sqrt(a * a + b * b) * PlatformSpecificViewScale;
            return DeviceInfo.PixelsToUnits(d);
        }

        /// <summary>
        /// Gets the distance of a vector, in units
        /// </summary>
        /// <param name="xVector">X vector</param>
        /// <param name="yVector">Y vector</param>
        /// <returns>The distance of the vector in units.</returns>
        public float Distance(float xVector, float yVector)
        {
            float d = (float)Math.Sqrt(xVector * xVector + yVector * yVector) * PlatformSpecificViewScale;
            return DeviceInfo.PixelsToUnits(d);
        }

        /// <summary>
        /// Get the distance of a length, in units
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Distance in units</returns>
        public float Distance(float length)
        {
            float d = Math.Abs(length) * PlatformSpecificViewScale;
            return DeviceInfo.PixelsToUnits(d);
        }

        /// <summary>
        /// Dispose of the gesture and ensure it is removed from the global list of active gestures
        /// </summary>
        public virtual void Dispose()
        {
            RemoveFromActiveGestures();
            foreach (DigitalRubyShared.GestureRecognizer gesture in simultaneousGestures.ToArray())
            {
                DisallowSimultaneousExecution(gesture);
            }
            foreach (DigitalRubyShared.GestureRecognizer gesture in failGestures)
            {
                gesture.requireGestureRecognizersToFail.Remove(this);
            }
        }

        /// <summary>
        /// Allows the simultaneous execution with other gesture. This links both gestures so this method
        /// only needs to be called once on one of the gestures.
        /// Pass null to allow simultaneous execution with all gestures.
        /// </summary>
        /// <param name="other">Gesture to execute simultaneously with</param>
        public void AllowSimultaneousExecution(DigitalRubyShared.GestureRecognizer other)
        {
            other = (other ?? allGesturesReference);
            simultaneousGestures.Add(other);
            if (other != allGesturesReference)
            {
                other.simultaneousGestures.Add(this);
            }
        }

        /// <summary>
        /// Allows simultaneous execution with all gestures
        /// </summary>
        public void AllowSimultaneousExecutionWithAllGestures()
        {
            AllowSimultaneousExecution(null);
        }

        /// <summary>
        /// Disallows the simultaneous execution with other gesture. This unlinks both gestures so this method
        /// only needs to be called once on one of the gestures.
        /// By default, gesures are not allowed to execute simultaneously, so you only need to call this method
        /// if you previously allowed the gestures to execute simultaneously.
        /// Pass null to disallow simulatneous execution with all gestures (i.e. you previously called
        /// AllowSimultaneousExecution with a null value.
        /// </summary>
        /// <param name="other">Gesture to no longer execute simultaneously with</param>
        public void DisallowSimultaneousExecution(DigitalRubyShared.GestureRecognizer other)
        {
            other = (other ?? allGesturesReference);
            simultaneousGestures.Remove(other);
            if (other != allGesturesReference)
            {
                other.simultaneousGestures.Remove(this);
            }
        }

        /// <summary>
        /// Disallows simultaneous execution with all gestures
        /// </summary>
        public void DisallowSimultaneousExecutionWithAllGestures()
        {
            DisallowSimultaneousExecution(null);
        }

        /// <summary>
        /// Require a gesture to fail in order for this gesture to end
        /// </summary>
        /// <param name="gesture">Gesture to require failure on</param>
        public void AddRequiredGestureRecognizerToFail(DigitalRubyShared.GestureRecognizer gesture)
        {
            if (gesture != null)
            {
                requireGestureRecognizersToFail.Add(gesture);
                gesture.failGestures.Add(this);
            }
        }

        /// <summary>
        /// Remove a gesture needing to fail in order for this gesture to end
        /// </summary>
        /// <param name="gesture">Gesture to remove</param>
        public void RemoveRequiredGestureRecognizerToFail(DigitalRubyShared.GestureRecognizer gesture)
        {
            if (gesture != null)
            {
                gesture.failGestures.Remove(this);
                requireGestureRecognizersToFail.Remove(gesture);
            }
        }

        /// <summary>
        /// Run an action on the main thread after a delay
        /// </summary>
        /// <param name="seconds">Delay in seconds</param>
        /// <param name="action">Action to run</param>
        public static void RunActionAfterDelay(float seconds, Action action)
        {
            RunActionAfterDelayInternal(seconds, action);
        }

        /// <summary>
        /// The global total number of gestures in progress
        /// </summary>
        /// <returns>Number of gestures in progress</returns>
        public static int NumberOfGesturesInProgress()
        {
            return ActiveGestures.Count;
        }

        /// <summary>
        /// Whether the gesture is Enabled. Default is true.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                Reset();
            }
        }

        /// <summary>
        /// Get the current gesture recognizer state
        /// </summary>
        /// <value>Gesture recognizer state</value>
        public GestureRecognizerState State { get { return state; } }

        /// <summary>
        /// Executes when the gesture changes
        /// </summary>
        [Obsolete("Please use StateChanged as this property will be removed in a future version.")]
        public event GestureRecognizerUpdated Updated;

        /// <summary>
        /// Fires when state is updated, use this instead of Updated, which has been deprecated.
        /// The gesture object has a CurrentTrackedTouches property where you can access the current touches.
        /// </summary>
        public event GestureRecognizerStateUpdatedDelegate StateUpdated;

        /// <summary>
        /// The current tracked touches for the gesture
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<GestureTouch> CurrentTrackedTouches { get { return currentTrackedTouchesReadOnly; } }

        /// <summary>
        /// Focus x value in pixels (average of all touches)
        /// </summary>
        /// <value>The focus x.</value>
        public float FocusX { get; private set; }

        /// <summary>
        /// Focus y value in pixels (average of all touches)
        /// </summary>
        /// <value>The focus y.</value>
        public float FocusY { get; private set; }

        /// <summary>
        /// Start focus x value in pixels (average of all touches)
        /// </summary>
        /// <value>The focus x.</value>
        public float StartFocusX { get; private set; }

        /// <summary>
        /// Start focus y value in pixels (average of all touches)
        /// </summary>
        /// <value>The focus y.</value>
        public float StartFocusY { get; private set; }

        /// <summary>
        /// Change in focus x in pixels
        /// </summary>
        /// <value>The change in x</value>
        public float DeltaX { get; private set; }

        /// <summary>
        /// Change in focus y in pixels
        /// </summary>
        /// <value>The change in y</value>
        public float DeltaY { get; private set; }

        /// <summary>
        /// The distance (in pixels) the gesture focus has moved from where it started along the x axis
        /// </summary>
        public float DistanceX { get; private set; }

        /// <summary>
        /// The distance (in pixels) the gesture focus has moved from where it started along the y axis
        /// </summary>
        public float DistanceY { get; private set; }

        /// <summary>
        /// Velocity x in pixels using focus
        /// </summary>
        /// <value>The velocity x value in pixels</value>
        public float VelocityX { get { return velocityTracker.VelocityX; } }

        /// <summary>
        /// Velocity y in pixels using focus
        /// </summary>
        /// <value>The velocity y value in pixels</value>
        public float VelocityY { get { return velocityTracker.VelocityY; } }

        /// <summary>
        /// The speed of the gesture in pixels using focus
        /// </summary>
        /// <value>The speed of the gesture</value>
        public float Speed { get { return velocityTracker.Speed; } }

        /// <summary>
        /// Average pressure of all tracked touches
        /// </summary>
        public float Pressure { get; private set; }

        /// <summary>
        /// A platform specific view object that this gesture can execute in, null if none
        /// </summary>
        /// <value>The platform specific view this gesture can execute in</value>
        public object PlatformSpecificView { get; set; }

        /// <summary>
        /// The platform specific view scale (default is 1.0). Change this if the view this gesture is attached to is being scaled.
        /// </summary>
        /// <value>The platform specific view scale</value>
        public float PlatformSpecificViewScale { get; set; }

        /// <summary>
        /// Custom data for the gesture that you can attach, or just leave null if you don't need this
        /// This is useful if you want to retrieve this in a callback function, but it is not require
        /// for the gesture to work properly
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// Convenience method to add / remove one gesture to require failure on. Set to null to clear all require gestures to fail.
        /// </summary>
        public DigitalRubyShared.GestureRecognizer RequireGestureRecognizerToFail
        {
            get
            {
                foreach (DigitalRubyShared.GestureRecognizer gesture in requireGestureRecognizersToFail)
                {
                    return gesture;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    foreach (DigitalRubyShared.GestureRecognizer gesture in requireGestureRecognizersToFail)
                    {
                        gesture.failGestures.Remove(this);
                    }
                    requireGestureRecognizersToFail.Clear();
                }
                else
                {
                    requireGestureRecognizersToFail.Add(value);
                    value.failGestures.Add(this);
                }
            }
        }

        /// <summary>
        /// If this gesture reaches the EndPending state and the specified gestures fail,
        /// this gestures will end. If the specified gesture begins, executes or ends,
        /// then this gesture will immediately fail.
        /// </summary>
        public IEnumerable<DigitalRubyShared.GestureRecognizer> RequireGestureRecognizersToFail
        {
            get { return requireGestureRecognizersToFail; }
        }

        /// <summary>
        /// The minimum number of touches to track. This gesture will not start unless this many touches are tracked. Default is usually 1 or 2.
        /// Not all gestures will honor values higher than 1.
        /// </summary>
        public int MinimumNumberOfTouchesToTrack
        {
            get { return minimumNumberOfTouchesToTrack; }
            set
            {
                minimumNumberOfTouchesToTrack = (value < 1 ? 1 : value);
                if (minimumNumberOfTouchesToTrack > maximumNumberOfTouchesToTrack)
                {
                    maximumNumberOfTouchesToTrack = minimumNumberOfTouchesToTrack;
                }
            }
        }

        /// <summary>
        /// The maximum number of touches to track. This gesture will never track more touches than this. Default is usually 1 or 2.
        /// Not all gestures will honor values higher than 1.
        /// </summary>
        public int MaximumNumberOfTouchesToTrack
        {
            get { return maximumNumberOfTouchesToTrack; }
            set
            {
                maximumNumberOfTouchesToTrack = (value < 1 ? 1 : value);
                if (maximumNumberOfTouchesToTrack < minimumNumberOfTouchesToTrack)
                {
                    minimumNumberOfTouchesToTrack = maximumNumberOfTouchesToTrack;
                }
            }
        }

        /// <summary>
        /// Whether the current number of tracked touches is within the min and max number of touches to track
        /// </summary>
        public bool TrackedTouchCountIsWithinRange
        {
            get { return currentTrackedTouches.Count >= minimumNumberOfTouchesToTrack && currentTrackedTouches.Count <= maximumNumberOfTouchesToTrack; }
        }

        /// <summary>
        /// Whether tracked touches are cleared when the gesture ends or fails, default is false. By setting to true, you allow the gesture to
        /// possibly execute again with a different touch even if the original touch it failed on is still on-going. This is a special case,
        /// so be sure to watch for problems if you set this to true, as leaving it false ensures the most correct behavior, especially
        /// with lots of gestures at once.
        /// </summary>
        public bool ClearTrackedTouchesOnEndOrFail { get; set; }

        /// <summary>
        /// Allows simultaneous execution if the platform specific views do no match. Default is true.
        /// </summary>
        public bool AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent { get; set; }

        /// <summary>
        /// Whether the gesture should reset when it ends
        /// </summary>
        public virtual bool ResetOnEnd { get { return true; } }

        /// <summary>
        /// True if gesture is in process of restarting, false otherwise
        /// </summary>
        public bool IsRestarting { get { return isRestarting; } }

        /// <summary>
        /// Whether additional touches were added to the pan gesture since the last execute state.
        /// </summary>
        public bool ReceivedAdditionalTouches { get; set; }

#if !PCL && !HAS_TASKS

        public delegate void CallbackMainThreadDelegate(float delay, Action callback);

        public static CallbackMainThreadDelegate MainThreadCallback;

#endif

    }

    /// <summary>
    /// Logger for gestures
    /// </summary>
    public static class GestureLogger
    {
        /// <summary>
        /// Log
        /// </summary>
        /// <param name="gesture">Gesture</param>
        /// <param name="text">Text</param>
        /// <param name="args">Args</param>
        public static void Log(this DigitalRubyShared.GestureRecognizer gesture, string text, params object[] args)
        {

#if UNITY_5_3_OR_NEWER

            UnityEngine.Debug.LogFormat(DateTime.UtcNow + " (" + gesture.ToString() + "): " + text, args);

#else

            System.Diagnostics.Debug.WriteLine(DateTime.UtcNow + " (" + gesture.ToString() + "): " + text, args);

#endif

        }
    }
}
