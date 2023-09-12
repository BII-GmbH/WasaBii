using System.Globalization;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace BII.WasaBii.UnitSystem.Tests
{
    public class UnitParsingTests
    {

        [Test]
        public void ValidateUnitParsing() {
            Assert.That(Units.TryParse<Length, Length.Unit>("1337m"), Is.EqualTo(1337.0.Meters().Some()));
            Assert.That(Units.TryParse<Length, Length.Unit>("420 Meters"), Is.EqualTo(420.0.Meters().Some()));
            Assert.That(Units.TryParse<Velocity, Velocity.Unit>("420MetersPerSecond"), Is.EqualTo(420.0.MetersPerSecond().Some()));
            Assert.That(Units.TryParse<Velocity, Velocity.Unit>("420 meters per second"), Is.EqualTo(420.0.MetersPerSecond().Some()));
            Assert.That(Units.TryParse<Length, Length.Unit>("288", fallbackUnit: Length.Unit.Centimeters.Instance), 
                Is.EqualTo(288.0.Centimeters().Some()));

            var formatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };
            Assert.That(
                Units.TryParse<Velocity, Velocity.Unit>("13.37 km/h", numberFormatInfo: formatInfo),
                Is.EqualTo(13.37.KilometersPerHour().Some()));
            
            formatInfo.NumberDecimalSeparator = ",";
            formatInfo.NumberGroupSeparator = ".";
            Assert.That(
                Units.TryParse<VolumePerDuration, VolumePerDuration.Unit>("1.337,0 m³/s", 
                    numberStyles: NumberStyles.Float | NumberStyles.AllowThousands, numberFormatInfo: formatInfo),
                Is.EqualTo(1337.0.CubicMetersPerSecond().Some()));
            
            Assert.That(Units.TryParse<Velocity, Velocity.Unit>("420"), Is.EqualTo(Option<Velocity>.None));
            Assert.That(Units.TryParse<Mass, Mass.Unit>(">900kg"), Is.EqualTo(Option<Mass>.None));
        }
        
    }
}