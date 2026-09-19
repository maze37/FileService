using Amazon.S3;
using FileService.Core.Abstractions;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace FileService.IntegrationTests.Infrastructure;

public class IntegrationTestsWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("file_service_db")
        .WithUsername("postgres")
        .WithPassword("1234")
        .Build();
    
    private readonly MinioContainer _minioContainer = new MinioBuilder()
        .WithImage("minio/minio")
        .WithUsername("minioadmin")
        .WithPassword("minioadmin")
        .Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FileServiceDb"] = _dbContainer.GetConnectionString(),
                ["S3Options:AccessKey"] = "minioadmin",
                ["S3Options:SecretKey"] = "minioadmin"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.RemoveAll<IAmazonS3>();

            services.AddSingleton<IAmazonS3>(sp =>
            {
                S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
                ushort minioPort = _minioContainer.GetMappedPublicPort(9000);

                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{_minioContainer.Hostname}:{minioPort}",
                    UseHttp = true,
                    ForcePathStyle = true,
                };

                return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
            });

            services.RemoveAllKeyed<IAmazonS3>(S3ClientKeys.PRESIGN);

            services.AddKeyedSingleton<IAmazonS3>(S3ClientKeys.PRESIGN, (sp, _) =>
            {
                S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
                ushort minioPort = _minioContainer.GetMappedPublicPort(9000);

                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{_minioContainer.Hostname}:{minioPort}",
                    UseHttp = true,
                    ForcePathStyle = true,
                };

                return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
            });

            services.AddHostedService<S3BucketInitializer>();
            
            services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _minioContainer.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        
        await _minioContainer.StopAsync();
        await _minioContainer.DisposeAsync();
    }
}