public class CotTongLonNhat
{
    /*
     // Nhập A
        Mang co 3 dong x 3 cot:
        1 11 9
        5 5 15
        3 16 8
        Cac cot co tong lon nhat: 1 2
     */

    public static (int[], int) TongLonNhat(int[,] a)
    {
        int m = a.GetLength(1);
        int maxTong = int.MinValue;
        int[] tongCot = new int[m];

        for (int j = 0; j < m; j++)
        {
            tongCot[j] = 0;
            for (int i = 0; i < a.GetLength(0); i++)
            {
                tongCot[j] += a[i, j];
            }
            if(tongCot[j] > maxTong)
            {
                maxTong = tongCot[j];
            }
        }

        return (tongCot, maxTong);
    }

    public static void TimCacCotTongLonNhat(int[,] a)
    {
        var (tongCot, maxTong) = TongLonNhat(a);
        Console.Write("Cac cot co tong lon nhat: ");
        for (int j = 0; j < a.GetLength(1); j++)
        {
            if (tongCot[j] == maxTong)
            {
                Console.Write($"{j} ");
            }
        }
        Console.WriteLine();
    }

    public static void run()
    {
        //int[,] a = Program.nhapMaTranSo<int>(ref n, ref m);
        int[,] a = new int[3, 3] { { 1, 11, 9 }, { 5, 5, 15 }, { 3, 16, 8 } };
        Program.inMaTranTraiSangPhai(a);
        TimCacCotTongLonNhat(a);
    }
}

