using System;
namespace Ngay_truoc
{
    public class NgayTruoc
    {
        public static void Main(string[] args)
        {
            // Khai báo biến   
            int d, m, y;
            int d1, m1, y1, soNgay = 0;
            bool laNhuan;
            // Nhập dữ liệu
            Console.Write("Moi ban nhap ngay: ");
            d = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap thang: ");
            m = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap nam: ");
            y = int.Parse(Console.ReadLine());
            // Xử lý
            switch (m - 1)
            {
                case 1:
                case 3:
                case 5:
                case 7:
                case 8:
                case 10:
                case 12:
                    soNgay = 31;
                    break;
                case 2:
                    laNhuan = (y % 4 == 0) || (y % 4 == 0 && y % 100 != 0);
                    if (laNhuan)
                    {
                        soNgay = 29;
                    }
                    else
                    {
                        soNgay = 28;
                    }
                    break;
                case 4: case 6: case 9: case 11:
                    soNgay = 30;
                    break;
              
            }
            if (d == 1)
            {
                d1 = soNgay;
                m1 = m - 1;
                y1 = y;
                if (m == 1)
                {
                    m1 = 12;
                    y1 = y - 1;
                }
            }
            else
            {
                d1 = d - 1;
                m1 = m;
                y1 = y;
            }
            // Xuất kết quả
            Console.WriteLine("Ngay truoc ngay {0}/{1}/{2} la ngay {3}/{4}/{5}.", d, m,y, d1, m1, y1);
        }
    }
}