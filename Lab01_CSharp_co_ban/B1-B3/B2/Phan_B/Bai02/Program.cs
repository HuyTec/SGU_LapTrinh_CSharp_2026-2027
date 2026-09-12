/*
Cho số thực a. Hãy tính a2
, a5
và a17 chỉ dùng 6 phép nhân.
*/
using System;
namespace NMLT.Buoi2
{
    class LuyThuaNhanh1
    {
        public static void Main(string[] args)
        {
            double a1, a2, a4,a5, a8,a16, a17;
            Console.Write("moi ban nhap so a: ");
            a1 = double.Parse(Console.ReadLine()!);

            a2 = a1*a1;
            a4 = a2*a2;
            a5 = a4*a1;
            a8 = a4*a4;
            a16 = a8*a8;
            a17 = a16*a1;

            Console.WriteLine("{0}^2 = {1}, {0}^5 ={2}, {0}^17={3}",a1,a2,a5,a17);
            Console.Read();
        }
    }
}