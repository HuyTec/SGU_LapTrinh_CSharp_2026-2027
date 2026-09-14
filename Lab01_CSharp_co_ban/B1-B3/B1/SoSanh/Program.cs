// Biêu thức so sánh
using System;
namespace NMLT.Buoi01
{
    class SoSanh
    {
        public static void Main(string[] agrs)
        {
            // Khai báo biến
            double a, b;
            bool laNhoHon, laNhoHonBang, laLonHon, laLonHonBang, laBang, laKhacNhau;
            // nhập dữ liệu
            Console.Write("moi ban nhap so a: ");
            a = double.Parse(Console.ReadLine()!);
            Console.Write("moi ban nhap so b: ");
            b = double.Parse(Console.ReadLine()!);
            // xử lý 
            laNhoHon = (a<b);
            laNhoHonBang = (a <= b);
            laLonHon = (a>b);
            laLonHonBang = (a>=b);
            laBang = (a==b);
            laKhacNhau = (a!=b);

            // xuất dữ liệu
            Console.WriteLine(" ket qua so sanh hai so thuc {0:#,#.00} va {1:#,#.00}:",a, b);
            Console.WriteLine("{0:#,#.00} < {1:#,#.00}:{2}",a, b, laNhoHon);
            Console.WriteLine("{0:#,#.00} <= {1:#,#.00}:{2}",a, b, laNhoHonBang);
            Console.WriteLine("{0:#,#.00} > {1:#,#.00}: {2}", a, b, laLonHon);
            Console.WriteLine("{0:#,#.00} >= {1:#,#.00}: {2}", a, b, laLonHonBang);
            Console.WriteLine("{0:#,#.00} == {1:#,#.00}: {2}", a, b, laBang);
            Console.WriteLine("{0:#,#.00} != {1:#,#.00}: {2}", a, b, laKhacNhau);
            // Dừng chương trình chờ nhập phím
            Console.Read();
        }
    }
}