public class XoaTrung {
    /*
     * Cho số nguyên n (n ≤ 500) và dãy n số nguyên ao, a1, a2, ..., an-1. Hãy xóa các phần tử trùng nhau
      trong dãy số và chỉ giữ lại duy nhất một phần tử.
     */

    public static int[] XoaTrungMang(int[] day)
    {
        int n = day.Length;
        int[] temp = new int[n];
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            bool isDuplicate = false;
            for (int k = 0; k < j; k++)
            {
                if (day[i] == temp[k])
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                temp[j++] = day[i];
            }
        }
        int[] result = new int[j];
        Array.Copy(temp, result, j);
        return result;
    }

    public static void run()
    {
        int n = 0;
        int[] day = new int[] { 1, 2, 3, 2, 4, 5, 3, 6, 1 };
        Console.WriteLine("Mang truoc khi xoa trung:");
        Program.inMangTraiSangPhai(day);
        day = XoaTrungMang(day);
        Console.WriteLine("Mang sau khi xoa trung:");
        Program.inMangTraiSangPhai(day);
    }
}
