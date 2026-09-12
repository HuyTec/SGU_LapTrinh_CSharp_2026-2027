public class MaTranToanLe
{
    /*  Cho ma trận A chứa các số nguyên có n dòng và m cột(1 ≤ n, m ≤ 100). Hãy kiểm tra xem ma
        trận có toàn lẻ không?
        // Nhập A
        Mang co 2 dong x 3 cot:
        1 7 9
        5 3 15
        Mang A toan le!
    */

    public static bool KiemTraToanLe(int[,] a)
    {
        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < a.GetLength(1); j++)
            {
                if (a[i, j] % 2 == 0)
                    return false;
            }
        }
        return true;
    }

    public static void run()
    {
        //int[,] a = Program.nhapMaTranSo<int>(ref n, ref m);
        int[,] a = new int[2, 3] { { 1, 7, 9 }, { 5, 3, 15 } };
        Program.inMaTranTraiSangPhai(a);
        if (KiemTraToanLe(a))
            Console.WriteLine("Mang A toan le!");
        else
            Console.WriteLine("Mang A khong toan le!");
    }
}