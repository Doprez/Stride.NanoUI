using System;
using System.Numerics;

var m3 = new Matrix3x2(1, 2, 3, 4, 100, 200);
var m4 = new Matrix4x4(m3);
Console.WriteLine($"Matrix3x2: M31(tx)={m3.M31} M32(ty)={m3.M32}");
Console.WriteLine($"Matrix4x4 Row3: {m4.M31} {m4.M32} {m4.M33} {m4.M34}");
Console.WriteLine($"Matrix4x4 Row4: {m4.M41} {m4.M42} {m4.M43} {m4.M44}");
