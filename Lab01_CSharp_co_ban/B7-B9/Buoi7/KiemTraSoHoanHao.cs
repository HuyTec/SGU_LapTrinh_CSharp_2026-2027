using System;
class KiemTraSoHoanHao
    {
        public static void run()
        {
            Console.Write("nhap n :");
            int n;
            while(!int.TryParse(Console.ReadLine(),out n)||n<=0)
            {
                Console.Write("gia tri khong hop le");
            }
        

        int tong = 0;
        string dsUoc = "";
        for(int i=1; i < n ; i++){
            if(n%i==0){
                tong += i;
                dsUoc += (dsUoc==""? i.ToString():" + "+i);
            }
        }
        if (tong == n){
            Console.WriteLine(n + " = " + dsUoc + " la so hoan hao");
        }
        else
        {
            Console.WriteLine(n + " khong la so hoan hao");
        }
    
      }

}
