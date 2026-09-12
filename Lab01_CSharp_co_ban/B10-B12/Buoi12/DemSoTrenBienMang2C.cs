public class DemSoTrenBienMang2C
{
    /*  Cho ma trận A chứa các số thực có n dòng và m cột(1 ≤ n, m ≤ 100). Hãy viết chương trình đếm
    các số dương, âm, và không trên biên ma trận A.
    // Nhập ma trận A
    Mang co 3 dong x 4 cot:
    1.5 -3.2 4.8 0
    -1.8 3.9 2.7 4.5
    3.3 9.6 -2.8 -4.9
    Tren duong bien co 5 so duong, 3 so am va 1 so khong.
    */

    public static int DemSoTrenBien(double[,] a)
    {
        int demDuong = 0;
        int demAm = 0;
        int demKhong = 0;
        int n = a.GetLength(0);
        int m = a.GetLength(1);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                if (i == 0 || i == n - 1 || j == 0 || j == m - 1)
                {
                    if (a[i, j] > 0)
                        demDuong++;
                    else if (a[i, j] < 0)
                        demAm++;
                    else
                        demKhong++;
                }
            }
        }
        Console.WriteLine($"Tren duong bien co {demDuong} so duong, {demAm} so am va {demKhong} so khong.");
        return demDuong + demAm + demKhong;
    }
    public static void run()
    {
        //int[,] a = Program.nhapMaTranSo<int>(ref n, ref m);
        double[,] a = new double[3, 4] {{1.5, -3.2, 4.8, 0},
                                         {-1.8, 3.9, 2.7, 4.5},
                                         {3.3, 9.6, -2.8, -4.9}};
        Program.inMaTranTraiSangPhai(a);
        int soLuong = DemSoTrenBien(a);
    }
}