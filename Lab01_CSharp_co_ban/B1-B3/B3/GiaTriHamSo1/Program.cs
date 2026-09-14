/*
Cho số thực x. Hãy tính giá trị của hàm số f1(x) và f2(x):

f1(x) = {

0 Nếu x ≤ 0
x Nếu 0 ≤ x ≤ 1
x^4 Nếu x > 1

f2(2) = {
x^2 + 4x + 5 với x ≤ 2
1/(x^2 + 4x + 5)x > 2
*/
using System;
namespace NMLT.Buoi3
{
    class GiaTriHamSo1
    {
        public static void Main(string[]args)
        {
            double x;
            Console.Write("Moi ban nhap so thuc x:");
            x = double.Parse(Console.ReadLine()!);
            double f1, f2;
            if (x <= 0)
            {
                f1 = 0;
            } else if  (x >= 0 && x <=1)
            {
                f1 = x;
            } else
            {
                f1= Math.Pow(x,4);
            }

            if (x <= 2)
            {
                f2 =x*x+ 4*x + 5;
            } else
            {
                f2 =  1/(x*x+ 4*x + 5);
            }
            Console.WriteLine("f1({0:#.00})= {1:#.00}",x,f1);
            Console.WriteLine("f1({0:#.00}) = {1:#.00}",x,f2);
            Console.Read();


        }
    }
}