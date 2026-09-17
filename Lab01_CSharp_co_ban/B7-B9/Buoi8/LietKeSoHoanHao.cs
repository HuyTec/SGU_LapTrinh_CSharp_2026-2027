using System;

class LietKeSoHoanHao{
    public static bool lasohoanhao(int so){
        int tong = 0;
        for(int i =1 ; i<so ;i++){
            if (so % i == 0)
            tong +=i;
        }
        return tong == so;

    }
    public static void run(){
        Console.Write("nhap so n : ");
        int n = int.Parse(Console.ReadLine());

        System.Collections.Generic.List<int> dssohoanhao = new System.Collections.Generic.List<int>();
        for( int so =1 ; so <= n;so++){
            if(lasohoanhao(so))
                dssohoanhao.Add(so);
        }
        Console.Write("cac so hoan hao trong doan [1, " + n + "]:");
        for(int i = dssohoanhao.Count -1 ;i>=0;i--){
            Console.Write(dssohoanhao[i]);
            if(i>0)
                Console.Write(",");
        }
        Console.WriteLine();
    }
}