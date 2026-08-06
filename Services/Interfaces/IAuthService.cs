using System.Threading;
using System.Threading.Tasks;

namespace CAS_Login_Back_End.Services.Interfaces
{
    public interface IAuthService
    {
        Task<CAS_Login_Back_End.Models.Responses.LoginResponse> LoginAsync(
            string email,
            string password,
            string businessEntityName,
            CancellationToken cancellationToken = default);

        Task<CAS_Login_Back_End.Models.Responses.ExchangeTokenResponse> ExchangeTokenAsync(
            string ssoToken,
            string businessEntityName,
            CancellationToken cancellationToken = default);

        Task<CAS_Login_Back_End.Models.Responses.ValidateTokenResponse> ValidateTokenAsync(
            string token,
            CancellationToken cancellationToken = default);
    }
}
