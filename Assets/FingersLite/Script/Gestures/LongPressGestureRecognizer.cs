//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// 


#if PCL || PORTABLE || HAS_TASKS || NETFX_CORE

#define USE_TASKS

using System.Threading.Tasks;

#endif

using System;
using System.Diagnostics;

namespace DigitalRubyShared
{
    /// <summary>
    /// A long press gesture detects a tap and hold and then calls back for movement until
    /// the touch is released
    /// </summary>
    public class LongPressGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        private readonly System.Diagnostics.Stopwatch stopWatch = new Stopwatch();

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            stopWatch.Reset();
            stopWatch.Start();
        }

        protected override void TouchesMoved()
        {
            CalculateFocus(CurrentTrackedTouches);
            if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
            {
                SetState(GestureRecognizerState.Executing);
            }
            else if (State == GestureRecognizerState.Possible && TrackedTouchCountIsWithinRange)
            {
                // if the touch moved too far to count as a long tap, fail the gesture
                float distance = Distance(DistanceX, DistanceY);
                if (distance > ThresholdUnits)
                {
                    SetState(GestureRecognizerState.Failed);
                }
                else
                {
                    if (stopWatch.Elapsed.TotalSeconds >= MinimumDurationSeconds)
                    {
                        SetState(GestureRecognizerState.Began);
                    }
                    else
                    {
                        SetState(GestureRecognizerState.Possible);
                    }
                }
            }
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
            {
                CalculateFocus(CurrentTrackedTouches);
                SetState(GestureRecognizerState.Ended);
            }
            else
            {
                // touch came up too soon, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LongPressGestureRecognizer()
        {
            MinimumDurationSeconds = 0.6f;
            ThresholdUnits = 0.35f;
            ClearTrackedTouchesOnEndOrFail = true;
        }

        /// <summary>
        /// The number of seconds that the touch must stay down to begin executing. Default is 0.6.
        /// </summary>
        /// <value>The minimum long press duration in seconds</value>
        public float MinimumDurationSeconds { get; set; }

        /// <summary>
        /// How many units away the long press can move before failing. After the long press begins,
        /// it is allowed to move any distance and stay executing. Default is 0.35.
        /// </summary>
        /// <value>The threshold in units</value>
        public float ThresholdUnits { get; set; }
    }
}
