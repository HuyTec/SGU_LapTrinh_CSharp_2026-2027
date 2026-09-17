using System;

class demchuso{
    public static void run(){
        Console.Write("nhap so nguyen n : ");
        int n = int.Parse(Console.ReadLine());
        int[] dem = new int[10];
        int temp = Math.Abs(n);
        if (temp == 0 ){
            dem[0] = 1;
        }
        else 
        {
            while(temp>0){
                int chuso = temp%10;
                dem[chuso]++;
                temp/=10;
            }
        }
        for(int i = 0; i<=9;i++){
            if(dem[i]>0){
                Console.WriteLine("Chu so " + i + " xuat hien " + dem[i] + " lan.");
            }
        }
    }
}