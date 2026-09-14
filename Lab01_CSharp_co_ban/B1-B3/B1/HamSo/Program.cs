// viết chương trình nhập số thức x và tính giá trị biểu thức
using System;
namespace NML.Buoi01
{
    class HamSo
    {
        public static void Main(String[] args)
        {
            // khai bao bien
            double x, fx;
            // nhap du lieu
            Console.Write("Moi ban nhap vao gia tri cua bien so x: ");
            x = double.Parse(Console.ReadLine()!);

            // xu ly
            fx = x + Math.Pow(x,5)/(1*2*3*4*5) + Math.Sqrt(Math.Abs(x))/(Math.Pow(x, 3/2));

            // xuat du lieu
            Console.WriteLine("Gia tri cua ham so f({0}) = {1:#.00}.",x,fx);

            // dung chuong trinh nhap phim
            Console.Read();

        }
    }
}
