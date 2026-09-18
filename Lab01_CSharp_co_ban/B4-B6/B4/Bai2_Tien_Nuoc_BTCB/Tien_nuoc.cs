using System;

namespace Bai2_Tien_Nuoc_BTCB
{
    public class Tien_nuoc
    {
        public static void Main(string[] args)
        {
            double chiSoCu, chiSoMoi;
            int soNguoi;
            double soKhoi;
            double tienNuoc = 0;
            double thue, phiMoiTruong, tongTien;
            Console.Write("Nhap chi so nuoc cu: ");
            chiSoCu = double.Parse(Console.ReadLine());
            Console.Write("Nhap chi so nuoc moi: ");
            chiSoMoi = double.Parse(Console.ReadLine());
            Console.Write("Nhap so nguoi: ");
            soNguoi = int.Parse(Console.ReadLine());
            soKhoi = chiSoMoi - chiSoCu;
            double dinhMuc = soNguoi * 4;
            if (soKhoi <= dinhMuc)
            {
                tienNuoc = soKhoi * 4400;
            }
            else
            {
                tienNuoc = dinhMuc * 4400;
                soKhoi = soKhoi - dinhMuc;

                dinhMuc = soNguoi * 2;

                if (soKhoi <= dinhMuc)
                {
                    tienNuoc += soKhoi * 8300;
                }
                else
                {
                    tienNuoc += dinhMuc * 8300;
                    soKhoi = soKhoi - dinhMuc;

                    tienNuoc += soKhoi * 10500;
                }
            }
            thue = tienNuoc * 5 / 100;
            phiMoiTruong = tienNuoc * 10 / 100;
            tongTien = tienNuoc + thue + phiMoiTruong;
            Console.WriteLine("Tien nuoc: {0:N0} dong", tienNuoc);
            Console.WriteLine("Thue GTGT: {0:N0} dong", thue);
            Console.WriteLine("Phi moi truong: {0:N0} dong", phiMoiTruong);
            Console.WriteLine("Tong tien phai tra: {0:N0} dong", tongTien);
        }
    }
}