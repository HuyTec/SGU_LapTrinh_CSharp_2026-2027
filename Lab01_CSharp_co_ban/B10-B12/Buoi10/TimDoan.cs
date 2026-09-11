using System;
using System.Collections.Generic;
public class TimDoan
{
    /*
        Cho dãy số thực có n phần tử (n ≤ 500) và số k cho trước. Hãy tìm đoạn [i, j] dài nhất trong dãy
        số có tổng bằng k (i ≤ j).
      
        Day so co 7 phan tu: -2 5 -3 4 -6 2 -1
        Moi ban nhap so k: 0
        Doan [0, 5] dai nhat co tong bang 0: -2 5 -3 4 -6 2
     */
    private const double SCALE = 10000.0;

    private static long ScaleToLong(double value)
    {
        return (long)(value * SCALE);
    }
    public static void TimDoanDaiNhat(double[] day, int k)
    {
        //Chuyển dãy thành long tránh dấu phẩy động làm sai lệch kết quả
        var firstIndex = new Dictionary<long, int>();
        firstIndex[0L] = -1;

        double prefixSum = 0;
        int maxLength = 0, start = -1, end = -1;

        for(int j = 0; j < day.Length; j++) 
        {
            prefixSum += day[j];

            long candidatKey = ScaleToLong(prefixSum - k);

            if (firstIndex.ContainsKey(candidatKey))
            {
                int i  =    firstIndex[candidatKey];
                int len = j - i;
                if (len > maxLength) 
                {
                    maxLength = len;
                    start = i + 1;
                    end = j;
                }
            }
            long tongKey = ScaleToLong(prefixSum);
            if(!firstIndex.ContainsKey(tongKey))
            {
                firstIndex[tongKey] = j;
            }
        }
        if(maxLength > 0)    {
            Console.WriteLine($"Doan [{start}, {end}] dai nhat co tong bang {k}: ");
            for(int i = start; i <= end; i++)
            {
                Console.Write($"{day[i]} ");
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("Khong tim thay doan co tong bang k.");
        }
    }

    public static void run()
    {
        // Khai báo mảng 1 chiều
        // double[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        double[] a = { -2, 5, -3, 4, -6, 2, -1 };
        int k = 0; // Chỉ định giá trị k để kiểm tra nhanh phương thức
        Program.inMangTraiSangPhai(a);
        Console.WriteLine($"Voi k = {k}: ");
        TimDoanDaiNhat(a, k);
    }
}