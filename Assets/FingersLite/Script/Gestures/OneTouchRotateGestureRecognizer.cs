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
    /// Allows rotating an object with just one finger. Typically you would put this on a button
    /// with a rotation symbol and then when the user taps and drags off that button, something
    /// would then rotate.
    /// </summary>
    public class OneTouchRotateGestureRecognizer : RotateGestureRecognizer
    {
        /// <summary>
        /// Current angle - if AnglePointOverrideX and AnglePointOverrideY are set, these are used instead of the start touch location to determine the angle.
        /// </summary>
        /// <returns>Current angle</returns>
        protected override float CurrentAngle()
        {
            if (AnglePointOverrideX != float.MinValue && AnglePointOverrideY != float.MinValue && CurrentTrackedTouches.Count != 0)
            {
                GestureTouch t = CurrentTrackedTouches[0];
                return (float)Math.Atan2(t.Y - AnglePointOverrideY, t.X - AnglePointOverrideX);
            }
            return (float)Math.Atan2(DistanceY, DistanceX);
        }

        /// <summary>
        /// Constructor - sets ThresholdUnits to 0.15f and AngleThreshold to 0.0f
        /// </summary>
        public OneTouchRotateGestureRecognizer()
        {
            MaximumNumberOfTouchesToTrack = 1;
            ThresholdUnits = 0.15f;
            AngleThreshold = 0.0f;
        }

        /// <summary>
        /// Normally angle is calculated against the start touch coordinate. This value allows using a different anchor for rotation purposes.
        /// </summary>
        public float AnglePointOverrideX = float.MinValue;

        /// <summary>
        /// Normally angle is calculated against the start touch coordinate. This value allows using a different anchor for rotation purposes.
        /// </summary>
        public float AnglePointOverrideY = float.MinValue;
    }
}

