public class SapTang
{
    /*
        Cho số nguyên n (n ≤ 500) và dãy n số thực ao, a1, a2, ..., an-1. Hãy sắp xếp dãy số thành dãy tăng
        dần.
        
        * Mang truoc khi sap xep:
        Day so co 8 phan tu: 2 4 3 4 2 8 2 3
        * Mang sau khi sap xep:
        Day so co 8 phan tu: 2 2 2 3 3 4 4 8
    */

    public static void CocktailSort(double[] day)
    {
        bool swapped = true;
        int start = 0;
        int end = day.Length - 1;
        while (swapped) 
        {
            swapped = false;
            for(int i = start; i < end; ++i)
            {
                if (day[i] > day[i + 1])
                {
                    double temp = day[i];
                    day[i] = day[i + 1];
                    day[i + 1] = temp;
                    swapped = true;
                }
            }
            if(!swapped)
                {
                break;
            }
            swapped = false;
            end--;
            for (int i = end - 1; i >= start; --i)
            {
                if (day[i] > day[i + 1])
                {
                    double temp = day[i];
                    day[i] = day[i + 1];
                    day[i + 1] = temp;
                    swapped = true;
                }
            }
            start++;
        }
    }

    public static void run()
    {
        int n = 0;
        double[] day = new double[] { 2, 4, 3, 4, 2, 8, 2, 3 };
        Console.WriteLine("Mang truoc khi sap xep:");
        Program.inMangTraiSangPhai(day);
        CocktailSort(day);
        Console.WriteLine("Mang sau khi sap xep:");
        Program.inMangTraiSangPhai(day);
    }
}
