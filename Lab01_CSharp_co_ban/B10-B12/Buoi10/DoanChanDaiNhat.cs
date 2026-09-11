 public class DoanChanDaiNhat
 {
    //Cho dãy số nguyên có n phần tử (n ≤ 500). Hãy xác định đoạn có chứa toàn số chẵn dài nhất có
    //trong dãy số nguyên.

    //Day so co 8 phan tu: 2 4 3 4 2 8 2 3
    //Day cac so chan dai nhat co 4 phan tu: 4 2 8 2

    public static void inDayCon(int[] day, int from, int to)
    {
        for (int i = from; i <= to; i++)
        {
            Console.Write(" {0}", day[i]);
        }
    }
    public static bool timDoanChanDaiNhat(int[] day, ref int fromMax, ref int toMax)
    {
        bool chuoi;
        int curFrom = 0, curTo = 0;
        chuoi = false;
        fromMax = -1; toMax = 0; // (toMax - fromMax + 1 --> 0)
                                 // duyệt các phần tử trong dãy
        for (int i = 0; i <= day.Length - 1; i++)
        {
            // Đầu đoạn chẵn (trước là lẻ và hiện tại là chẵn)
            if ((i == 0 && day[i] % 2 == 0) ||
            (i > 0 && day[i - 1] % 2 == 1 && day[i] % 2 == 0))
            {
                chuoi = true;
                curFrom = i;
            }
            // Cuối đoạn chẵn (hiện tại là chẵn, sau là lẻ)
            if ((i == day.Length - 1 && day[i] % 2 == 0) ||
            (i != day.Length - 1 && day[i + 1] % 2 == 1 && day[i] % 2 == 0))
            {
                curTo = i;
                if (curTo - curFrom + 1 > toMax - fromMax + 1)
                {
                    fromMax = curFrom;
                    toMax = curTo;     
                }
            }
        }
        return chuoi;
    }
    public static void run()
    {
        // Khai báo mảng 1 chiều
        // int[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        int[] a = { 2, 4, 3, 4, 2, 8, 2, 3 };
        int i = 0, j = 0;
        bool kq;
        // Xử lý
        // Kết quả
        Program.inMangTraiSangPhai(a);
        kq = timDoanChanDaiNhat(a, ref i, ref j);
        if (kq == false)
        {
            Console.WriteLine("Day khong co doan chan.");
        }
        else
        {
            Console.Write("Day cac so chan dai nhat co {0} phan tu:", j - i + 1);
            inDayCon(a, i, j);
            Console.WriteLine();
        }
    }
}

