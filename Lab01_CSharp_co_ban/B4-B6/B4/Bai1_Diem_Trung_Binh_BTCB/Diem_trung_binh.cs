using System;
namespace NMLT.Buoi04
{
    public class DiemTrungBinh
    {
        public static void Main(string[] args)
        {
            double toan, ly, hoa, dtb;
            Console.Write("Nhap diem Toan: ");
            toan = double.Parse(Console.ReadLine());
            Console.Write("Nhap diem Ly: ");
            ly = double.Parse(Console.ReadLine());
            Console.Write("Nhap diem Hoa: ");
            hoa = double.Parse(Console.ReadLine());
            dtb = (toan * 2 + ly * 3 + hoa) / 6;
            Console.WriteLine("Diem trung binh: {0:F2}", dtb);
            if (dtb >= 8 && dtb <= 10)
            {
                Console.WriteLine("Xep loai: Gioi");
            }
            else if (dtb >= 6.5)
            {
                Console.WriteLine("Xep loai: Kha");
            }
            else if (dtb >= 5)
            {
                Console.WriteLine("Xep loai: Trung binh");
            }
            else
            {
                Console.WriteLine("Xep loai: Yeu");
            }
        }
    }
}