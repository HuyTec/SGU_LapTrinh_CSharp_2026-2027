// / CHƯƠNG TRÌNH TÍNH TOÁN BIỂU THỨC

using System;
namespace NMLT.Buoi01
{
    class BieuThuc
    {
        public static void Main(string []agrs)
        {
            // khai báo biến
            int a, b;
            int KqCong, KqTru, KqNhan, KqChiaNguyen, KqDu;
            double KqChiaThuc;
            // nhập dữ Liệu
            Console.Write("moi ban nhap so a: ");
            a = int.Parse(Console.ReadLine()!);
            Console.Write("Moi ban nhap so b: ");
            b = int.Parse(Console.ReadLine()!);

            // xử lý 
            KqCong = a+b;
            KqTru = a-b;
            KqNhan= a*b;
            KqChiaNguyen= a/b;
            KqDu = a%b;
            KqChiaThuc = a / (double)b;

            // Xuất dữ liệu
            Console.WriteLine("Cac ket qua tinh toan: ");
            Console.WriteLine("{0, -5} + {1, 5} = {2, 5}", a, b, KqCong);
            Console.WriteLine("{0, -5} - {1, 5} = {2, 5}", a, b, KqTru);
            Console.WriteLine("{0, -5} * {1, 5} = {2, 5}", a, b, KqNhan);
            Console.WriteLine("{0, -5} / {1, 5} = {2, 5}", a, b, KqChiaNguyen);
            Console.WriteLine("{0, -5} % {1, 5} = {2, 5}", a, b, KqDu);
            Console.WriteLine("{0, -5} / {1, 5} = {2, 5: #.00}", a, b, KqChiaThuc);
            // Dừng chương trình chờ nhập phím
            Console.Read();

        }
    }
}