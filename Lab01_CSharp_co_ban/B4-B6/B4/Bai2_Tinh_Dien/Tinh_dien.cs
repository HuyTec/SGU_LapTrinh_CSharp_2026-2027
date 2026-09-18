using System;
namespace Tinh_dien
{
    public class TinhDien
    {
        // Hằng số của chương trình
        const int KWH100 = 1242, KWH150 = 1304;
        const int KWH200 = 1651, KWH300 = 1788, KWH400 = 1912, KWH_TREN_400 = 1962;
        const double VAT = 0.1;
        public static void Main(string[] args)
        {
            // Khai báo biến
            int chiSoCu, chiSoMoi, kwh;
            double soTien;
            // Nhập dữ liệu
            Console.Write("Moi ban nhap so tieu thu cu (kwh): ");
            chiSoCu = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so tieu thu moi (kwh): ");
            chiSoMoi = int.Parse(Console.ReadLine());
            // Xử lý
            kwh = chiSoMoi - chiSoCu;
            if (kwh <= 100)
                soTien = kwh * KWH100;
            else if (kwh <= 150)
                soTien = 100 * KWH100 + (kwh - 100) * KWH150;
            else if (kwh <= 200)
                soTien = 100 * KWH100 + 50 * KWH150 + (kwh - 150) * KWH200;
            else if (kwh <= 300)
                soTien = 100 * KWH100 + 50 * KWH150 + 50 * KWH200 + (kwh - 200) * KWH300;
            else if (kwh <= 400)
                soTien = 100 * KWH100 + 50 * KWH150 + 50 * KWH200 + 100 * KWH300 + (kwh - 300) * KWH400;

            else
                soTien = 100 * KWH100 + 50 * KWH150 + 50 * KWH200 + 100 * KWH300 + 100 * KWH400 + (kwh - 400) * KWH_TREN_400;

            // Tinh VAT
            soTien = (1 + VAT) * soTien;
            // Xuất kết quả
            Console.WriteLine("So tien phai tra cho {0} kWh tieu thu trong thang la {1:#,#} Đ.", kwh, soTien);

        }
    }
}