using System;

namespace NMLT.Buoi07
{
    class In3HinhTamGiac
    {
        /// <summary>
        /// Đơn thể in hình tam giác với chiều cao h
        /// </summary>
        /// <param name="vtx">Tọa độ x khi in chữ</param>
        /// <param name="vty">Tọa độ y khi in chữ</param>
        /// <param name="h">Chiều cao h</param>
        public static void InHinhTamGiac(int vtx, int vty, int h)
        {
            int i, j;

            // In hình tam giác
            for (i = 1; i <= h; i++)
            {
                // Thiết lập tọa độ đầu dòng
                Console.SetCursorPosition(vtx, vty + i - 1);

                // In khoảng trắng bên trái
                for (j = 1; j <= h - i; j++)
                    Console.Write("  ");

                // In dấu *
                for (j = 1; j <= 2 * i - 1; j++)
                    Console.Write("{0,2}", '*');
            }

            // Nhảy tới đầu dòng kết thúc
            Console.SetCursorPosition(0, vty + h);

            // Phục hồi màu chữ
            Console.ResetColor();
        }

        public static void Main(string[] args)
        {
            // Khai báo biến
            int h;

            // Nhập dữ liệu
            Console.Write("Moi ban nhap chieu cao h: ");
            h = int.Parse(Console.ReadLine());

            // In hình tam giác
            Console.WriteLine("h = {0}", h);
            InHinhTamGiac(Console.CursorLeft, Console.CursorTop, h);

            Console.WriteLine("h = {0}", h + 2);
            InHinhTamGiac(Console.CursorLeft, Console.CursorTop, h + 2);

            Console.WriteLine("h = {0}", h + 4);
            InHinhTamGiac(Console.CursorLeft, Console.CursorTop, h + 4);
        }
    }
}