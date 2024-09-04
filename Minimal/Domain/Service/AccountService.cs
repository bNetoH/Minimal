using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Minimal.Domain.Entity;
using Minimal.Domain.Interface;
using Minimal.Domain.Model;
using System.Text;

namespace Minimal.Domain.Service
{
    public class AccountService : IAccountService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountService(
            IConfiguration configuration,
            UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            SignInManager<IdentityUser> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public async Task<List<string>> Register(Register model) 
        {
            var feedbacks = new List<string>();

            var role = await _roleManager.FindByNameAsync(model.Role);
            if (role == null) 
            { 
                feedbacks.Add("Erro ao tentar localizar a função!");
                return feedbacks;
            }

            var user = new IdentityUser { Email = model.Email, UserName = model.Email };
            if (!(await _userManager.CreateAsync(user, model.Password)).Succeeded) 
            { 
                feedbacks.Add("Erro ao tentar criar usuário!");
                return feedbacks;
            }

            if (!(await _userManager.AddToRoleAsync(user, role.Name)).Succeeded) feedbacks.Add("Erro ao tentar adicionar função para o usuário!");

            return feedbacks;
        }

        public async Task<(IdentityUser, string)> Login(Login model) 
        {
            var user = new IdentityUser();
            var resultMsg = string.Empty;

            user = await _userManager.FindByNameAsync(model.Email);
            var results = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            switch (results)
            {
                case SignInResult r when r.Succeeded:
                    resultMsg = "Succeeded";
                    break;
                case SignInResult r when r.IsLockedOut:
                    resultMsg = "IsLockedOut";
                    break;
                case SignInResult r when r.IsNotAllowed:
                    resultMsg = "IsNotAllowed";
                    break;
                case SignInResult r when r.RequiresTwoFactor:
                    resultMsg = "RequiresTwoFactor";
                    break;
                default:
                    resultMsg = "Login failed.";
                    break;
            }
            return (user, resultMsg);
        }

        public async Task<string> LoginReturnTokenAccess(Login model) 
        { 
            var token = string.Empty;
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user != null) 
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                if (result.Succeeded) token = await TokenGenarator(user);
            }
            return token;
        }

        public async Task Logout() 
        {
            await _signInManager.SignOutAsync(); 
        }

        public async Task<string> TokenGenarator(IdentityUser user)
        {
            string key = _configuration.GetSection("Jwt").ToString() ?? string.Empty;
            var roles = await _userManager.GetRolesAsync(user);
            
            var userRole = roles?.FirstOrDefault()?.ToString();

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, userRole)
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        key)), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> ChangePassword(ChangePassword model, string email) 
        {
            var user = await _userManager.FindByEmailAsync(email) ?? new IdentityUser();
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded) return true;
            return false;
        }

        public async Task<IdentityUser> GetUser(string email) 
        {
            return await _userManager.FindByNameAsync(email) ?? new IdentityUser();
        }

    }
}