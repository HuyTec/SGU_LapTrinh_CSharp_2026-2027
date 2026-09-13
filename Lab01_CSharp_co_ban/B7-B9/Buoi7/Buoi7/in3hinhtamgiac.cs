using System;

public class InHinhTamGiacVaVuong
{
    public void InHinh()
    {
        Console.Write("Moi ban nhap chieu cao h: ");
        int h = int.Parse(Console.ReadLine());

        Console.WriteLine("h = " + h);

        for (int i = 1; i <= h; i++)
        {
            int soSao;

            if (i == 1)
            {
                soSao = 1;
            }
            else if (i == h)
            {
                soSao = 2 * h - 1;
            }
            else
            {
                soSao = 2;
            }

            for (int j = 1; j <= soSao; j++)
            {
                Console.Write("* ");
            }

            Console.WriteLine();
        }
    }
}

public class Program
{
    public static void Main()
    {
        InHinhTamGiacVaVuong bai = new InHinhTamGiacVaVuong();
        bai.InHinh();
    }
}