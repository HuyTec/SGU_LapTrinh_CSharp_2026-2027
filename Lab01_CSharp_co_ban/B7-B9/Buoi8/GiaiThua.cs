using System;

class Giaithua
{
    public static long tingGiaithua(int x){
        long ketqua=1;
        for (int i = 2; i<=x;i++){
            ketqua*=i;
        }
        return ketqua;

    }
    public static void run(){
        Console.Write("nhap k");
        int k = int.Parse(Console.ReadLine());
        Console.Write("nhap n");
        int n = int.Parse(Console.ReadLine());

        while(k>n){
            Console.WriteLine("k phai nho hon n");
            Console.Write("Moi ban nhap k: ");
            k = int.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap n: ");
            n = int.Parse(Console.ReadLine());
        }
        long tuso = tingGiaithua(n);
        long mauso = tingGiaithua(k)*tingGiaithua(n-k);
        long C = tuso/mauso;
        Console.WriteLine("C(" + k + ", " + n + ") = " + C + ".");
    }
}