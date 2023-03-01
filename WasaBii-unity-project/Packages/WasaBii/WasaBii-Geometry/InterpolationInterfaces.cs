namespace BII.WasaBii.Geometry
{
    public interface WithLerp<T> where T: WithLerp<T> {
        T LerpTo(T other, double progress, bool shouldClamp = true);
    }
    
    public interface WithSlerp<T> where T: WithSlerp<T> {
        T SlerpTo(T other, double progress, bool shouldClamp = true);
    }
    
}