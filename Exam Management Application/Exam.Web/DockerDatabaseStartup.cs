using Microsoft.Data.SqlClient;

namespace Exam.Web;

/// <summary>Require the Docker SQL endpoint and verify the database before accepting requests.</summary>
public sealed class DockerDatabaseStartup(IConfiguration configuration) : IHostedService
{
    public static SqlConnectionStringBuilder Connection(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        if (!new[] { "tcp:127.0.0.1,14333", "127.0.0.1,14333", "tcp:localhost,14333", "localhost,14333" }
            .Contains(connection.DataSource, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Ứng dụng yêu cầu SQL Server Docker tại tcp:127.0.0.1,14333. " +
                "Chạy scripts/Initialize-Local.ps1 để cấu hình; không dùng SQLEXPRESS.");
        if (string.IsNullOrWhiteSpace(connection.InitialCatalog))
            throw new InvalidOperationException("Chưa chọn database Docker. Chạy scripts/Initialize-Local.ps1.");
        return connection;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = Connection(configuration.GetConnectionString("ExamDb")
            ?? throw new InvalidOperationException("Chạy scripts/Initialize-Local.ps1 trước khi mở Web."));
        settings.ConnectTimeout = 5;
        settings.ConnectRetryCount = 0;
        await using var connection = new SqlConnection(settings.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (SqlException)
        {
            throw new InvalidOperationException(
                "Không kết nối được database SQL Server Docker. Mở Docker Desktop, chạy " +
                "scripts/Start-SqlDocker.ps1 rồi mở lại Web. Nếu chưa khởi tạo database, " +
                "chạy scripts/Initialize-Local.ps1. Kiểm tra lại cấu hình đăng nhập nếu SQL đã chạy.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
