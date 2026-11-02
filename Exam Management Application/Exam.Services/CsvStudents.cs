using System.Data;
using System.Text;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;
namespace Exam.Services;
public static class CsvStudents
{
    /// <summary>Parse RFC 4180 fields, including escaped quotes and embedded line breaks.</summary>
    public static List<string[]> Parse(string csv)
    {
        if(csv.Length>1_000_000)throw new BusinessException("Tệp CSV tối đa 1 MB.");
        var rows=new List<string[]>();var row=new List<string>();var field=new StringBuilder();bool quoted=false,closed=false;
        void Cell(){row.Add(field.ToString());field.Clear();closed=false;}
        void Row(){Cell();if(row.Any(s=>s.Length>0))rows.Add(row.ToArray());row.Clear();}
        for(int i=0;i<csv.Length;i++){
            var c=csv[i];
            if(quoted){if(c=='"'){if(i+1<csv.Length&&csv[i+1]=='"'){field.Append('"');i++;}else{quoted=false;closed=true;}}else field.Append(c);continue;}
            if(c=='"'){if(field.Length!=0||closed)throw new BusinessException("CSV có dấu ngoặc kép không hợp lệ.");quoted=true;continue;}
            if(c==','){Cell();continue;}
            if(c=='\r'||c=='\n'){if(c=='\r'&&i+1<csv.Length&&csv[i+1]=='\n')i++;Row();continue;}
            if(closed){if(!char.IsWhiteSpace(c))throw new BusinessException("CSV có dữ liệu sau dấu ngoặc kép đóng.");continue;}
            field.Append(c);
        }
        if(quoted)throw new BusinessException("CSV thiếu dấu ngoặc kép đóng.");
        if(field.Length>0||row.Count>0||closed)Row();
        return rows;
    }
}

