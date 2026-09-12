/*
Cho số thực x. Hãy tính giá trị của hàm số f1(x) và f2(x):

f1(x) = {

0 Nếu x ≤ 0
x Nếu 0 ≤ x ≤ 1
x^4 Nếu x > 1

f2(2) = {
x^2 + 4x + 5 với x ≤ 2
1/(x^2 + 4x + 5)x > 2
*/
using System;
namespace NMLT.Buoi03
{
    class ThangTiengAnh
    {
        public static void Main(string[]args)
        {
            Console.Write("Nhap thang (1-12): ");
            int thang = int.Parse(Console.ReadLine()!);

            switch (thang)
            {
                case 1:
                    Console.WriteLine("January");
                    break;

                case 2:
                    Console.WriteLine("February");
                    break;

                case 3:
                    Console.WriteLine("March");
                    break;

                case 4:
                    Console.WriteLine("April");
                    break;

                case 5:
                    Console.WriteLine("May");
                    break;

                case 6:
                    Console.WriteLine("June");
                    break;

                case 7:
                    Console.WriteLine("July");
                    break;

                case 8:
                    Console.WriteLine("August");
                    break;

                case 9:
                    Console.WriteLine("September");
                    break;

                case 10:
                    Console.WriteLine("October");
                    break;

                case 11:
                    Console.WriteLine("November");
                    break;

                case 12:
                    Console.WriteLine("December");
                    break;

                default:
                    Console.WriteLine("Thang khong hop le");
                    break;
            }


        }
    }
}
