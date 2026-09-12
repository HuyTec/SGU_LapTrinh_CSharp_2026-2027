/*Nhập vào tháng, năm. Hãy cho biết tháng trong năm vừa nhập có bao nhiêu ngày.*/
using System;
namespace NMLT.Bai05
{
    class SoNgayThangReNhanh
    {
        public static void Main(string[] args)
        {
            int thang, nam;
            Console.Write("Moi ban nhap vao thang, nam: ");
            string[] input = Console.ReadLine()!.Split();

            if (input.Length < 2 || !int.TryParse(input[0], out thang) || !int.TryParse(input[1], out nam))
            {
                Console.WriteLine("Du lieu nhap khong hop le. Vui long nhap theo dinh dang: thang nam");
                return;
            }

            int soNgay = 0;
            switch (thang)
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
                case 4:
                case 6:
                case 9:
                case 11:
                    soNgay = 30;
                    break;
                case 2:
                    if (nam % 400 == 0 || (nam % 4 == 0 && nam % 100 != 0))
                        soNgay = 29;
                    else
                        soNgay = 28;
                    break;
                default:
                    soNgay = 0;
                    break;
            }

            if (soNgay == 0)
                Console.WriteLine("Thang khong hop le");
            else
                Console.WriteLine($"Thang {thang} trong nam {nam} co {soNgay} ngay.");
        }
    }
}
