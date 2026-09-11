public class KeNhauTraiDau
{
    /*  Cho dãy số thực có n phần tử (n ≤ 500). Hãy tính tổng các phần tử cực trị có trong dãy số. Biết
        một phần tử được gọi là cực trị khi nó lớn hơn hoặc nhỏ hơn các phần tử xung quanh nó.

        Day so co 7 phan tu: -2 5 -3 -4 -6 2 1
        Tong cac phan tu cuc tri co trong day so: 7.
     */

    public int demCapKeNhauTraidau(int[] day)
    {
        int dem = 0;
        for (int i = 0; i < day.Length - 1; i++)
        {
            if (day[i] * day[i + 1] < 0)
            {
                dem = dem + 1;
            }
        }
        return dem;
    }

    public static void run()
    {
        // Khai báo mảng 1 chiều
        // int[] a = null;
        // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
        int[] a = { -2, 5, -3, -4, -6, 2, 1 };
        int kq = 0;
        // Xử lý
        KeNhauTraiDau keNhauTraiDau = new KeNhauTraiDau();
        kq = keNhauTraiDau.demCapKeNhauTraidau(a);
        // Kết quả
        Program.inMangTraiSangPhai(a);
        Console.WriteLine("So luong cap ke nhau ma trai dau: {0}.", kq);
    }
}
