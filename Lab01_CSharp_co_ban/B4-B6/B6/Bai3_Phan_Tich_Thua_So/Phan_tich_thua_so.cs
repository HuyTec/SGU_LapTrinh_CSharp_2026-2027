using System;
namespace Bai3_Phan_Tich_Thua_So
{
    public class Phan_tich_thua_so
    {
        public static void Main(string[] args)
        {
            // Khai báo biến
            int n, i;
            // Nhập dữ liệu
            Console.Write("Moi ban nhap so nguyen n: ");
            n = int.Parse(Console.ReadLine());
            // Xử lý
            Console.Write("{0} =", n);
            i = 2; // Xét i: 2, 3, 4, ...
            while (n != 1)
            {
                // Trong khi i còn là ước của n
                while (n != 1 && n % i == 0)
                {
                    n = n / i;
                    if (n == 1)
                    {
                        // Chỉ còn 1 số
                        Console.Write("{0, 2}", i);
                    }
                    else
                    {
                        Console.Write("{0, 2} x", i);
                    }
                }
                // i không là ước của n → tăng i lên 1
                i = i + 1;
            }
            Console.WriteLine();
        }
    }
}