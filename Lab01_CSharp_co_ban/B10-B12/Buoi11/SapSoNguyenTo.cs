public class SapSoNguyenTo
{
    /** Cho số nguyên n (n ≤ 500) và dãy n số nguyên ao, a1, a2, ..., an-1. Hãy sắp xếp các số nguyên tố
        tăng dần, các phần tử khác vẫn giữa nguyên giá trị và vị trí.
        Ví dụ: Dãy trước khi sắp xếp: 5 6 4 7 8 12 2 8 3, và dãy sau khi sắp xếp: 2 6 4 3 8 12 5 8 7 */

    public static bool KiemTraNguyenTo(int n)
    {
        if (n < 2)
            return false;
        for (int i = 2; i <= Math.Sqrt(n); i++)
        {
            if (n % i == 0)
                return false;
        }
        return true;
    }

    public static void SapXepNguyenTo(ref int[] day)
    {
        int n = day.Length;
        int[] nguyenTo = new int[n];
        int count = 0;
        
        for (int i = 0; i < n; i++)
        {
            if (KiemTraNguyenTo(day[i]))
            {
                nguyenTo[count++] = day[i];
            }
        }
        
        Array.Sort(nguyenTo, 0, count);
        
        int index = 0;
        for (int i = 0; i < n; i++)
        {
            if (KiemTraNguyenTo(day[i]))
            {
                day[i] = nguyenTo[index++];
            }
        }
    }

    public static void run()
    {
        int n = 0;
        int[] day = new int[] { 5, 6, 4, 7, 8, 12, 2, 8, 3 };
        Console.WriteLine("Mang truoc khi sap xep:");
        Program.inMangTraiSangPhai(day);
        SapXepNguyenTo(ref day);
        Console.WriteLine("Mang sau khi sap xep:");
        Program.inMangTraiSangPhai(day);
    }
}
