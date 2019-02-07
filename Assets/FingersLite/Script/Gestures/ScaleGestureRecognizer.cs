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
    /// A scale gesture detects two fingers moving towards or away from each other to scale something
    /// </summary>
    public class ScaleGestureRecognizer : DigitalRubyShared.GestureRecognizer
    {
        // these five constants to determine the hysteresis to reduce or eliminate wobble as a zoom is done,
        //  think of a two finger pan and zoom at the same time, tiny changes in scale direction are ignored
        private const float minimumScaleResolutionSquared = 1.005f; // incremental changes
        private const float stationaryScaleResolutionSquared = 1.05f; // change from idle
        private const float stationaryTimeSeconds = 0.1f; // if stationary for this long, use stationaryScaleResolutionSquared else minimumScaleResolutionSquared
        private const float hysteresisScaleResolutionSquared = 1.15f; // higher values resist scaling in the opposite direction more
        private const int resetDirectionMilliseconds = 250;

        // the min amount that can scale down each update
        private const float minimumScaleDownPerUpdate = 0.25f;

        // the max amount that can scale up each update
        private const float maximumScaleUpPerUpdate = 4.0f;

        private float previousDistanceDirection;
        private float previousDistance;
        private float previousDistanceX;
        private float previousDistanceY;

        private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        public ScaleGestureRecognizer()
        {
            ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
            ZoomSpeed = 3.0f;
            ThresholdUnits = 0.15f;
            MinimumNumberOfTouchesToTrack = MaximumNumberOfTouchesToTrack = 2;
            timer.Start();
        }

        private void SetPreviousDistance(float distance, float distanceX, float distanceY)
        {
            previousDistance = distance;
            previousDistanceX = distanceX;
            previousDistanceY = distanceY;
        }

        private float ClampScale(float rawScale)
        {
            return (rawScale > maximumScaleUpPerUpdate ? maximumScaleUpPerUpdate : (rawScale < minimumScaleDownPerUpdate ? minimumScaleDownPerUpdate : rawScale));
        }

        private float GetScale(float rawScale)
        {
            rawScale = ClampScale(rawScale);
            if (ZoomSpeed != 1.0f)
            {
                if (rawScale < 1.0f)
                {
                    rawScale -= ((1.0f - rawScale) * ZoomSpeed);
                }
                else if (rawScale > 1.0f)
                {
                    rawScale += ((rawScale - 1.0f) * ZoomSpeed);
                }

                // clamp again to account for zoom speed modifiers
                rawScale = (rawScale > maximumScaleUpPerUpdate ? maximumScaleUpPerUpdate : (rawScale < minimumScaleDownPerUpdate ? minimumScaleDownPerUpdate : rawScale));
            }
            return rawScale;
        }

        private void ProcessTouches()
        {
            CalculateFocus(CurrentTrackedTouches);

            if (!TrackedTouchCountIsWithinRange)
            {
                return;
            }

            float distance = DistanceBetweenPoints(CurrentTrackedTouches[0].X, CurrentTrackedTouches[0].Y, CurrentTrackedTouches[1].X, CurrentTrackedTouches[1].Y);
            float distanceX = Distance(CurrentTrackedTouches[0].X - CurrentTrackedTouches[1].X);
            float distanceY = Distance(CurrentTrackedTouches[0].Y - CurrentTrackedTouches[1].Y);

            if (State == GestureRecognizerState.Possible)
            {
                if (previousDistance == 0.0f)
                {
                    // until the gesture starts, previousDistance is actually firstDistance
                    previousDistance = distance;
                    previousDistanceX = distanceX;
                    previousDistanceY = distanceY;
                }
                else
                {
                    float diff = Math.Abs(previousDistance - distance);
                    if (diff >= ThresholdUnits)
                    {
                        SetState(GestureRecognizerState.Began);
                    }
                }
            }
            else if (State == GestureRecognizerState.Executing)
            {
                // must have a change in distance to execute
                if (distance != previousDistance)
                {
                    // line 3300: https://chromium.googlesource.com/chromiumos/platform/gestures/+/master/src/immediate_interpreter.cc

                    // get jitter threshold based on stationary movement or not
                    float jitterThreshold = (float)timer.Elapsed.TotalSeconds <= stationaryTimeSeconds ? minimumScaleResolutionSquared : stationaryScaleResolutionSquared;

                    // calculate distance suqared
                    float currentDistanceSquared = distance * distance;
                    float previousDistanceSquared = previousDistance * previousDistance;

                    // if a change in direction, the jitter threshold can be increased to determine whether the change in direction is significant enough
                    if ((currentDistanceSquared - previousDistanceSquared) * previousDistanceDirection < 0.0f)
                    {
                        jitterThreshold = Math.Max(jitterThreshold, hysteresisScaleResolutionSquared);
                    }

                    // check if we are above the jitter threshold - will always be true if moving in the same direction as last time
                    bool aboveJitterThreshold = ((previousDistanceSquared > jitterThreshold * currentDistanceSquared) ||
                        (currentDistanceSquared > jitterThreshold * previousDistanceSquared));

                    // must be above jitter threshold to execute
                    if (aboveJitterThreshold)
                    {
                        timer.Reset();
                        timer.Start();
                        float newDistanceDirection = (currentDistanceSquared - previousDistanceSquared >= 0.0f ? 1.0f : -1.0f);
                        if (newDistanceDirection == previousDistanceDirection)
                        {
                            ScaleMultiplier = GetScale(distance / previousDistance);
                            ScaleMultiplierX = GetScale(distanceX / previousDistanceX);
                            ScaleMultiplierY = GetScale(distanceY / previousDistanceY);
                            SetState(GestureRecognizerState.Executing);
                        }
                        else
                        {
                            ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
                            previousDistanceDirection = newDistanceDirection;
                        }
                        SetPreviousDistance(distance, distanceX, distanceY);
                    }
                    else if (timer.ElapsedMilliseconds > resetDirectionMilliseconds)
                    {
                        previousDistanceDirection = 0.0f;
                    }
                }
            }
            else if (State == GestureRecognizerState.Began)
            {
                ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
                previousDistanceDirection = 0.0f;
                SetPreviousDistance(distance, distanceX, distanceY);
                SetState(GestureRecognizerState.Executing);
            }
            else
            {
                SetState(GestureRecognizerState.Possible);
            }
        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            previousDistance = 0.0f;
        }

        protected override void TouchesMoved()
        {
            ProcessTouches();
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Executing)
            {
                CalculateFocus(CurrentTrackedTouches);
                SetState(GestureRecognizerState.Ended);
            }
            else
            {
                // didn't get to the executing state, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
        }

        /// <summary>
        /// The current scale multiplier. Multiply your current scale value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplier { get; private set; }

        /// <summary>
        /// The current scale multiplier for x axis. Multiply your current scale x value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierX { get; private set; }

        /// <summary>
        /// The current scale multiplier for y axis. Multiply your current scale y value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierY { get; private set; }

        /// <summary>
        /// Additional multiplier for ScaleMultipliers. This will making scaling happen slower or faster. Default is 3.0.
        /// </summary>
        /// <value>The zoom speed.</value>
        public float ZoomSpeed { get; set; }

        /// <summary>
        /// How many units the distance between the fingers must increase or decrease from the start distance to begin executing. Default is 0.15.
        /// </summary>
        /// <value>The threshold in units</value>
        public float ThresholdUnits { get; set; }
    }
}
