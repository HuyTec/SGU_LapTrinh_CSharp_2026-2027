//Cho dãy số thực có n phần tử (n ≤ 500) và số k cho trước. Hãy tìm các cặp trong dãy số có tổng bằng k.

//Day so co 5 phan tu: -2 5 - 3 9 - 6
//Moi ban nhap so k: 3
//- 2 + 5 = 3.
//9 + -6 = 3.
public static class TimCap
{
    public static int LietKeCap(double[] day, double k)
    {
        var hash = new HashSet<double>();
        bool coCap = false;

        for(int i = 0; i < day.Length; i++)
        {
            double complement = k - day[i];
            if (hash.Contains(complement))
            {
                coCap = true;
                Console.WriteLine("{0} + {1} = {2}.", complement, day[i], k);
            }
            hash.Add(day[i]);
        }
        return coCap ? 1 : 0;
    }

    public static void run()
    {
        // Khai báo mảng 1 chiều
        // double[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        double[] a = { -2, 5, -3, 9, -6 };
        double k;
        // nhapDaySo(ref a);
        Program.inMangTraiSangPhai(a);
        
        k = 6; // Chỉ định giá trị k để kiểm tra nhanh phương thức
        Console.Write("Voi k = {0}: ", k);
        if (LietKeCap(a, k) == 0)
        {
            Console.WriteLine("Day so khong co cap nao thoa man.");
        }
    }
}