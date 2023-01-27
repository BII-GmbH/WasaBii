using System;
using BII.WasaBii.UnitSystem;
using NUnit.Framework;

namespace WasaBii_Units.Tests
{
    public class UnitsTests
    {

        [Test]
        public void ValidateDefaultUnitConversionFactors() {
            Assert.That(180.0.Degrees(), Is.EqualTo(Math.PI.Radians()).WithinUnitValue(1E-7.Radians()));
            Assert.That(1.0.Seconds(), Is.EqualTo(1000.0.Milliseconds()).WithinUnitValue(1E-7.Seconds()));
            Assert.That(1.0.Minutes(), Is.EqualTo(60.0.Seconds()).WithinUnitValue(1E-7.Seconds()));
            Assert.That(1.0.Hours(), Is.EqualTo(60.0.Minutes()).WithinUnitValue(1E-7.Seconds()));
            Assert.That(1.0.Days(), Is.EqualTo(24.0.Hours()).WithinUnitValue(1E-7.Seconds()));
            Assert.That(1.0.Weeks(), Is.EqualTo(7.0.Days()).WithinUnitValue(1E-7.Seconds()));
            Assert.That(1.0.Centimeters(), Is.EqualTo(10.0.Millimeters()).WithinUnitValue(1E-7.Meters()));
            Assert.That(1.0.Meters(), Is.EqualTo(100.0.Centimeters()).WithinUnitValue(1E-7.Meters()));
            Assert.That(1.0.Kilometers(), Is.EqualTo(1000.0.Meters()).WithinUnitValue(1E-7.Meters()));
            Assert.That(1.0.Grams(), Is.EqualTo(1000.0.Milligrams()).WithinUnitValue(1E-7.Meters()));
            Assert.That(1.0.Kilograms(), Is.EqualTo(1000.0.Grams()).WithinUnitValue(1E-7.Meters()));
            Assert.That(1.0.Tons(), Is.EqualTo(1000.0.Kilograms()).WithinUnitValue(1E-7.Meters()));
            Assert.That(100.0.Meters() * 200.0.Meters(), Is.EqualTo(20000.0.SquareMeters()).WithinUnitValue(1E-7.SquareMeters()));
            Assert.That(100.0.Kilometers() * 200.0.Kilometers(), Is.EqualTo(20000.0.SquareKilometers()).WithinUnitValue(1E-7.SquareKilometers()));
            Assert.That(10.0.Meters() * 20.0.Meters() * 30.0.Meters(), Is.EqualTo(6000.0.CubicMeters()).WithinUnitValue(1E-7.CubicMeters()));
            Assert.That(10.0.Centimeters() * 20.0.Centimeters() * 30.0.Centimeters(), Is.EqualTo(6.0.Liters()).WithinUnitValue(1E-7.CubicMeters()));
            Assert.That(288.0.Degrees() / 100.0.Seconds(), Is.EqualTo(2.88.DegreesPerSecond()).WithinUnitValue(1E-7.RadiansPerSecond()));
            Assert.That(288.0.Degrees() / 100.0.Minutes(), Is.EqualTo(2.88.DegreesPerMinute()).WithinUnitValue(1E-7.RadiansPerSecond()));
            Assert.That(288.0.Meters() / 10.0.Seconds(), Is.EqualTo(28.8.MetersPerSecond()).WithinUnitValue(1E-7.MetersPerSecond()));
            Assert.That(3600.0.Kilometers() / 1000.0.Hours(), Is.EqualTo(3.6.KilometersPerHour()).WithinUnitValue(1E-7.KilometersPerHour()));
            Assert.That(1337.0.Kilograms() / 1.0.Kilometers(), Is.EqualTo(1.337.KilogramsPerMeter()).WithinUnitValue(1E-7.KilogramsPerMeter()));
            Assert.That(60.0.CubicMeters() / 1.0.Minutes(), Is.EqualTo(1.0.CubicMetersPerSecond()).WithinUnitValue(1E-7.CubicMetersPerSecond()));
            Assert.That(288.0.MetersPerSecond() / 100.0.Seconds(), Is.EqualTo(2.88.MetersPerSecondSquared()).WithinUnitValue(1E-7.MetersPerSecondSquared()));
        }
        
    }
}