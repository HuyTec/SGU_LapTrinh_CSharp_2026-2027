/*
Cho số thực x. Hãy tính giá trị của biểu thức sau chỉ dùng phép cộng, trừ, nhân:
f(x) = 1 + 2x + 3x^2– 4x^3
*/
using System;
using System.Security.Cryptography;
namespace NMLT.Buoi2
{
    class BieuThucNhanh1
    {
        public static void Main(string[] args)
        {
            double x,x2, f;
            Console.Write("moi ban nhap  so thuc x: ");
            x = double.Parse(Console.ReadLine()!);

            x2 = x*x;
            f = 1+x*(2+3*x-4*x2);

            Console.WriteLine("f({0}) = {1}", x, f);
            Console.Read();
        }
    }
}