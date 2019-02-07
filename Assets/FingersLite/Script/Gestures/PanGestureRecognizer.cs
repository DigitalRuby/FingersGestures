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
    /// A pan gesture detects movement of a touch
    /// </summary>
    public class PanGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        private void ProcessTouches(bool resetFocus)
        {
            bool firstFocus = CalculateFocus(CurrentTrackedTouches, resetFocus);

            if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
            {
                SetState(GestureRecognizerState.Executing);
            }
            else if (firstFocus)
            {
                SetState(GestureRecognizerState.Possible);
            }
            else if (State == GestureRecognizerState.Possible && TrackedTouchCountIsWithinRange)
            {
                float distance = Distance(DistanceX, DistanceY);
                if (distance >= ThresholdUnits)
                {
                    SetState(GestureRecognizerState.Began);
                }
                else
                {
                    SetState(GestureRecognizerState.Possible);
                }
            }
        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            ProcessTouches(true);
        }

        protected override void TouchesMoved()
        {
            ProcessTouches(false);
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Possible)
            {
                // didn't move far enough to start a pan, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
            else
            {
                ProcessTouches(false);
                SetState(GestureRecognizerState.Ended);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PanGestureRecognizer()
        {
            ThresholdUnits = 0.2f;
        }

        /// <summary>
        /// How many units away the pan must move to execute - default is 0.2
        /// </summary>
        /// <value>The threshold in units</value>
        public float ThresholdUnits { get; set; }
    }
}

