using System.Threading.Tasks;
using Xero.Authorisation.Integration.Common;

namespace WebApplication14.Controllers
{
    public interface IDoAuth
    {
        Outcome Assert(Action action);

        Task<Outcome> AssertAsync(Action action);
    }
}