using BII.WasaBii.Core;

namespace Test; 

[MustBeImmutable]
public class Foo {
    public Bar Bar = new();
    public int someVal;
}
[MustBeImmutable]
public class Bar {
    public int Barr;
}