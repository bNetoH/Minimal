using Microsoft.AspNetCore.Identity;
using Minimal.Domain.Entity;
using Minimal.Domain.Model;

namespace Minimal.Domain.Interface
{
    public interface IAccountService
    {
        Task<List<string>> Register(Register model);
        Task<(IdentityUser, string)> Login(Login model);
        Task<string> LoginReturnTokenAccess(Login model);
        Task<string> TokenGenarator(IdentityUser user);
        Task Logout();
        Task<bool> ChangePassword(ChangePassword model, string email); 
    }
}