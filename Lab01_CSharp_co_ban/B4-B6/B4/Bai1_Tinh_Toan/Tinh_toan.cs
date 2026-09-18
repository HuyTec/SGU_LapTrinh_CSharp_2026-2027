using System;
namespace Tinh_toan
{
    public class TinhToan
    {
        public static void Main(string[] args)
        {
            // Khai báo biến
            double a, b, kq = 0;
            char cPhepTinh;
            bool laTinhDuoc;
            // Nhập dữ liệu
            Console.Write("Moi ban nhap so thu 1: ");
            a = double.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap so thu 2: ");
            b = double.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap phep toan: ");
            cPhepTinh = char.Parse(Console.ReadLine());
            // Xử lý
            laTinhDuoc = true;
             // giả sử là tính được (kỹ thuật cầm canh)
            if (cPhepTinh == '+')
                kq = a + b;
            else if (cPhepTinh == '-')
                kq = a - b;
            else if (cPhepTinh == '*')
                kq = a * b;
            else if(cPhepTinh == '/')
            {
                if (b == 0)
                {
                    // không tính được
                    laTinhDuoc = false;
                }
                else
                {
                    kq = a / b;
                }
                }
                // Xuất kết quả
                if(laTinhDuoc == false)
                    Console.WriteLine("Ket qua: {0} {1} {2} khong hop le.", a, cPhepTinh,b);
                else
                    Console.WriteLine("Ket qua: {0} {1} {2} = {3}.", a, cPhepTinh, b, kq);
                }
                }
                }