using System;
namespace Sap_xep
{
    class Program
    {
        static void Main(string[] args)
        {
            // Khai bao bien 
            double a, b, c, tmp;
            // Nhap du lieu
            Console.Write("Moi ban nhap so a: ");
            a = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so b: ");
            b = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so c: ");
            c = int.Parse(Console.ReadLine());
            // xuat ket qua
            Console.WriteLine("Cac canh vua nhap la: {0}, {1}, {2}", a, b, c);
            // xu ly
            // sap xep a, b, c, sao cho a <= b <= c
            if (a > b)
            {
                tmp = a;
                a = b;
                b = tmp;
            }
            if (a > c)
            {
                tmp = a;
                a = c;
                c = tmp;
            }
            if (b > c)
            {
                tmp = b;
                b = c;
                c = tmp;
            }
            // xuat ket qua
            Console.WriteLine("Thu tu so a, b, c sau khi sap xep: {0} {1} {2}", a, b, c);
        }
    }
}