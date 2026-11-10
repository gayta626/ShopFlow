

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Categories;

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool ConfirmRename { get; set; }
}
