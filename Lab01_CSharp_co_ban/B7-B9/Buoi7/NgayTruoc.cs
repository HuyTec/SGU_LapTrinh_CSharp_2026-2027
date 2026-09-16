using System;

class Ngaytruoc
{
    public static bool laNamNhuan(int nam)
    {
        return (nam%4==0&& nam%100!=0)||(nam%400==0);
    }
    public static int songaycuathang(int thang,int nam){
        int[] songay = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        if(thang == 2 && laNamNhuan(nam))
            return 29;
        return songay[thang -1];
    }

    public static void run(){
        Console.Write("nhap ngay");
        int ngay = int.Parse(Console.ReadLine());
        Console.Write("nhap thang");
        int thang = int.Parse(Console.ReadLine());
        Console.Write("nhap nam");
        int nam = int.Parse(Console.ReadLine());
        int ngaytruoc,thangtruoc,namtruoc;
        if (ngay>1){
            ngaytruoc=ngay-1;
            thangtruoc=thang;
            namtruoc=nam;
        }
        else
        {
            if(thang >1)
            {
                thangtruoc = thang -1;
                namtruoc= nam;
            }
            else
            {
                thangtruoc=12;
                namtruoc=nam-1;
            }
            ngaytruoc=songaycuathang(thangtruoc,namtruoc);

        }
        Console.WriteLine( " ngaytruoc "+ngay+"/"+thang+"/"+nam+" la "+ngaytruoc+"/"+thangtruoc+"/"+namtruoc);
        }
}