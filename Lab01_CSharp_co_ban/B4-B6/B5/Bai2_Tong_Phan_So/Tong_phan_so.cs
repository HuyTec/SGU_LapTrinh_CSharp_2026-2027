using System;
namespace Bai2_Tong_Phan_So
{
    class Tong_phan_so
    {
        static void Main(string[] args)
        {
            // khai bao bien
            double a, s1, s2, ai;
            int n, i;
            //nhap du lieu
            Console.Write("Moi ban nhap so a: ");
            a = double.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so n: ");
            n = int.Parse(Console.ReadLine());
            // xu ly 
            s1 = 1;
            for (i = 1; i <= n; i++)
            {
                ai = 1.0 / (a + i);
                s1 += ai;
            }
            s2 = 1;
            for (i = 2 * n; i >= 2; i = i - 2)
            {
                ai = (2.0 * i - 1) / (2.0 * i);
                s2 = s2 * ai;
            }
            // xuat ket qua
            Console.WriteLine("Gia tri cua bieu thuc S1(a = {0}, n = {1}) la{2:#.00}.", a, n, s1);
            Console.WriteLine("Gia tri cua bieu thuc S2(n = {0}) la {1:#.00}.", n, s2);
        }
    }
}