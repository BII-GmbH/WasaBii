using System;
using BII.WasaBii.UnitSystem;
using NUnit.Framework;

namespace WasaBii_Units.Tests
{
    public class UnitsTests
    {

        [Test]
        public void Degrees2Radians() {
            var degrees = 180.0.Degrees();
            var radians = Math.PI.Radians();
            Assert.That(degrees, Is.EqualTo(radians).WithinUnitValue(1E-7.Radians()));
        }

        [Test]
        public void SecondsMilliseconds() {
            var seconds = 1.0.Seconds();
            var ms = 1000.0.Milliseconds();
            Assert.That(seconds, Is.EqualTo(ms).WithinUnitValue(1E-7.Seconds()));
        }
        
        [Test]
        public void MinutesSeconds() {
            var min = 1.0.Minutes();
            var seconds = 60.0.Seconds();
            Assert.That(min, Is.EqualTo(seconds).WithinUnitValue(1E-7.Seconds()));
        }
        
        [Test]
        public void HoursMinutes() {
            var h = 1.0.Hours();
            var min = 60.0.Minutes();
            Assert.That(h, Is.EqualTo(min).WithinUnitValue(1E-7.Seconds()));
        }
        
        [Test]
        public void DaysHours() {
            var d = 1.0.Days();
            var h = 24.0.Hours();
            Assert.That(d, Is.EqualTo(h).WithinUnitValue(1E-7.Seconds()));
        }
        
        [Test]
        public void WeeksDays() {
            var w = 1.0.Weeks();
            var d = 7.0.Days();
            Assert.That(w, Is.EqualTo(d).WithinUnitValue(1E-7.Seconds()));
        }

        [Test]
        public void CentimetersMillimeters() {
            var cm = 1.0.Centimeters();
            var mm = 10.0.Millimeters();
            Assert.That(cm, Is.EqualTo(mm).WithinUnitValue(1E-7.Meters()));
        }

        [Test]
        public void MetersCentimeters() {
            var m = 1.0.Meters();
            var cm = 100.0.Centimeters();
            Assert.That(m, Is.EqualTo(cm).WithinUnitValue(1E-7.Meters()));
        }
        
        [Test]
        public void KilometersMeters() {
            var km = 1.0.Kilometers();
            var m = 1000.0.Meters();
            Assert.That(km, Is.EqualTo(m).WithinUnitValue(1E-7.Meters()));
        }
        
        [Test]
        public void GramsMilligrams() {
            var g = 1.0.Grams();
            var mg = 1000.0.Milligrams();
            Assert.That(g, Is.EqualTo(mg).WithinUnitValue(1E-7.Meters()));
        }
        
        [Test]
        public void KiloGramsGrams() {
            var kg = 1.0.Kilograms();
            var g = 1000.0.Grams();
            Assert.That(kg, Is.EqualTo(g).WithinUnitValue(1E-7.Meters()));
        }
        
        [Test]
        public void TonsKilograms() {
            var t = 1.0.Tons();
            var kg = 1000.0.Kilograms();
            Assert.That(t, Is.EqualTo(kg).WithinUnitValue(1E-7.Meters()));
        }
        
        [Test]
        public void SquareMeters() {
            var a = 100.0.Meters();
            var b = 200.0.Meters();
            var expected = 20000.0.SquareMeters();
            Assert.That(a * b, Is.EqualTo(expected).WithinUnitValue(1E-7.SquareMeters()));
        }
        
        [Test]
        public void SquareKilometers() {
            var a = 100.0.Kilometers();
            var b = 200.0.Kilometers();
            var expected = 20000.0.SquareKilometers();
            Assert.That(a * b, Is.EqualTo(expected).WithinUnitValue(1E-7.SquareKilometers()));
        }

        [Test]
        public void CubicMeters() {
            var a = 10.0.Meters();
            var c = 20.0.Meters();
            var b = 30.0.Meters();
            var expected = 6000.0.CubicMeters();
            Assert.That(a * b * c, Is.EqualTo(expected).WithinUnitValue(1E-7.CubicMeters()));
        }

        [Test]
        public void Liters() {
            var a = 10.0.Centimeters();
            var c = 20.0.Centimeters();
            var b = 30.0.Centimeters();
            var expected = 6.0.Liters();
            Assert.That(a * b * c, Is.EqualTo(expected).WithinUnitValue(1E-7.CubicMeters()));
        }

        [Test]
        public void DegreesPerSecond() {
            var angle = 288.0.Degrees();
            var duration = 100.0.Seconds();
            var expected = 2.88.DegreesPerSecond();
            Assert.That(angle / duration, Is.EqualTo(expected).WithinUnitValue(1E-7.RadiansPerSecond()));
        }
        
        [Test]
        public void DegreesPerMinute() {
            var angle = 288.0.Degrees();
            var duration = 100.0.Minutes();
            var expected = 2.88.DegreesPerMinute();
            Assert.That(angle / duration, Is.EqualTo(expected).WithinUnitValue(1E-7.RadiansPerSecond()));
        }

        [Test]
        public void MetersPerSecond() {
            var length = 288.0.Meters();
            var duration = 10.0.Seconds();
            var expected = 28.8.MetersPerSecond();
            Assert.That(length / duration, Is.EqualTo(expected).WithinUnitValue(1E-7.MetersPerSecond()));
        }
        
        [Test]
        public void KilometersPerHour() {
            var length = 3600.0.Kilometers();
            var duration = 1000.0.Hours();
            var expected = 3.6.KilometersPerHour();
            Assert.That(length / duration, Is.EqualTo(expected).WithinUnitValue(1E-7.KilometersPerHour()));
        }
        
    }
}