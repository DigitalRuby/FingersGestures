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
    public static class DeviceInfo
    {
        private static float pixelsPerInch;
        private static float unitMultiplier;
        private static float oneOverUnitMultiplier;

        /// <summary>
        /// Convert centimeters to inches
        /// </summary>
        /// <param name="centimeters">Centimeters</param>
        /// <returns>Inches</returns>
        public static float CentimetersToInches(float centimeters)
        {
            return centimeters * 0.393701f;
        }

        /// <summary>
        /// Convert inches to centimeters
        /// </summary>
        /// <param name="inches">Inches</param>
        /// <returns>Centimeters</returns>
        public static float InchesToCentimeters(float inches)
        {
            return inches * 2.539998f;
        }

        /// <summary>
        /// Convert pixels to units
        /// </summary>
        /// <param name="pixels">Pixels</param>
        /// <returns>Units</returns>
        public static float PixelsToUnits(float pixels)
        {
            return pixels * oneOverUnitMultiplier;
        }

        /// <summary>
        /// Convert units to pixels
        /// </summary>
        /// <param name="units">Units</param>
        /// <returns>Pixels</returns>
        public static float UnitsToPixels(float units)
        {
            return units * UnitMultiplier;
        }

        /// <summary>
        /// Pixels per inch
        /// </summary>
        /// <value>Pixels per inch</value>
        public static float PixelsPerInch
        {
            get { return pixelsPerInch; }
            set { pixelsPerInch = value; }
        }

        /// <summary>
        /// Gets or sets the unit multiplier. For example, if you are specifying units in inches,
        /// you would want to set this to PixelsPerInch. If you want to use cm, you can
        /// set this to InchesToCentimeters(PixelsPerInch)
        /// </summary>
        /// <value>The unit multiplier.</value>
        public static float UnitMultiplier
        {
            get { return unitMultiplier; }
            set
            {
                value = Math.Max(0.00001f, value);
                unitMultiplier = value;
                oneOverUnitMultiplier = 1.0f / value;
            }
        }
    }
}

