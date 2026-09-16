using System;
using System.Globalization;
class Tinhsopi
{
    public static void run()
    {
        Console.Write("nhap do sai so ;");
        double epsilon;
        while(!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out epsilon)|| epsilon<=0)
        {
            Console.Write("gia tri khong hop le");
        }
        double pi = 0;
        int  n = 0;
        double sai = 4.0 / (2*n+1);

        while(sai >= epsilon){
            double dauso = (n%2==0)?1:-1;
            pi += dauso *(4.0/(2*n+1));
            n++;
            sai = 4.0 / (2*n+1);

        }
        Console.WriteLine("so pi " + epsilon.ToString(CultureInfo.InvariantCulture) + " la " + pi.ToString(CultureInfo.InvariantCulture));

    }

}