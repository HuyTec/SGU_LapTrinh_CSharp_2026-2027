
using System;
using System.Globalization;
public static class Program
{
    public static T[] nhapMangSo<T>(ref int n)
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
        Console.Write("Day so nhap vao: ");
        for (int i = 0; i < day.Length; i++)
        {
            Console.Write($"{day[i]} ");
        }
        Console.WriteLine();
        return day;
    }

    public static T[,] nhapMaTranSo<T>(ref int n, ref int m)
    {
        Console.WriteLine("Nhap so dong (n <= 100):");
        do
        {
            n = int.Parse(Console.ReadLine());
            if (n <= 0 || n > 100)
                Console.WriteLine("So dong phai trong khoang (0, 100]. Nhap lai:");
        } while (n <= 0 || n > 100);

        Console.WriteLine("Nhap so cot (m <= 100):");
        do
        {
            m = int.Parse(Console.ReadLine());
            if (m <= 0 || m > 100)
                Console.WriteLine("So cot phai trong khoang (0, 100]. Nhap lai:");
        } while (m <= 0 || m > 100);

        T[,] maTran = new T[n, m];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                Console.Write($"Nhap phan tu tai vi tri [{i}, {j}]: ");
                maTran[i, j] = (T)Convert.ChangeType(Console.ReadLine(), typeof(T));
            }
        }
        return maTran;
    }

    public static void inMaTranTraiSangPhai<T>(T[,] maTran)
    {
        Console.WriteLine("Ma tran nhap vao:");
        for (int i = 0; i < maTran.GetLength(0); i++)
        {
            for (int j = 0; j < maTran.GetLength(1); j++)
            {
                Console.Write($"{maTran[i, j]} ");
            }
            Console.WriteLine();
        }
    }

    public static void Main(string[] args)
    {
        TongChanLeMang2C.run();
        DemSoTrenBienMang2C.run();
        MaTranToanLe.run();
        DongTangDan.run();
        CotTongLonNhat.run();
    }
}