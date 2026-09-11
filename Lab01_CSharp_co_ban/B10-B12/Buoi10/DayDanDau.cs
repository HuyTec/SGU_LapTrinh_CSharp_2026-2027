public class DayDanDau
{
    /*
        Cho dãy số thực có n phần tử (n ≤ 500). Hãy xác định dãy có tính chất các phần tử kề nhau là
        trái dấu nhau hay không?

        Day so co 7 phan tu: -2 5 -3 4 -6 2 -1
        Day so tren la day dan dau.
     */

    public static bool laDanDau(double[] day)
    {
        bool coHieu = true;
        for (int i = 0; i < day.Length - 1; i++)
        {
            if (day[i] * day[i+1] >= 0)
            {
                coHieu = false;
                break;
            }
        }
        return coHieu;
    }

    public static void run()
    {
        // Khai báo mảng 1 chiều
        // double[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        double[] a = { -2, 5, -3, 4, -6, 2, -1 };
        bool kq;
        // Xử lý
        kq = laDanDau(a);
        // Kết quả
        Program.inMangTraiSangPhai(a);
        if (kq == true)
        {
            Console.WriteLine("Day so tren la day dan dau.");
        }
        else
        {
            Console.WriteLine("Day so tren khong la day dan dau.");
        }
    }

}
