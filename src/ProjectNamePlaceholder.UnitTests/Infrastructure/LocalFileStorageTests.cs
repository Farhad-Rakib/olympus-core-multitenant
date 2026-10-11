using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.Infrastructure.Storage;

namespace ProjectNamePlaceholder.UnitTests.Infrastructure;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"olympus-storage-{Guid.NewGuid():N}");

    private (LocalFileStorage Storage, CurrentTenantService Tenant) Create()
    {
        var tenantService = new CurrentTenantService();
        var options = Options.Create(new FileStorageOptions { RootPath = _root });
        return (new LocalFileStorage(options, tenantService, new StubHostEnvironment()), tenantService);
    }

    private static MemoryStream Png() => new([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02]);

    [Fact]
    public async Task Saved_files_are_partitioned_by_tenant()
    {
        var (storage, tenant) = Create();

        using (tenant.BeginScope(7, "acme"))
        {
            var stored = await storage.SaveAsync(Png(), "avatar.png");
            Assert.StartsWith("7/", stored.StorageKey);
        }
    }

    [Fact]
    public async Task The_client_filename_is_never_used_on_disk()
    {
        var (storage, tenant) = Create();

        using (tenant.BeginScope(1, "t"))
        {
            // A traversal attempt in the original name must not influence where the file lands.
            var stored = await storage.SaveAsync(Png(), "../../../etc/passwd.png");

            Assert.DoesNotContain("passwd", stored.StorageKey);
            Assert.DoesNotContain("..", stored.StorageKey);
            Assert.Matches(@"^1/[0-9a-f]{32}\.png$", stored.StorageKey);
        }
    }

    [Fact]
    public async Task Uploading_without_a_tenant_scope_is_refused()
    {
        var (storage, _) = Create();

        await Assert.ThrowsAsync<AppException>(() => storage.SaveAsync(Png(), "avatar.png"));
    }

    [Fact]
    public async Task A_disallowed_extension_is_refused()
    {
        var (storage, tenant) = Create();

        using (tenant.BeginScope(1, "t"))
        {
            await Assert.ThrowsAsync<AppException>(() => storage.SaveAsync(Png(), "payload.svg"));
        }
    }

    [Theory]
    [InlineData("1/../../../../etc/passwd")]
    [InlineData("../secrets.png")]
    [InlineData("1/not-a-guid.png")]
    [InlineData("1/../1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png")]
    public async Task Malformed_storage_keys_never_resolve(string key)
    {
        var (storage, _) = Create();

        Assert.Null(await storage.OpenReadAsync(key));
        Assert.False(await storage.DeleteAsync(key));
    }

    [Fact]
    public async Task A_saved_file_can_be_read_back_and_deleted()
    {
        var (storage, tenant) = Create();

        using (tenant.BeginScope(3, "t"))
        {
            var stored = await storage.SaveAsync(Png(), "a.png");

            await using (var read = await storage.OpenReadAsync(stored.StorageKey))
            {
                Assert.NotNull(read);
            }

            Assert.True(await storage.DeleteAsync(stored.StorageKey));
            Assert.Null(await storage.OpenReadAsync(stored.StorageKey));
        }
    }

    [Fact]
    public async Task DeleteAllForTenant_removes_only_that_tenants_files()
    {
        var (storage, tenant) = Create();

        string keptKey;
        using (tenant.BeginScope(1, "one"))
        {
            await storage.SaveAsync(Png(), "a.png");
            await storage.SaveAsync(Png(), "b.png");
        }

        using (tenant.BeginScope(2, "two"))
        {
            keptKey = (await storage.SaveAsync(Png(), "c.png")).StorageKey;
        }

        var removed = await storage.DeleteAllForTenantAsync(1);

        Assert.Equal(2, removed);
        Assert.False(Directory.Exists(Path.Combine(_root, "1")));

        // The other tenant is untouched -- the whole point of partitioning by tenant.
        await using var stillThere = await storage.OpenReadAsync(keptKey);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public async Task DeleteAllForTenant_is_a_no_op_for_a_tenant_with_no_files()
    {
        var (storage, _) = Create();

        Assert.Equal(0, await storage.DeleteAllForTenantAsync(999));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
