public class  KeNhauChan
    {
        //Cho dãy số nguyên có n phần tử(n ≤ 500). Hãy xác định số lượng các phần tử kề nhau mà cả hai đều là số chẵn.

        //Day so co 7 phan tu: 2 5 6 4 3 4 2
        //So luong cap ke nhau ma la so chan: 2.
        public static void demKeNhauChan(int[] day, ref int dem)
        {
            dem = 0;
        
            for (int i = 0; i <= day.Length - 2; i++)
            {
                if (day[i] % 2 == 0 && day[i + 1] % 2 == 0)
                {
                    dem = dem + 1;
                }
            }
        }
        public static void run()
        {
            // Khai báo mảng 1 chiều
            // int[] a = null;
            // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
            int[] a = { 2, 5, 6, 4, 3, 4, 2 };
            int kq = 0;
            // Xử lý
            demKeNhauChan(a, ref kq);
            // Kết quả
            Program.inMangTraiSangPhai(a);
            Console.WriteLine("So luong cap ke nhau ma la so chan: {0}.", kq);
        }
    }
