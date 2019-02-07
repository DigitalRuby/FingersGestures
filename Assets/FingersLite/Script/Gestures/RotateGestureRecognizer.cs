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
    /// A rotate gesture uses two touches to call back rotation angles as the two touches twist around a central point
    /// </summary>
    public class RotateGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        // minimum angle change to rotate the other direction - helps with wobble when panning and rotating at the same time
        private const float minAngleDifferenceToChangeDirection = 0.15f;
        private float startAngle = float.MinValue;
        private float previousAngle;
        private float previousAngleSign;

        private float DifferenceBetweenAngles(float angle1, float angle2)
        {
            float angle = angle1 - angle2;
            return (float)Math.Atan2(Math.Sin(angle), Math.Cos(angle));
        }

        private void UpdateAngle()
        {
            CalculateFocus(CurrentTrackedTouches);
            float currentAngle = CurrentAngle();
            float angleDifferenceFromPrevious = DifferenceBetweenAngles(currentAngle, previousAngle);
            if (angleDifferenceFromPrevious != 0.0f)
            {
                float currentAngleSign = (angleDifferenceFromPrevious >= 0.0f ? 1.0f : -1.0f);
                if (previousAngleSign == 0.0f || currentAngleSign == previousAngleSign || Math.Abs(angleDifferenceFromPrevious) >= minAngleDifferenceToChangeDirection)
                {
                    if (currentAngleSign != previousAngleSign)
                    {
                        previousAngleSign = currentAngleSign;
                        startAngle = previousAngle = currentAngle;
                    }
                    else
                    {
                        float angleDifferenceFromStart = DifferenceBetweenAngles(currentAngle, startAngle);
                        RotationRadians = angleDifferenceFromStart;
                        RotationRadiansDelta = angleDifferenceFromPrevious;
                        previousAngle = currentAngle;
                        SetState(GestureRecognizerState.Executing);
                    }
                }
            }
        }

        private void CheckForStart()
        {
            CalculateFocus(CurrentTrackedTouches);

            if (!TrackedTouchCountIsWithinRange || Distance(DistanceX, DistanceY) < ThresholdUnits)
            {
                return;
            }

            float angle = CurrentAngle();
            if (startAngle == float.MinValue)
            {
                startAngle = previousAngle = angle;
            }
            else
            {
                float angleDiff = Math.Abs(DifferenceBetweenAngles(angle, startAngle));
                if (angleDiff >= AngleThreshold)
                {
                    previousAngleSign = 0.0f;
                    SetState(GestureRecognizerState.Began);
                }
            }
        }

        protected override void StateChanged()
        {
            base.StateChanged();

            if (State == GestureRecognizerState.Ended || State == GestureRecognizerState.Failed)
            {
                startAngle = float.MinValue;
                RotationRadians = 0.0f;
            }
        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            CalculateFocus(CurrentTrackedTouches);
        }

        protected override void TouchesMoved()
        {
            if (CurrentTrackedTouches.Count == MaximumNumberOfTouchesToTrack)
            {
                // we have the right number of touches to do the gesture, check if it's to start or execute
                if (State == GestureRecognizerState.Possible)
                {
                    CheckForStart();
                }
                else if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
                {
                    UpdateAngle();
                }
            }
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Possible)
            {
                // didn't move far enough to rotate, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
            else if (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing)
            {
                CalculateFocus(CurrentTrackedTouches);
                SetState(GestureRecognizerState.Ended);
            }
        }

        /// <summary>
        /// Get the current angle
        /// </summary>
        /// <returns>Current angle</returns>
        protected virtual float CurrentAngle()
        {
            return (float)Math.Atan2(CurrentTrackedTouches[0].Y - CurrentTrackedTouches[1].Y, CurrentTrackedTouches[0].X - CurrentTrackedTouches[1].X);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RotateGestureRecognizer()
        {
            MaximumNumberOfTouchesToTrack = 2;
            AngleThreshold = 0.05f;
        }

        /// <summary>
        /// Angle threshold in radians that must be met before rotation starts - this is the amount of rotation that must happen to start the gesture. Default is 0.05.
        /// </summary>
        /// <value>The angle threshold.</value>
        public float AngleThreshold { get; set; }

        /// <summary>
        /// The gesture focus must change distance by this number of units from the start focus in order to start. Default is 0.0.
        /// </summary>
        /// <value>The threshold in units.</value>
        public float ThresholdUnits { get; set; }

        /// <summary>
        /// The current rotation angle in radians.
        /// </summary>
        /// <value>The rotation angle in radians.</value>
        public float RotationRadians { get; private set; }

        /// <summary>
        /// The change in rotation radians.
        /// </summary>
        /// <value>The rotation radians delta.</value>
        public float RotationRadiansDelta { get; private set; }

        /// <summary>
        /// The current rotation angle in degrees.
        /// </summary>
        /// <value>The rotation angle in degrees.</value>
        public float RotationDegrees { get { return RotationRadians * (180.0f / (float)Math.PI); } }

        /// <summary>
        /// The change in rotation degrees.
        /// </summary>
        /// <value>The rotation degrees delta.</value>
        public float RotationDegreesDelta { get { return RotationRadiansDelta * (180.0f / (float)Math.PI); } }
    }
}

