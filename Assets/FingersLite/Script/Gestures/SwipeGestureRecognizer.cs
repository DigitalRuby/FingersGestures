//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// 


using System;

namespace DigitalRubyShared
{
    /// <summary>
    /// Swipe gesture directions - assumes 0,0 is in the bottom left
    /// </summary>
    public enum SwipeGestureRecognizerDirection
    {
        /// <summary>
        /// Swipe left
        /// </summary>
        Left,

        /// <summary>
        /// Swipe right
        /// </summary>
        Right,

        /// <summary>
        /// Swipe down
        /// </summary>
        Down,

        /// <summary>
        /// Swipe up
        /// </summary>
        Up,

        /// <summary>
        /// Any direction
        /// </summary>
        Any
    }

    /// <summary>
    /// Swipe gesture recognizer end mode
    /// </summary>
    public enum SwipeGestureRecognizerEndMode
    {
        /// <summary>
        /// End the swipe gesture and do not allow any additional swipes until the touch releases
        /// </summary>
        EndImmediately = 0,

        /// <summary>
        /// End the swipe gesture and allow repeated swipe gestures with the same touch
        /// </summary>
        EndContinusously = 1,

        /// <summary>
        /// Only end the swipe gesture when the touch ends
        /// </summary>
        EndWhenTouchEnds = 2
    }

    /// <summary>
    /// A swipe gesture is a rapid movement in one of five directions: left, right, down, up or any.
    /// </summary>
    public class SwipeGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        private bool CalculateEndDirection(float x, float y)
        {
            SwipeGestureRecognizerDirection endDir = EndDirection;
            float xDiff = VelocityX;
            float yDiff = VelocityY;
            float absXDiff = Math.Abs(xDiff);
            float absYDiff = Math.Abs(yDiff);

            if (absXDiff > absYDiff)
            {
                if (DirectionThreshold > 1.0f && absXDiff / absYDiff < DirectionThreshold)
                {
                    return false;
                }
                else if (xDiff > 0)
                {
                    EndDirection = SwipeGestureRecognizerDirection.Right;
                }
                else
                {
                    EndDirection = SwipeGestureRecognizerDirection.Left;
                }
            }
            else
            {
                if (DirectionThreshold > 1.0f && absYDiff / absXDiff < DirectionThreshold)
                {
                    return false;
                }
                else if (yDiff < 0)
                {
                    EndDirection = SwipeGestureRecognizerDirection.Down;
                }
                else
                {
                    EndDirection = SwipeGestureRecognizerDirection.Up;
                }
            }

            // check for direction change - if so fail the gesture
            if (FailOnDirectionChange && State != GestureRecognizerState.Possible && endDir != SwipeGestureRecognizerDirection.Any && endDir != EndDirection)
            {
                SetState(GestureRecognizerState.Failed);
                return false;
            }

            return true;
        }

        private void CheckForSwipeCompletion(bool end)
        {
            if (Speed < DeviceInfo.UnitsToPixels(MinimumSpeedUnits) || !TrackedTouchCountIsWithinRange)
            {
                // reset focus to current position, we are not going fast enough
                CalculateFocus(CurrentTrackedTouches, true);
                return;
            }

            float distance = DistanceBetweenPoints(StartFocusX, StartFocusY, FocusX, FocusY);
            if (distance < MinimumDistanceUnits || !CalculateEndDirection(FocusX, FocusY))
            {
                // not enough distance covered to be a swipe, or direction failure
                return;
            }
            else if (Direction == SwipeGestureRecognizerDirection.Any || Direction == EndDirection)
            {
                if (end)
                {
                    bool temp = ClearTrackedTouchesOnEndOrFail;
                    if (EndMode != SwipeGestureRecognizerEndMode.EndContinusously)
                    {
                        ClearTrackedTouchesOnEndOrFail = true;
                    }
                    SetState(GestureRecognizerState.Ended);
                    ClearTrackedTouchesOnEndOrFail = temp;
                }
                else if (!SendBeginExecutingStates)
                {
                    SetState(GestureRecognizerState.Possible);
                }
                else if (State == GestureRecognizerState.Possible)
                {
                    SetState(GestureRecognizerState.Began);
                }
                else if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
                {
                    SetState(GestureRecognizerState.Executing);
                }
            }
        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            CalculateFocus(CurrentTrackedTouches);
            EndDirection = SwipeGestureRecognizerDirection.Any;
            SetState(GestureRecognizerState.Possible);
        }

        protected override void TouchesMoved()
        {
            CalculateFocus(CurrentTrackedTouches);
            CheckForSwipeCompletion(EndMode != SwipeGestureRecognizerEndMode.EndWhenTouchEnds);
        }

        protected override void TouchesEnded()
        {
            CalculateFocus(CurrentTrackedTouches);
            if (State == GestureRecognizerState.Possible || State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
            {
                CheckForSwipeCompletion(true);
            }

            // if we didn't end, fail the gesture
            if (State != GestureRecognizerState.Ended)
            {
                SetState(GestureRecognizerState.Failed);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SwipeGestureRecognizer()
        {
            Direction = SwipeGestureRecognizerDirection.Any;
            MinimumDistanceUnits = 1.0f; // default to 1 inch minimum distance
            MinimumSpeedUnits = 3.0f; // must move 3 inches / second speed to execute
            DirectionThreshold = 1.5f;
            EndMode = SwipeGestureRecognizerEndMode.EndImmediately;
            SendBeginExecutingStates = true;
        }

        public override void Reset()
        {
            base.Reset();

            EndDirection = SwipeGestureRecognizerDirection.Any;
        }

        /// <summary>
        /// The swipe direction required to recognize the gesture (default is any)
        /// </summary>
        /// <value>The swipe direction</value>
        public SwipeGestureRecognizerDirection Direction { get; set; }

        /// <summary>
        /// The minimum distance the swipe must travel to be recognized. Default is 1.
        /// </summary>
        /// <value>The minimum distance in units</value>
        public float MinimumDistanceUnits { get; set; }

        /// <summary>
        /// The minimum units per second the swipe must travel to be recognized. Default is 3.0.
        /// </summary>
        /// <value>The minimum speed in units</value>
        public float MinimumSpeedUnits { get; set; }

        /// <summary>
        /// For set directions, this is the amount that the swipe must be proportionally in that direction
        /// vs the other direction. For example, a swipe down gesture will need to move in the y axis
        /// by this multiplier more versus moving along the x axis. Default is 1.5, which means the swipe
        /// down gesture needs to be 1.5 times greater in the y axis vs. the x axis.
        /// Less than or equal to 1 means any ratio is acceptable.
        /// </summary>
        /// <value>The direction threshold.</value>
        public float DirectionThreshold { get; set; }

        /// <summary>
        /// Controls how the swipe gesture ends - see SwipeGestureRecognizerEndMode for more details
        /// </summary>
        /// <value>The swipe mode</value>
        public SwipeGestureRecognizerEndMode EndMode { get; set; }

        /// <summary>
        /// Whether to fail if the gesture changes direction mid swipe
        /// </summary>
        /// <value>True to fail on direction change, false othwerwise</value>
        public bool FailOnDirectionChange { get; set; }

        /// <summary>
        /// The direction of the completed swipe gesture, will be Any if unknown
        /// </summary>
        /// <value>The end direction.</value>
        public SwipeGestureRecognizerDirection EndDirection { get; private set; }

        /// <summary>
        /// Whether to send begin and executing states. Default is true. If false, only possible, ended or failed state is sent.
        /// </summary>
        public bool SendBeginExecutingStates { get; set; }
    }
}
