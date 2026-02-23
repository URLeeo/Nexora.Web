using System.ComponentModel.DataAnnotations;

namespace Nexora.Web.Models.ProductModels;

public class ProductEditVm : ProductCreateVm
{
    [Required]
    public Guid Id { get; set; }
}