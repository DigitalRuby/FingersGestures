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
    public class OneTouchScaleGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        public OneTouchScaleGestureRecognizer()
        {
            ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
            ThresholdUnits = 0.15f;

#if UNITY_5_3_OR_NEWER

            ZoomSpeed = -0.2f;

#else

            ZoomSpeed = 0.2f;

#endif

        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            CalculateFocus(CurrentTrackedTouches);
            SetState(GestureRecognizerState.Possible);
        }

        protected override void TouchesMoved()
        {
            CalculateFocus(CurrentTrackedTouches);

            if (!TrackedTouchCountIsWithinRange)
            {
                return;
            }
            else if (State == GestureRecognizerState.Possible)
            {
                // see if we have moved far enough to start scaling
                if (Distance(DistanceX, DistanceY) < ThresholdUnits)
                {
                    return;
                }

                // begin the gesture
                ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
                SetState(GestureRecognizerState.Began);
            }
            else if (DeltaX != 0.0f || DeltaY != 0.0f)
            {
                // continue the gesture
                ScaleMultiplier = 1.0f + (Distance(DeltaX, DeltaY) * Math.Sign(DeltaY) * ZoomSpeed);
                ScaleMultiplierX = 1.0f + (Distance(DeltaX) * -Math.Sign(DeltaX) * ZoomSpeed);
                ScaleMultiplierY = 1.0f + (Distance(DeltaY) * Math.Sign(DeltaY) * ZoomSpeed);
                SetState(GestureRecognizerState.Executing);
            }
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Possible)
            {
                // didn't move far enough to start scaling, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
            else
            {
                CalculateFocus(CurrentTrackedTouches);
                SetState(GestureRecognizerState.Ended);
            }
        }

        /// <summary>
        /// The current scale multiplier. Multiply your current scale value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplier { get; private set; }

        /// <summary>
        /// The current scale multiplier. Multiply your current x scale value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierX { get; private set; }

        /// <summary>
        /// The current scale multiplier. Multiply your current y scale value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierY { get; private set; }

        /// <summary>
        /// Additional multiplier for ScaleMultipliers. This will making scaling happen slower or faster.
        /// For this one finger scale gesture, this value is generally a small fraction, like 0.2 (the default).
        /// For a UI that starts 0,0 in the bottom left (like Unity), this value should be negative.
        /// For most other UI (0,0 in top left), this should be positive.
        /// </summary>
        /// <value>The zoom speed.</value>
        public float ZoomSpeed { get; set; }

        /// <summary>
        /// The threshold in units that the touch must move to start the gesture. Default is 0.15.
        /// </summary>
        /// <value>The threshold units.</value>
        public float ThresholdUnits { get; set; }
    }
}