/*Cho 3 số thực a, b, c là độ dài 3 cạnh của một tam giác. Hãy tính diện tích tam giác này theo
công thức Heron
*/
using System;
namespace NMLt.Bai01
{
    class DienTichTamGiac
    {
        public static void Main(string[] args)
        {
            double a, b, c, P, S;

            Console.Write("nhap canh a: ");
            a = double.Parse(Console.ReadLine()!);
            Console.Write("nhap canh b: ");
            b = double.Parse(Console.ReadLine()!);
            Console.Write("nhap canh c: ");
            c = double.Parse(Console.ReadLine()!);

            P =(a+b+c)/2;
            S = Math.Sqrt(P*(P-a)*(P-b)*(P-c));
            Console.WriteLine("dien tich tam giac S = {0:#. 00}", S);
        
            Console.Read();
                
        }
    }
}