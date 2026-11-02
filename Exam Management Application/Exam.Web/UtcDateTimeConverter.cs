using System.Text.Json;
using System.Text.Json.Serialization;
namespace Exam.Web;
public class UtcDateTimeConverter:JsonConverter<DateTime>{public override DateTime Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)=>DateTime.Parse(r.GetString()!,null,System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();public override void Write(Utf8JsonWriter w,DateTime d,JsonSerializerOptions o)=>w.WriteStringValue(DateTime.SpecifyKind(d,DateTimeKind.Utc));}
