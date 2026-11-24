namespace ShopFlow.Tests;

// Test đăng ký tạo/gỡ trigger trên Carts. Chạy các nhóm DB lần lượt để DDL không
// chặn các request đang chờ cổng đồng bộ; concurrency bên trong mỗi test vẫn giữ nguyên.
[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection;
