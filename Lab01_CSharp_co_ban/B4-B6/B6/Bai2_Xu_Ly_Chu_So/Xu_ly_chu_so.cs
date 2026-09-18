using System;   
namespace Bai2_Xu_Ly_Chu_So
{
    class Xu_ly_chu_so
    {
        static void Main(string[] args)
        {
            // Khai báo biến
            int n, n1;
            int demcs, tongcs, csmax;

            // Nhập dữ liệu
            Console.Write("Moi ban nhap so nguyen n: ");
            n = int.Parse(Console.ReadLine());
            // Xử lý
            n1 = n;
            demcs = 0;      // Đếm chữ số
            tongcs = 0;     // Tổng chữ số
            csmax = -1;     // Chữ số lớn nhất
            // Duyệt các chữ số của n từ phải qua trái
            while (n1 != 0)
            {
                int cs = n1 % 10;    // Lấy chữ số cuối
                demcs++;             // Tăng số lượng chữ số
                tongcs += cs;        // Cộng chữ số vào tổng
                if (csmax == -1)  // Nếu chưa có chữ số nào
                    csmax = cs;
                else
                {
                    // Nếu đã có chữ số
                    if (csmax < cs) csmax = cs;
                }
                n1 = n1 / 10;        // Bỏ chữ số cuối
            }
            // Xuất kết quả
            Console.WriteLine("So {0} co {1} chu so, tong chu so la {2}, chu so lon nhat la {3}.", n, demcs, tongcs, csmax);
        }
    }
}