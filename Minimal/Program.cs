using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minimal.Domain.DTO;
using Minimal.Domain.Entity;
using Minimal.Domain.Interface;
using Minimal.Domain.Model;
using Minimal.Domain.Service;
using Minimal.Infrastructure;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var services = builder.Services;
    var config = builder.Configuration;
    var conn = config.GetConnectionString("DefaultConnection");


    services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));
    services.AddEndpointsApiExplorer();

    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MinimalApi", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization scheme {Bearer token}",
        });
        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
                }, new string[] {}
        }
            });
    });

    services.AddAuthentication(option => {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(option => {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config.GetSection("Jwt").ToString() ?? string.Empty)
            )
        };
    });
    services.AddAuthorization(options =>
    {
        options.AddPolicy("MinimalApi_Dio_me_Administrator", x => x.RequireRole("admin"));
    });

    services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
    services.AddScoped<IVehicleService, VehicleService>();
    services.AddScoped<IAccountService, AccountService>();

    var app = builder.Build();

    app.MapGet("/", ctx =>
    {
        ctx.Response.Redirect("/swagger/index.html");
        return Task.CompletedTask;
    });


    app.MapPost("/users/create", async (IAccountService account, [FromBody] Register model) =>
    {
        var errors = await account.Register(model);
        if (errors.Count > 0) return Results.BadRequest(errors);
        return Results.Ok("Sucesso!");
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Sistema");

    app.MapGet("/users/list", (UserManager<IdentityUser> userManager) =>
    {
        var users = userManager.Users;
        if (users.Count() <= 0) return Results.NoContent();
        foreach (var user in users)
        {
            user.ConcurrencyStamp = "";
            user.PasswordHash = "";
            user.SecurityStamp = "";
        }
        return Results.Ok(users);
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Sistema");

    app.MapPost("/roles/create", async (RoleManager<IdentityRole> roleManager, [FromBody] RoleDTO role) =>
    {
        var identityRole = new IdentityRole();
        identityRole.Name = role.Name;

        var result = await roleManager.CreateAsync(identityRole);
        if (result.Succeeded)
        {
            return Results.Ok();
        }
        return Results.BadRequest(result.Errors);
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Sistema");

    app.MapGet("/roles/list", (RoleManager<IdentityRole> roleManager) =>
    {
        var roles = roleManager.Roles;
        return Results.Ok(roles);
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Sistema");


    app.MapPost("/users/login", async (IAccountService account, [FromBody] Login model) =>
    {
        var token = await account.LoginReturnTokenAccess(model);
        if (string.IsNullOrEmpty(token)) return Results.NotFound();
        return Results.Ok(token);
    })
    .WithTags("Usuário");

    app.MapPost("users/logout", async (IAccountService account) => {
        await account.Logout();
        return Results.Ok();
    })
    .RequireAuthorization()
    .WithTags("Usuário");

    app.MapPost("users/changepassword", async (HttpContext context, IAccountService account, [FromBody] ChangePassword model) => {
        var userMail = context.User.Identity?.Name?.ToString();
        if (userMail == null) return Results.BadRequest("Erro não localizei você no sistema, contacte seu suporte de TI.");
        var sucesso = await account.ChangePassword(model, userMail.ToString());
        if (!sucesso) return Results.BadRequest("Erro ao tentar trocar a senha, contacte seu suporte de TI.");
        return Results.Ok("Sucesso!");
    })
    .RequireAuthorization()
    .WithTags("Usuário");


    app.MapPost("/veiculos/Cadastrar", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
    {
        var feedbacks = new List<string>();
        if (vehicleDTO.Ano.ToString().Length < 4 || vehicleDTO.Ano.ToString().Length > 4) feedbacks.Add("Please send us the year YYYY of manufacturing.");
        var vehicleAge = DateTime.Now.Year - vehicleDTO.Ano;
        if (vehicleAge > 6) feedbacks.Add("Sorry! We only accept vehicles that are less than 6 years old.");
        if (vehicleDTO.Kilometragem > 220000) feedbacks.Add("Sorry! We only accept vehicles with fewer than 220,000 kilometers.");

        if (feedbacks.Count > 0) return Results.BadRequest(feedbacks);
        if (vehicleService.Insert(vehicleDTO)) { return Results.Ok("Success!"); }
        else { return Results.BadRequest("Not Accepted!"); }
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Negócios");

    app.MapDelete("/veiculo", ([FromQuery] int id, IVehicleService vehicleService) =>
    {
        if (vehicleService.Delete(id)) { return Results.Ok("Success!"); }
        else { return Results.BadRequest("An error has occurred!"); }
    })
    .RequireAuthorization("MinimalApi_Dio_me_Administrator")
    .WithTags("Admin Negócios");


    app.MapGet("/veiculo/obter", ([FromQuery] int id, IVehicleService vehicleService) =>
    {
        var vehicle = vehicleService.GetById(id);
        if (vehicle != null) { return Results.Ok(vehicle); }
        else { return Results.NotFound("Not found!"); }
    })
    .RequireAuthorization()
    .WithTags("Comercial");

    app.MapGet("/veiculos/list", ([FromQuery] int pagina, string? modelo, string? marca, int? ano, IVehicleService vehicleService) =>
    {
        var vehicles = vehicleService.GetAll(pagina, modelo, marca, ano);
        if (vehicles.Count > 0) { return Results.Ok(vehicles); }
        else { return Results.NotFound("None was found!"); }
    })
    .RequireAuthorization()
    .WithTags("Comercial");

    app.MapPut("/veiculo/atualizar", ([FromBody] Vehicle vehicle, IVehicleService vehicleService) =>
    {
        var feedbacks = new List<string>();

        if (string.IsNullOrEmpty(vehicle.Modelo)) feedbacks.Add("Please send us vehicle model.");
        if (string.IsNullOrEmpty(vehicle.Marca)) feedbacks.Add("Please send us vehicle brand manufacturer.");
        if (string.IsNullOrEmpty(vehicle.Cor)) feedbacks.Add("Sorry! We need you inform us the vehicle's color.");
        if (vehicle.Ano.ToString().Length != 4) feedbacks.Add("Please send us the year YYYY of manufacturing.");
        var vehicleAge = DateTime.Now.Year - vehicle.Ano;
        if (vehicleAge > 6) feedbacks.Add("Sorry! It's not allowed to change the year property of a vehicle!");
        if (vehicleService.GetById(vehicle.Id)?.Kilometragem != vehicle.Kilometragem) feedbacks.Add("Sorry! It's not allowed to change the kilometers property of a vehicle.");
        if (feedbacks.Count > 0) return Results.BadRequest(feedbacks);

        if (vehicleService.Update(vehicle)) { return Results.Ok("Success!"); }
        else { return Results.BadRequest("An error has occurred!"); }
    })
    .RequireAuthorization()
    .WithTags("Comercial");


    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();

    app.Run();

}
catch (Exception e)
{
    Console.WriteLine($"Erro {e.Message} ao tentar executar a aplicação!");
}