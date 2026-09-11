public class TongCucTri
    {
        //Cho dãy số thực có n phần tử (n ≤ 500). Hãy tính tổng các phần tử cực trị có trong dãy số. Biết
        //một phần tử được gọi là cực trị khi nó lớn hơn hoặc nhỏ hơn các phần tử xung quanh nó.

        //Day so co 5 phan tu: -2.3 5.4 - 2.9 9.7 - 3.8
        //Tong cac phan tu cuc tri trong day so: 12.2.

        public static double tongCucTri(double[] day)
        {
            double tongCucTri = 0;
            for (int i = 0; i < day.Length; i++)
            {
                if(i==0)
                {
                    continue;
                }
                else if (i == day.Length - 1)
                {
                    if (day[i] > day[i - 1] || day[i] < day[i - 1])
                    {
                        continue;
                    }
                }
                else
                {
                    if ((day[i] >= day[i - 1] && day[i] >= day[i + 1]) || (day[i] <= day[i - 1] && day[i] <= day[i + 1]))
                    {
                        tongCucTri = tongCucTri + day[i];
                    }
                }
            }
            return tongCucTri;
        }

        public static void run()
        {
            // Khai báo mảng 1 chiều
            // double[] a = null;
            // Khởi tạo sẵn giá trị dãy số để kiểm tra nhanh phương thức
            double[] a = { -2.3, 5.4, -2.9, 9.7, -3.8 };
            double kq;
            kq = tongCucTri(a);
            // nhapDaySo(ref a);
            Program.inMangTraiSangPhai(a);
            Console.WriteLine("Tong cac phan tu cuc tri trong day so: {0}.", kq);
        }
    }
