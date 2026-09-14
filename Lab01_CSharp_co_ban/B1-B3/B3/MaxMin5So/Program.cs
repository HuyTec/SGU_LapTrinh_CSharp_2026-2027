/*Nhập vào giá trị a, b, c, d, e. Hãy tìm giá trị lớn nhất và nhỏ nhất của 5 số a, b, c, d, e.*/
using System;

namespace NMLT.Buoi3
{
    class MaxMin5So
    {
        public static void Main(string[]args)
        {
            double a,b,c,d,e;
            Console.Write("Moi ban nhap 5 so a, b, c, d ,e: ");
            String [] input = Console.ReadLine()!.Split();
            a = double.Parse(input[0]);
            b = double.Parse(input[1]);
            c = double.Parse(input[2]);
            d = double.Parse(input[3]);
            e = double.Parse(input[4]);

            double max=a, min =a;
            if(b>max) max =b;
            if(c>max) max =c;
            if(d>max) max =d;
            if(e>max) max =e;

            if(b<min) min =b;
            if(c<min) min =c;
            if(d<min) min =d;
            if(e<min) min =e;

            Console.WriteLine("Gia tri lon nhat cua {0},{1},{2},{3},{4} la {5}", a,b,c,d,e,max);
            Console.WriteLine("Gia tri nho nhat cua {0},{1},{2},{3},{4} la {5}", a,b,c,d,e,min);
            Console.Read();
        }
    }
}