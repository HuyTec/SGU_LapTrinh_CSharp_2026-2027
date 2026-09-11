public class DayChanLe
    {
        // Cho dãy số nguyên có n phần tử(n ≤ 500). Hãy xác định dãy có tính chất đan xen các phần tử chẵn và lẻ không.

        //Day so co 7 phan tu: 2 5 6 3 4 7 2
        //Day so la day chan le.

        public static bool laChanLe(int[] day)
        {
            bool coHieu;
            coHieu = true;
                         
            for (int i = 0; i <= day.Length - 2 && coHieu == true; i++)
            {
                if ((day[i] % 2 == 0 && day[i + 1] % 2 == 0) || (day[i] % 2 == 1 && day[i + 1]
                % 2 == 1))
                {
                    coHieu = false;
                }
            }
            return coHieu;
        }
        public static void run()
        {
            // Khai báo mảng 1 chiều
            // int[] a = null;
            // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
            int[] a = { 2, 5, 6, 3, 4, 7, 2 };
            bool kq;
            // Xử lý
            kq = laChanLe(a);
            // Kết quả
            Program.inMangTraiSangPhai(a);
            if (kq == true)
            {
                Console.WriteLine("Day so la day chan le.");
            }
            else
            {
                Console.WriteLine("Day so khong la day chan le.");
            }
        }
    }