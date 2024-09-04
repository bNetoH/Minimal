using Minimal.Domain.Entity;
using Minimal.Domain.DTO;

namespace Minimal.Domain.Interface
{
    public interface IVehicleService
    {
        bool Insert(VehicleDTO vehicle);
        bool Delete(int id);
        List<Vehicle> GetAll(int pagina = 1, string? modelo = null, string? marca = null, int? ano = null);
        Vehicle? GetById(int id);
        bool Update(Vehicle vehicle);
    }
}