using System;
namespace Bai4_Kiem_Tra_Nguyen_To
{
    public class Kiem_tra_nguyen_to
    {
        public static void Main(string[] args)
        {
            // Khai báo biến
            int n, i;
            bool laNguyenTo;
            // Nhập dữ liệu
            Console.Write("Moi ban nhap n: ");
            n = int.Parse(Console.ReadLine());
            // Xử lý
            laNguyenTo = true;
            // Kiểm tra các ước từ 2 đến căn bậc hai của n
            // Dùng cờ laNguyenTo thay cho break
            for (i = 2; i * i <= n && laNguyenTo == true; i++)
            {
                if (n % i == 0)
                    laNguyenTo = false;
            }
            // Xuất kết quả
            if (laNguyenTo)
                Console.WriteLine("So {0} la nguyen to.", n);
            else
                Console.WriteLine("So {0} khong la nguyen to.", n);
        }
    }
}