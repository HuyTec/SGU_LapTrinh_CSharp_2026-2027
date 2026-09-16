using System;

namespace NMLT.Buoi7
{
    class in3tamgiacvuong
    {
        public static void inhinhtamgiacvuong(int h)
        {
            for(int i = 1; i <= h; i++)
            {
                int leading = 2 * (h - i);
                int rowlen = 4 * i - 3;

                string row;
                if(i==h)
                {
                    row = "";
                    for(int j = 1; j <= 2*i-1;j++)
                        row += (j ==1 ? "*" :" *");
                }
                else if(i==1)
                {
                    row = "*";
                }
                else
                {
                    row = "*" + new string(' ',rowlen -2) + "*";
                }
                Console.WriteLine(new string(' ',leading)+row);

            }
        }
        public static void Main(String[] args)
        {
            Console.Write("nhap chieu cao h :");
            int h;

            while(!int.TryParse(Console.ReadLine(),out h))
            {
                Console.Write("gia tri khong hop le");
            }

            for(int k=0; k<3; k++)
            {
                int hh = h + 2 * k;
                Console.WriteLine("h=" +  hh);
                inhinhtamgiacvuong(hh);
                Console.WriteLine();
            }
        }
    }
}