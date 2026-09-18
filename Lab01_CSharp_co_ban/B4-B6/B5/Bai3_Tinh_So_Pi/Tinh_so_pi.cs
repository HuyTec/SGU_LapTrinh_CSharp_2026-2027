using System;
namespace Bai3_Tinh_So_Pi
{
    class Tinh_so_pi
    {
        static void Main(string[] args)
        {
            // khai bao bien
            double epsilon, pi, ai, i;
            int dau;
            // nhap du lieu
            Console.Write("Moi ban nhap so epsilon: ");
            epsilon = double.Parse(Console.ReadLine());
            // xu ly
            pi = 4;
            i = 1;
            dau = -1;
            ai = 4.0 / (2 * i + 1);
            while (ai >= epsilon)
            {
                pi = pi + dau * ai;
                i = i + 1;
                dau = -dau;
                ai = 4.0 / (2 * i + 1);
            }
            //khi thoat, thoa ai < epsilon
            // xuat ket qua
            Console.WriteLine("So pi tinh do chinh xac {0} la {1}.", epsilon, pi);
            Console.WriteLine("Do sai so thuc te la {0}.", Math.Abs(pi - Math.PI));
        }
    }
}