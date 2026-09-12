public class TongChanLeMang2C
{
    /*  Cho ma trận A chứa các số nguyên có n dòng và m cột(1 ≤ n, m ≤ 100). Hãy viết chương trình
        tính tổng các số chẵn, số lẻ có trong ma trận A.

        // Nhập A
        Mang co 2 dong x 3 cot:
        2 4 5
        8 10 15
        Tong cac so chan la 24 va tong so le la 20.
    */

    public static void TinhTongChanLe(int[,] a)
    {
        int tongChan = 0;
        int tongLe = 0;

        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < a.GetLength(1); j++)
            {
                if (a[i, j] % 2 == 0)
                    tongChan += a[i, j];
                else
                    tongLe += a[i, j];
            }
        }

        Console.WriteLine($"Tong cac so chan la {tongChan} va tong so le la {tongLe}.");
    }

    public static void run()
    {
        //int[,] a = Program.nhapMaTranSo<int>(ref n, ref m);

        int[,] a = new int[2, 3] {{2, 4, 5}, {8, 10, 15}};
        Program.inMaTranTraiSangPhai(a);
        TinhTongChanLe(a);
    }
}
