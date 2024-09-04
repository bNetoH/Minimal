using Microsoft.EntityFrameworkCore;
using Minimal.Domain.Entity;
using Minimal.Domain.DTO;
using Minimal.Domain.Interface;
using Minimal.Infrastructure;

namespace Minimal.Domain.Service
{
    public class VehicleService : IVehicleService
    {
        private readonly AppDbContext _context;
        public VehicleService(AppDbContext context) => _context = context; 

        public bool Insert(VehicleDTO vehicle)
        {
            try
            {
                _context.Vehicles.Add(new Vehicle 
                {
                    Modelo = vehicle.Modelo.ToUpper(),
                    Marca = vehicle.Marca.ToUpper(),
                    Cor = vehicle.Cor.ToUpper(),
                    Ano = vehicle.Ano,
                    Kilometragem = vehicle.Kilometragem
                });
                return _context.SaveChanges() > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro: {e.Message} ao tentar salvar veículo!");
                return false;                
            }
        }

        public bool Delete(int id)
        {
            var vehicle = _context.Vehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle == null) return false;

            try
            {
                _context.Vehicles.Remove(vehicle);
                return _context.SaveChanges() > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro: {e.Message} ao tentar deletar veículo!");
                return false;
            }
        }

        public List<Vehicle> GetAll(int pagina = 1, string? modelo = null, string? marca = null, int? ano = null)
        {
            var query = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrEmpty(modelo)) 
            { 
                query = query.Where(v => EF.Functions.Like(v.Modelo, $"%{modelo}%")); 
            }
            if (!string.IsNullOrEmpty(marca)) 
            { 
                query = query.Where(v => EF.Functions.Like(v.Marca, $"%{marca}%")); 
            }
            if (ano > 0) { query = query.Where(v => v.Ano == ano); }

            int itemsPerPage = 10;

            query = query.Skip((pagina -1) * itemsPerPage).Take(itemsPerPage);

            return query.ToList();
        }

        public Vehicle? GetById(int id)
        {
            try
            {
                return _context.Vehicles.FirstOrDefault(v => v.Id == id);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro: {e.Message} ao tentar obter um veículo!");
            }
            return null;               
        }

        public bool Update(Vehicle vehicle)
        { 
            try
            {
                _context.Entry(vehicle).State = EntityState.Modified;
                return _context.SaveChanges() > 0;        
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro: {e.Message} ao tentar atualizar veículo!");
                return false;
            }
        }

    }
}