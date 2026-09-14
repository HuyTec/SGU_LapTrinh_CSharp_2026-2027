/*
Nhập vào bán kính R của một hình tròn. Hãy tính diện tích và chu vi hình tròn đó theo công
thức:S = 3.14xR
2
, P = 2x3.14xR. In kết quả với 1 số lẻ thập phân.
*/
using System;

namespace NMLT.Bai01
{
    class HinhTron
    {
        public static void Main(string []args)
        {
            double r, S, P;

            Console.Write("nhap ban kinh hinh tron: ");
            r =double.Parse(Console.ReadLine()!);
            
            S =3.14*r;
            P =3.14*2*r;

            Console.WriteLine("dien tich S = {0:#00}", S);
            Console.WriteLine("chu vi P= {0:#00}", P);

            Console.Read();
        }
    }
}