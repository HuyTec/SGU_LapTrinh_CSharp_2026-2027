using System.Text;
using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("ExamDb") ?? throw new InvalidOperationException("Set ConnectionStrings:ExamDb using user-secrets or environment variables.");
var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Set Jwt:Key using user-secrets or environment variables.");
if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
builder.Services.AddScoped<StudentClassService>();
builder.Services.AddScoped<LecturerClassService>();
builder.Services.AddScoped<LecturerSessionService>();
builder.Services.AddScoped<QuestionExcelImportService>();
builder.Services.AddDbContext<ExamDbContext>(o => o.UseSqlServer(connection, s => s.EnableRetryOnFailure()));
builder.Services.AddIdentityCore<AppUser>(o => { o.Password.RequiredLength = 8; o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10); o.User.RequireUniqueEmail = false; }).AddRoles<AppRole>().AddEntityFrameworkStores<ExamDbContext>().AddDefaultTokenProviders();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ExamService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<LecturerSessionService>();
builder.Services.AddScoped<LecturerClassService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/AccessDenied"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; o.Events.OnValidatePrincipal = async c => { if (!await c.HttpContext.RequestServices.GetRequiredService<AuthService>().ValidAsync(c.Principal!, desktop: false)) c.RejectPrincipal(); }; }).AddJwtBearer(o => { o.MapInboundClaims = false; o.TokenValidationParameters = new() { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Exam.Web", ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Exam.Desktop", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(5) }; o.Events = new JwtBearerEvents { OnTokenValidated = async c => { if (!await c.HttpContext.RequestServices.GetRequiredService<AuthService>().ValidAsync(c.Principal!, desktop: true)) c.Fail("Session invalid"); } }; });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new Exam.Web.UtcDateTimeConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<Exam.Web.DockerDatabaseStartup>();
builder.Services.AddHostedService<ExpiryWorker>();
var app = builder.Build();
app.Use(async (ctx, next) => { try { await next(); } catch (BusinessException ex) { ctx.Response.StatusCode = ex.Status; await Results.Problem(statusCode: ex.Status, title: "Không thể thực hiện yêu cầu", detail: ex.Message).ExecuteAsync(ctx); } catch (DbUpdateException) { ctx.Response.StatusCode = 409; await Results.Problem(statusCode: 409, title: "Dữ liệu bị tham chiếu hoặc xung đột. Vui lòng tải lại và thử lại.").ExecuteAsync(ctx); } catch (Exception ex) { app.Logger.LogError(ex, "Request {RequestId} failed", ctx.TraceIdentifier); ctx.Response.StatusCode = 500; await Results.Problem(statusCode: 500, title: "Có lỗi xử lý. Vui lòng thử lại.", extensions: new Dictionary<string, object?> { { "requestId", ctx.TraceIdentifier } }).ExecuteAsync(ctx); } });
app.Use(async (context, next) =>
{
    await next();
    if (context.Request.Method != "GET" && context.Response.StatusCode < 400)
        app.Logger.LogInformation("Request {RequestId}: {Method} {Path}; user {UserId}; HTTP {StatusCode}",
            context.TraceIdentifier, context.Request.Method, context.Request.Path.Value,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous", context.Response.StatusCode);
});
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("lecturer-class-alias", "ClassLecturer/{action=Index}/{id?}", new { controller = "LecturerClasses" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
if (args.Contains("--seed")) { if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Seed is only allowed in Development."); await DemoSeeder.SeedAsync(app.Services, builder.Configuration["Seed:Password"] ?? ""); return; }
await app.RunAsync();
public partial class Program
{
}
public class ExpiryWorker(IServiceScopeFactory scopes, ILogger<ExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var count = await scope.ServiceProvider.GetRequiredService<ExamService>().ExpireAsync(cancellationToken: stoppingToken);
                if (count > 0)
                    logger.LogInformation("Expired grading batch {RequestId}: {Count} attempts completed", "worker-" + Guid.NewGuid().ToString("N"), count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Timeout grading retry will run in 5 seconds."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

