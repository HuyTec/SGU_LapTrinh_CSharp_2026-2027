/*
Một thiết bị hoạt động được h giờ, m phút và s giây. Hãy viết chương trình chuyển thời gian đó
sang tổng số giây.
*/
using System;
namespace NMLT.Bai01
{
    class DoiGiay
    {
        public static void Main(string[] args)
        {
            int h,m,s,kq;
            
            Console.Write("nhap so gio: ");
            h = int.Parse(Console.ReadLine()!);
             Console.Write("nhap so phut: ");
            m = int.Parse(Console.ReadLine()!);
            Console.Write("nhap so giay: ");
            s = int.Parse(Console.ReadLine()!);

            kq=s+(h*60*60)+(m*60);

            Console.WriteLine("tong so giay cua {0}:{1}:{2} la {3} giay", h,m,s,kq);
            
            Console.Read();

        }
        
    }
}