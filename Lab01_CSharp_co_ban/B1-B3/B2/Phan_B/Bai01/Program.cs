/*
Nhập vào hai số nguyên a và b (a<=b). Hãy tính tổng các số nằm trong đoạn [a, b]. (Lưu ý: tổng
từ a đến b bằng tổng từ 1 đến b trừ cho tổng từ 1 đến a-1).
S=b(b+1)/2-a(a-1)/2

*/
using System;

namespace NMLT.Buoi2
{
    class TongDoan
    {
        public static void Main(string[] args)
        {
            int a, b;
            Console.Write("Moi ban nhap so a,b: ");
            string[] input = Console.ReadLine()!.Split();
            a = int.Parse(input[0]);
            b = int.Parse(input[1]);

            int S = b*(b+1)/2 - a*(a-1)/2;

            Console.WriteLine("tong cac so tu [{0},{1}] la {2}", a,b,S);
            Console.Read();
        }
    }
}