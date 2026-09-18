using System;
namespace NMLT.Buoi04
{
    public class TamGiac
    {
        public static void Main(string[] args)
        {
            double a, b, c, tmp;
            bool laTamGiac;
            string[] t;
            Console.Write("Moi ban nhap 3 canh tam giac: ");
            t = Console.ReadLine().Split(
                new char[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            );
            a = double.Parse(t[0]);
            b = double.Parse(t[1]);
            c = double.Parse(t[2]);
            Console.Write(
                "Ba canh ({0}, {1}, {2}) vua nhap ",
                a, b, c
            );

            if (a > b)
            {
                tmp = a;
                a = b;
                b = tmp;
            }

            if (a > c)
            {
                tmp = a;
                a = c;
                c = tmp;
            }

            if (b > c)
            {
                tmp = b;
                b = c;
                c = tmp;
            }

            laTamGiac = (a + b > c);

            if (laTamGiac)
            {
                Console.WriteLine("tao thanh mot tam giac.");
            }
            else
            {
                Console.WriteLine("khong tao thanh mot tam giac.");
            }
        }
    }
}