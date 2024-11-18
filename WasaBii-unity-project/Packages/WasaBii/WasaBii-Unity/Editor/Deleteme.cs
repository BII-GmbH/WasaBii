using System;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Unity.Geometry;
using UnityEngine;

namespace BII.WasaBii.Unity
{
    public class Deleteme : MonoBehaviour
    {

        public Transform[] Handles;
        public Transform[] Velocidies;
        public float[] SectionLengths;
        
        public float T = 0;
        public float speed = 1f;
        public float durationFactor = 1f;
        public bool shouldMove = false;
        public Transform toMove;
        
        // private UnitySpline Spline => UnitySpline.FromHandlesWithVelocities(
        //     Handles.Select(t => t.position).Zip(Velocidies.Select(v => v.localPosition), SectionLengths.Select(f => f.Seconds())));

        private UniformUnitySpline Spline => UniformUnitySpline.FromHandles(
            Handles.Select(t => t.position), 
            SplineType.Chordal);
        // private UnitySpline Spline => UnitySpline.FromHandles(
        // Handles.Select(t => t.position).Zip(SectionLengths.Select(f => f.Seconds() * durationFactor)),
        // SplineType.Chordal);

        private void OnDrawGizmos() {
            var spline = Spline;
            foreach (var (a, b) in spline.SampleSplinePerSegment(200).PairwiseSliding()) {
                var speed = (a.Velocity.magnitude + b.Velocity.magnitude);
                var t1 = (float)Math.Tanh(speed / 30);
                var t2 = (float)Math.Tanh(speed / 20);
                var t3 = (float)Math.Tanh(speed / 10);
                Gizmos.color = Color.HSVToRGB(t1, t2, t3);
                // Gizmos.color = new Color(speed/10, speed/10, speed/10);
                // Gizmos.color = new Color(t, t, t);
                Gizmos.DrawLine(a.Position, b.Position);
            }
        }

        private void Update() {
            if (shouldMove) {
                T += Time.deltaTime * speed;
                // var sample = Spline[T.Seconds()];
                var sample = Spline[T];
                toMove.position = sample.Position;
                toMove.forward = sample.Velocity;
            }
        }
        
        private void OnValidate() {
            var sample = Spline[T];
            // var sample = Spline[T.Seconds()];
            toMove.position = sample.Position;
            toMove.forward = sample.Velocity;
        }

    }
}