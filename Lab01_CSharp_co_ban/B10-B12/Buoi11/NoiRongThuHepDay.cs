public class NoiRongThuHepDay
{

    /*
        Day so co 5 phan tu: 225.24 325.50 340.22 381.09 217.91
        * Mang ban dau:
        Day so co 10 phan tu: 225.24 325.50 340.22 381.09 217.91
                0.00 0.00 0.00 0.00 0.00
        * Mang sau khi them 5 phan tu:
        Day so co 10 phan tu: 225.24 325.50 340.22 381.09 217.91
                0.71 0.41 0.86 0.82 0.09
        * Mang sau khi thu hep 2 phan tu:
        Day so co 8 phan tu: 225.24 325.50 340.22 381.09 217.91
                0.71 0.41 0.86
    */
    public static double[] sinhNgauNhienDaySo(int n, int vmin, int vmax)
    {
        int i;
        double[] day = null;
  
        day = new double[n];
        Random rand = new Random();
        for (i = 0; i <= day.Length - 1; i++)
        {
            day[i] = rand.NextDouble() * (vmax - vmin) + vmin;
        }
        return day;
    }
    public static double[] noiRongDay(double[] day, int n)
    {
        // Cấp phát dãy mới có day.Length + n phần tử
        double[] dayMoi = new double[day.Length + n];
        for (int i = 0; i <= day.Length - 1; i++)
        {
            dayMoi[i] = day[i];
        }
        return dayMoi;
    }
    public static void ChepDay(double[] day, int from, params double[] aThem)
    {
        for (int i = 0; i <= aThem.Length - 1 && i + from <= day.Length - 1; i++)
        {
            day[i + from] = aThem[i];
        }
    }
    public static double[] thuHepDay(double[] day, int n)
    {
        // Cấp phát dãy mới có day.Length - n phần tử
        double[] dayMoi = new double[day.Length - n];
        // Sao chép các phần tử sang dãy mới
        for (int i = 0; i <= dayMoi.Length - 1; i++)
        {
            dayMoi[i] = day[i];
        }
        return dayMoi;
    }
    public static void run()
    {
        // Khai báo mảng 1 chiều
        // double[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        double[] a = { 225.24, 325.50, 340.22, 381.09, 217.91 };
        int cuoiMang;
        // Xử lý
        // a = sinhNgauNhienDaySo(5, 100, 500);
        Console.WriteLine("* Mang ban dau: ");
        Program.inMangTraiSangPhai(a);
        cuoiMang = a.Length;// Luu vi tri cuoi, can them
                            // Noi rong mang 5 phan tu
        a = noiRongDay(a, 5);
        Console.WriteLine("* Mang ban dau: ");
        Program.inMangTraiSangPhai(a);
        // Them 5 phan tu ngau nhien [0, 1] vao cuoi mang
        Random rand = new Random();
        ChepDay(a, cuoiMang, rand.NextDouble(), rand.NextDouble(), rand.NextDouble(),
        rand.NextDouble(), rand.NextDouble());
        Console.WriteLine("* Mang sau khi them 5 phan tu: ");
        Program.inMangTraiSangPhai(a);

        // Thu hep 2 phan tu
        a = thuHepDay(a, 2);
        Console.WriteLine("* Mang sau khi thu hep 2 phan tu: ");
        Program.inMangTraiSangPhai(a);
    }
}