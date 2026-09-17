using System;

class TimKiemGiaTriChan
{
    public static void run()
    {
        Console.Write("Moi ban nhap so luong phan tu: ");
        int n = int.Parse(Console.ReadLine());

        int[] a = new int[n];
        for (int i = 0; i < n; i++)
        {
            Console.Write("Phan tu " + i + ": ");
            a[i] = int.Parse(Console.ReadLine());
        }

        
        int viTriDau = -1;
        for (int i = 0; i < n; i++)
        {
            if (a[i] % 2 == 0&&a[i] > 0)
            {
                viTriDau = i;
                break;
            }
        }

        
        int viTriCuoi = -1;
        for (int i = n - 1; i >= 0; i--)
        {
            if (a[i] % 2 == 0)
            {
                viTriCuoi = i;
                break;
            }
        }

        if (viTriDau == -1)
        {
            Console.WriteLine("Day so khong co gia tri chan nao.");
        }
        else
        {
            Console.WriteLine("Gia tri chan dau tien o vi tri " + viTriDau + " co gia tri " + a[viTriDau] + ".");
            Console.WriteLine("Gia tri chan cuoi cung o vi tri " + viTriCuoi + " co gia tri " + a[viTriCuoi] + ".");
        }
    }
}