using System;

class RutGonPhanSo
{
    public static int tinhUCLN(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            int tam = b;
            b = a % b;
            a = tam;
        }
        return a;
    }

    public static void run()
    {
        Console.Write("Moi ban nhap tu so: ");
        int tuSo = int.Parse(Console.ReadLine());
        Console.Write("Moi ban nhap mau so: ");
        int mauSo = int.Parse(Console.ReadLine());
        int uocChung = tinhUCLN(tuSo, mauSo);
        int tuSoRutGon = tuSo / uocChung;
        int mauSoRutGon = mauSo / uocChung;
        Console.WriteLine("Phan so " + tuSo + "/" + mauSo + " duoc rut gon thanh " + tuSoRutGon + "/" + mauSoRutGon + ".");
    }
}