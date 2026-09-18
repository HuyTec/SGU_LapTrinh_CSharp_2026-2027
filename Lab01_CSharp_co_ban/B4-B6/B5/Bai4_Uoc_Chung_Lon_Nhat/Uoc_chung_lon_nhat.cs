using System;

namespace Bai4_Uoc_Chung_Lon_Nhat
{
    class Uoc_chung_lon_nhat
    {
        public static void Main(string[] args)
        {
            // Khai báo biến             
            int a, b, ucln;
            int am, bm, tmp;

            // Nhập dữ liệu 
            Console.Write("Moi ban nhap so a: ");
            a = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so b: ");
            b = int.Parse(Console.ReadLine());

            // Xử lý             
            am = a;
            bm = b;
            while (bm != 0)
            {
                tmp = am; am = bm; bm = tmp % am;
            }
            ucln = am;

            // Xuất kết quả 
            Console.WriteLine("Uoc so chung lon nhat cua ({0}, {1}) la {2}.", a, b, ucln);
        }
    }
}
