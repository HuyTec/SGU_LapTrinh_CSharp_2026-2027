using System;
using System.Globalization;

class timkiem{
    public static void run(){
        Console.Write("Moi ban nhap so luong phan tu: ");
        int n = int.Parse(Console.ReadLine());

        double[] a = new double[n];
        for (int i = 0; i < n; i++)
        {
            Console.Write("Phan tu " + i + ": ");
            a[i] = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
        }
        Console.Write("Moi ban nhap so can tim kiem: ");
        double x = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

        int viTriDau = -1;
        int solanxuathien = 0;
        for (int i = 0; i < n; i++)
        {
            if (a[i] == x)
            {
                if(viTriDau == -1)
                    viTriDau = i;
                solanxuathien++;
            }
        }

        int viTriCuoi = -1;
        for (int i = n - 1; i >= 0; i--)
        {
            if ( a[i] == x)
            {
                viTriCuoi = i;
                break;
            }
        }
        if (viTriDau == -1)
        {
            Console.WriteLine("Gia tri x khong ton tai");
        } else {
            Console.WriteLine("Gia tri " + x.ToString("F2", CultureInfo.InvariantCulture) + " xuat hien " + solanxuathien + " lan o vi tri dau tien " + viTriDau + " va cuoi cung " + viTriCuoi);;
        }
    }

}