public class DongTangDan
{
   /*   Cho ma trận A chứa các số nguyên có n dòng và m cột(1 ≤ n, m ≤ 100). Hãy liệt kê các dòng
        mà các giá trị tạo thành dãy tăng.
        // Nhập A
        Mang co 3 dong x 3 cot:
        1 7 9
        5 3 15
        3 4 8
        Cac dong tao thanh day tang: 0 2
    */

    public static bool KiemTraDayTang(int[] day)
    {
        for (int i = 0; i < day.Length - 1; i++)
        {
            if (day[i] >= day[i + 1])
                return false;
        }
        return true;
    }

    public static void LietKeDongTangDan(int[,] a)
    {
        Console.Write("Cac dong tao thanh day tang: ");
        for (int i = 0; i < a.GetLength(0); i++)
        {
            int[] day = new int[a.GetLength(1)];
            for (int j = 0; j < a.GetLength(1); j++)
            {
                day[j] = a[i, j];
            }
            if (KiemTraDayTang(day))
                Console.Write($"{i} ");
        }
        Console.WriteLine();
    }

    public static void run()
    {
        //int[,] a = Program.nhapMaTranSo<int>(ref n, ref m);
        int[,] a = new int[3, 3] { { 1, 7, 9 }, { 5, 3, 15 }, { 3, 4, 8 } };
        Program.inMaTranTraiSangPhai(a);
        LietKeDongTangDan(a);
    }
}