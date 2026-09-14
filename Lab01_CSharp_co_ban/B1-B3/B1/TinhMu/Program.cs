/*Nhập số thực a và số nguyên n. Hãy tính a^n.
*/
using System;

namespace NMLT.Bai01
{
    class HamMu
    {
        public static void Main(string[] args)
        {
            double a, kq;
            int n;

            Console.Write("so thuc a: ");
            a = double.Parse(Console.ReadLine()!);
            Console.Write("so mu n: ");
            n = int.Parse(Console.ReadLine()!);

            kq = Math.Pow(a,n);
            Console.WriteLine("ket qua {0}^{1} = {2}", a,n,kq);
            
            Console.Read();
        }
    }
}