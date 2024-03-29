﻿using System.Globalization;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace BII.WasaBii.UnitSystem.Tests
{
    public class UnitParsingTests
    {

        [Test]
        public void ValidateUnitParsing() {
            Assert.That(Units.TryParse<Length>("1337m"), Is.EqualTo(1337.0.Meters().Some()));
            Assert.That(Units.TryParse<Length>("420 Meters"), Is.EqualTo(420.0.Meters().Some()));
            Assert.That(Units.TryParse<Speed>("420MetersPerSecond"), Is.EqualTo(420.0.MetersPerSecond().Some()));
            Assert.That(Units.TryParse<Speed>("420 meters per second"), Is.EqualTo(420.0.MetersPerSecond().Some()));
            Assert.That(Units.TryParse("288", fallbackUnit: Length.Unit.Centimeters.Instance), 
                Is.EqualTo(288.0.Centimeters().Some()));

            var formatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };
            Assert.That(
                Units.TryParse<Speed>("13.37 km/h", numberFormatInfo: formatInfo),
                Is.EqualTo(13.37.KilometersPerHour().Some()));
            
            formatInfo.NumberDecimalSeparator = ",";
            formatInfo.NumberGroupSeparator = ".";
            Assert.That(
                Units.TryParse<VolumePerDuration>("1.337,0 m³/s", 
                    numberStyles: NumberStyles.Float | NumberStyles.AllowThousands, numberFormatInfo: formatInfo),
                Is.EqualTo(1337.0.CubicMetersPerSecond().Some()));
            
            Assert.That(Units.TryParse<Speed>("420"), Is.EqualTo(Option<Speed>.None));
            Assert.That(Units.TryParse<Mass>(">900kg"), Is.EqualTo(Option<Mass>.None));
        }
        
    }
}