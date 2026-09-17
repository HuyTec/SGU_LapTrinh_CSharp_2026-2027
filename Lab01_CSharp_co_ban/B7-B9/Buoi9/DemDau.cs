using System;
using System.Globalization;
class demdau{
    public static void run(){
        Console.Write("nhap so luong phan tu : ");
        int n = int.Parse(Console.ReadLine());
        double[] a = new double[n];
        for(int i = 0 ;i<n ;i++){
            Console.Write("nhap phan tu "+i+":");
            a[i] = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture); 
        }
        int soduong = 0 , soam = 0, sokhong = 0;
        for(int i =0; i<n;i++){
            if(a[i]>0){
                soduong++;
            }
            else if (a[i]<0){
                soam++;
            }
            else
            sokhong++;
        }
        Console.WriteLine("day so co : "+soam+" so am ; "+soduong+" so duong ; "+sokhong+" so khong ");
    }
}