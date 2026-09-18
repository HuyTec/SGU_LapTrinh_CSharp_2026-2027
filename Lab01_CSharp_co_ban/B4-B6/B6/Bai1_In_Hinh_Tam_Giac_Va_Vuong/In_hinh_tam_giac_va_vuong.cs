using System;

namespace Bai1_In_hinh_tam_giac_va_vuong
{
    public class In_hinh_tam_giac_va_vuong
    {
        public static void Main(string[] args)
        {
            // Khai báo biến             
            int h, i, j;

            // Nhập dữ liệu 
            Console.Write("Moi ban nhap chieu cao h: ");
            h = int.Parse(Console.ReadLine());
            // Thiết lập màu chữ 
            Console.ForegroundColor = ConsoleColor.Red;
            // In tam giác 
            // Dòng 1: Thiết lập tọa độ đầu dòng, In h-1 khoảng trắng, 1 dấu *             
            Console.SetCursorPosition(0, 1);
            for (i = 1; i <= h - 1; i++) Console.Write("{0, 2}", ' ');
            Console.Write("{0,2}", '*');
            // Dòng thứ 2 den h-1            
            for (i = 2; i <= h - 1; i++)
            {
                // Thiết lập tọa độ đầu dòng i 
                Console.SetCursorPosition(0, i);
                // In (h-1) - (i - 1) khoảng trắng, 1 dấu * 
                for (j = 1; j <= (h - 1) - (i - 1); j++) Console.Write("{0, 2}", ' ');
                Console.Write("{0, 2}", '*');
                // In 2 * (i - 1) - 1 khoảng trắng, 1 dấu * 
                for (j = 1; j <= 2 * (i - 1) - 1; j++) Console.Write("{0, 2}", ' ');
                Console.Write("{0, 2}", '*');
            }
            // Dòng h: Thiết lập tọa độ đầu dòng, In 2h - 1 dấu *             
            Console.SetCursorPosition(0, h);
            for (i = 1; i <= 2 * h - 1; i++) Console.Write("{0, 2}", '*');
            // In hình vuông 
            // Dòng 1: // Thiết lập tọa độ đầu dòng, In h dấu *             
            Console.SetCursorPosition(2 * (2 * h - 1) + 1, 1);
            for (i = 1; i <= h; i++) Console.Write("{0, 2}", '*');
            // Dòng 2 đến h-1:              
            for (i = 2; i <= h - 1; i++)
            {
                // Thiết lập đầu dòng i 
                Console.SetCursorPosition(2 * (2 * h - 1) + 1, i);
                // In 1 đấu *, h - 2 khoảng trắng, 1 dấu *                 
                Console.Write("{0, 2}", '*');
                for (j = 1; j <= h - 2; j++) Console.Write("{0, 2}", ' ');
                Console.Write("{0, 2}", '*');
            }
            // Dòng h: // Thiết lập đầu dòng h, In h dấu *             
            Console.SetCursorPosition(2 * (2 * h - 1) + 1, h);
            for (i = 1; i <= h; i++) Console.Write("{0, 2}", '*');
            // Nhảy tới đầu dòng kết thúc, phục hồi màu chữ 
            Console.SetCursorPosition(0, h + 1);
            Console.ResetColor();
        }
    }
}






