/*Nhập 3 số thực a, b, c. Hãy tìm nghiệm của phương trình bậc 2: ax^2 + bx + c = 0*/
using System;
namespace NMLT.Buoi03
{
    class PhuongTrinhBac2
    {
        public static void Main(string[] args)
        {
            double a,b,c;
            Console.Write("nhap he so a, b, c: ");
            string [] input = Console.ReadLine()!.Split();
            a = double.Parse(input[0]);
            b = double.Parse(input[1]);
            c = double.Parse(input[2]);

            if (a == 0)
            {
                if (b == 0)
                {
                    if(c == 0)
                        Console.WriteLine("Phuong trinh vo so nghiem");
                    else
                        Console.WriteLine("Phuong trinh vo so nghiem");
                }
                else
                {
                    double x = -c/b;
                    Console.WriteLine("Phuong trinh co nghiem x= " + x);
                }
            } else
            {
                double delta = b*b -4*a*c;
                if(delta < 0) 
                    Console.WriteLine("Phuong trinh vo so nghiem");
                else if (delta == 0)
                {
                    double x = -b / (2 * a);
                    Console.WriteLine("Phuong trinh co nghiem kep x = " + x); 
                }
                else
                {
                    double x1 = (-b + Math.Sqrt(delta)) / (2 * a);
                    double x2 = (-b - Math.Sqrt(delta)) / (2 * a);

                    Console.WriteLine("x1 = " + x1);
                    Console.WriteLine("x2 = " + x2);
                }
            }
        }
    }
}
