/*Nhập vào một số nguyên n. Hãy tính toán hai chữ số cuối cùng của n^278
.*/
using System;
namespace NMLT.Buoi2
{
    class  HaiChuSoCuoi
    {
        public static void Main (string []args)
        {
            int n;
            Console.Write("nhap so nguyen a: ");
            n = int.Parse(Console.ReadLine()!);
        int kq = 1;

        for (int i = 1; i <= 278; i++)
        {
        kq = (kq * n) % 100;
        }   
        Console.Write("{0}^278 co 2 chu so cuoi cung la {1}", n,kq);
        Console.Read();
        
        }

    }
}