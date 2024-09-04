using System.ComponentModel.DataAnnotations;

namespace Minimal.Domain.DTO
{
    public class VehicleDTO
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; }  = default;

        [Required]
        [StringLength(100)]
        [Display(Name = "Fabricante")]
        public string Marca  { get; set; } = default;

        [Required]
        [StringLength(25)]
        [Display(Name = "Cor")]
        public string Cor { get; set; } = default;
 
        [Required]
        [Display(Name = "Ano de Fabricação (yyyy)")]
        public int Ano { get; set; } = default;
 
        [Required]
        [Display(Name = "Kilometragem")]
        public int Kilometragem { get; set; } = default;
    }
}