/* 
Một thiết bị hoạt động được t giây. Hãy viết chương trình chuyển số giây đó dưới dạng số giờ,
số phút và số giây.
*/
using System;
 namespace NMLT.Bai01
{
    class DoiGio
    {
        public static void Main(string[] args)
        {
            int s,m,h,s1;

            Console.Write("nhap so giay: ");
            s = int.Parse(Console.ReadLine()!);

            h = s/3600;
            m = (s%3600)/60;
            s1 = (s%3600)%60;

            Console.WriteLine("{0} giay co bang {1}:{2}:{3}", s, h, m, s1);
            Console.Read();

        }
    }
    
}