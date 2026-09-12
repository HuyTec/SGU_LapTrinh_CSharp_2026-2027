using System;
using System.Diagnostics.CodeAnalysis;

namespace NMLT.Buoi01
{
    class TongChuSo
    {
        public static void Main(string[] args)
        {
            int n, kq;
            int dv, chuc, tram, ngan;

            Console.Write("moi ban nhap so nguyen n (co bon chu so): ");
            n = int.Parse(Console.ReadLine()!);
            
            dv = (n/1) % 10;
            chuc = (n/10) % 10;
            tram = (n/100) % 10;
            ngan = (n/1000) % 10;
            kq = dv+chuc+tram+ngan;

            Console.Write("{0}={1}+{2}+{3}+{4}={5}", n, ngan, tram, chuc, dv, kq);

            Console.Read();


        }
    }
}