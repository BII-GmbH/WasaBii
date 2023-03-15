namespace BII.WasaBii.Core.WasaBii_Core
{
    public class Test {
        
        public static Option<int> ForcePositive(int a) => Option.If(a > 0, a);
        
    }
}