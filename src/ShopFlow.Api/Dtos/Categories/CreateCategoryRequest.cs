

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Categories;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public String Name { get; set; } = string.Empty;

}