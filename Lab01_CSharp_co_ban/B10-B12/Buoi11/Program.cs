
using System;
using System.Globalization;
public static class Program
{
    public static T[] nhapDaySo<T>(ref int n)
    {
        Console.WriteLine("Nhap so luong phan tu trong day so (n <= 500):");
        do
        {
            n = int.Parse(Console.ReadLine());
            if (n <= 0 || n > 500)
                Console.WriteLine("So phan tu phai trong khoang (0, 500]. Nhap lai:");
        } while (n <= 0 || n > 500);

        T[] day = new T[n];
        for (int i = 0; i < n; i++)
        {
            Console.Write($"Nhap phan tu thu {i + 1}: ");
            day[i] = (T)Convert.ChangeType(Console.ReadLine(), typeof(T));
        }
        return day;
    }

    public static T[] inMangTraiSangPhai<T>(T[] day)
    {
        Console.Write("Day so la: ");
        for (int i = 0; i < day.Length; i++)
        {
            Console.Write($"{day[i]} ");
        }
        Console.WriteLine();
        return day;
    }
    
    public static void Main(string[] args)
    {
        SapTang.run();
        NoiRongThuHepDay.run();
        SapSoNguyenTo.run();
        XoaTrung.run();
    }
}