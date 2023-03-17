using System;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry
{
    public interface WithLerp<T> where T: WithLerp<T> {
        [Pure] T LerpTo(T other, double progress, bool shouldClamp = true);
        
        /// <summary>
        /// Lerps between two values such that the interpolation can go on indefinitely,
        /// approaching the target asymptotically. See <see cref="BII.WasaBii.Extra.SmoothInterpolation"/>
        /// for more information.
        /// </summary>
        [Pure] T SmoothLerpTo(T other, double smoothness, double progress) => 
            LerpTo(other, Math.Pow(smoothness, progress));
    }
    
    public interface WithSlerp<T> where T: WithSlerp<T> {
        [Pure] T SlerpTo(T other, double progress, bool shouldClamp = true);
        
        /// <summary>
        /// Slerps between two values such that the interpolation can go on indefinitely,
        /// approaching the target asymptotically. See <see cref="BII.WasaBii.Extra.SmoothInterpolation"/>
        /// for more information.
        /// </summary>
        [Pure] T SmoothSlerpTo(T other, double smoothness, double progress) => 
            SlerpTo(other, Math.Pow(smoothness, progress));
    }
    
}