using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Minimal.Domain.Entity
{
    public class Vehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = default;

        [Display(Name = "Modelo")]
        [StringLength(150), MaxLength(150)]
        public string Modelo { get; set; }  = default;

        [Display(Name = "Fabricante")]
        [StringLength(100),MaxLength(100)]
        public string Marca  { get; set; } = default;

        [Display(Name = "Cor")]
        [StringLength(25),MaxLength(25)]
        public string Cor { get; set; } = default;
 
        [Display(Name = "Ano de Fabricação (yyyy)")]
        public int Ano { get; set; } = default;
        
        [Display(Name = "Kilometragem")]
        public int Kilometragem { get; set; } = default;
    }
}