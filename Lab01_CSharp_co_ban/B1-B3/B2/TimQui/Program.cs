/*Nhập vào tháng. Hãy cho biết tháng đó thuộc quí nào?*/
using System;
namespace NMLT.Buoi02
{
    class TimQui
    {
        public static void Main(string[] args)
        {
            int thang, qui;
            Console.Write("moi ban nhap thang: ");
            thang = int.Parse(Console.ReadLine()!);
            qui =   (thang-1)/3 +1;
            Console.WriteLine("thang {0} thuoc qui {1}",thang, qui);
            Console.Read();
            
        }
    }
}