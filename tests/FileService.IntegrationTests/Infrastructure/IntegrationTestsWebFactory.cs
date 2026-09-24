using System.Data.Common;
using Amazon.S3;
using Amazon.S3.Model;
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
using Npgsql;
using Respawn;
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
    
    private Respawner _respawner = null!;
    private DbConnection _dbConnection = null!;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FileServiceDb"] = _dbContainer.GetConnectionString(),
                ["FileStorageOptions:AccessKey"] = "minioadmin",
                ["FileStorageOptions:SecretKey"] = "minioadmin"
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
                FileStorageOptions fileStorageOptions = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                ushort minioPort = _minioContainer.GetMappedPublicPort(9000);

                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{_minioContainer.Hostname}:{minioPort}",
                    UseHttp = true,
                    ForcePathStyle = true,
                };

                return new AmazonS3Client(fileStorageOptions.AccessKey, fileStorageOptions.SecretKey, config);
            });

            services.RemoveAllKeyed<IAmazonS3>(S3ClientKeys.PRESIGN);
            services.AddKeyedSingleton<IAmazonS3>(S3ClientKeys.PRESIGN, (sp, _) =>
            {
                FileStorageOptions fileStorageOptions = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                ushort minioPort = _minioContainer.GetMappedPublicPort(9000);

                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{_minioContainer.Hostname}:{minioPort}",
                    UseHttp = true,
                    ForcePathStyle = true,
                };

                return new AmazonS3Client(fileStorageOptions.AccessKey, fileStorageOptions.SecretKey, config);
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

        await dbContext.Database.MigrateAsync();
        
        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        
        await InitializeRespawner();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        
        await _minioContainer.StopAsync();
        await _minioContainer.DisposeAsync();
        
        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();
    }
    
    private async Task InitializeRespawner()
    {
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions
            { 
                DbAdapter = DbAdapter.Postgres, 
                SchemasToInclude = ["files"]
            });
    }
    
    /// <summary>
    /// Полный сброс состояния между тестами: БД и содержимое бакетов.
    /// </summary>
    public async Task ResetAsync()
    {
        await ResetDatabaseAsync();
        await ResetStorageAsync();
    }
 
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection!);
    }
 
    /// <summary>
    /// Очищает объекты и незавершённые multipart-загрузки. Сами бакеты остаются.
    /// </summary>
    public async Task ResetStorageAsync()
    {
        var s3 = Services.GetRequiredService<IAmazonS3>();
        var options = Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
 
        foreach (string bucket in options.RequiredBuckets)
        {
            // 1. Незавершённые multipart-загрузки (ListObjects их не показывает)
            var uploads = await s3.ListMultipartUploadsAsync(bucket);
            foreach (MultipartUpload upload in uploads.MultipartUploads ?? [])
            {
                await s3.AbortMultipartUploadAsync(bucket, upload.Key, upload.UploadId);
            }
 
            // 2. Объекты (страницами по 1000, лимит DeleteObjects)
            var request = new ListObjectsV2Request { BucketName = bucket };
            ListObjectsV2Response page;
            do
            {
                page = await s3.ListObjectsV2Async(request);
 
                if (page.S3Objects is { Count: > 0 })
                {
                    await s3.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucket,
                        Objects = page.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList(),
                    });
                }
 
                request.ContinuationToken = page.NextContinuationToken;
            } while (page.IsTruncated == true);
        }
    }
}