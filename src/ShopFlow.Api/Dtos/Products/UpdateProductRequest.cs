using System.ComponentModel.DataAnnotations;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Products;

public class UpdateProductRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [EnumDataType(typeof(SwitchType))]
    public SwitchType SwitchType { get; set; } = SwitchType.Unknown;

    // Request nhận từng loại kết nối; service sẽ gộp thành flags của entity.
    [Required]
    [MinLength(1)]
    [MaxLength(3)]
    public List<Connectivity> Connectivity { get; set; } = new();

    [Required]
    [MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    // null = chưa có thông tin; false = không hỗ trợ thay switch nóng.
    public bool? IsHotSwappable { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "1000000000")]
    public decimal Price { get; set; }

    [Range(typeof(long), "0", "1000000")]
    public long Stock { get; set; }

    public bool IsActive { get; set; } = true;
    [Required]
    public uint? RowVersion { get; set; }

    // Các điều kiện không thể diễn đạt đầy đủ bằng Required/Range/MaxLength.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Cho phép Admin nhập chữ thường và khoảng trắng hai đầu.
        // Service vẫn phải chuẩn hóa SKU trước khi lưu.
        string sku = Sku?.Trim() ?? string.Empty;
        if (sku.Length < 3 || sku.Length > 50 ||
            sku.Any(c => !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
        {
            yield return new ValidationResult(
                "SKU phải có 3–50 ký tự sau trim, chỉ gồm chữ ASCII, số và dấu -.",
                new[] { nameof(Sku) });
        }

        if (Price != decimal.Truncate(Price))
            yield return new ValidationResult("Giá phải là số nguyên VND.", new[] { nameof(Price) });

        if (Connectivity != null)
        {
            if (Connectivity.Distinct().Count() != Connectivity.Count)
                yield return new ValidationResult("Các loại kết nối không được trùng.", new[] { nameof(Connectivity) });

            if (Connectivity.Any(value => value is not (Entities.Connectivity.Wired or
                Entities.Connectivity.Bluetooth or Entities.Connectivity.Wireless2_4G)))
                yield return new ValidationResult(
                    "Kết nối chỉ nhận Wired, Bluetooth hoặc Wireless2_4G riêng lẻ.",
                    new[] { nameof(Connectivity) });
        }
    }
}
