// See https://aka.ms/new-console-template for more information

using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using Test;

double i = Math.Acos(100);
Angle angle = 1.0.Degrees();
var f = new Foo();
var foo = i > 0 ? Option.Some(i) : Option.None;
var fooo = i > 0 ? Option.Some(i + 1) : Option.None;

var myObj = new MyObj(new Foo());
var fooooo = i > 0 ? i.Some() : Option<double>.None;
var foooooo = Result.If(i > 0, i, 0);

f.Bar = new Bar { 
    Barr = 0
};

var sw = (BestEnum)(int)i switch {
    BestEnum.Foo => 1,
    BestEnum.Bar => 2,
    BestEnum.Baz => 3,
    _ => throw new ArgumentOutOfRangeException()
};

object obj = new Foo();
sw = obj switch {
    BestEnum.Foo => 1,
    Foo => 2,
    Option<Result<Option<int>, BestEnum>> => 3,
    _ => throw new ArgumentOutOfRangeException()
};

Console.WriteLine($"Hello, World! {angle} | {f.Bar.Barr} | {foo}");

record MyObj(Foo Foo);

enum BestEnum { Foo, Bar, Baz }