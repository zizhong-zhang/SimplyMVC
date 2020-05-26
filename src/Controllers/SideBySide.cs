using System.Threading.Tasks;
using Xero.Authorisation.Integration.Common;

namespace WebApplication14.Controllers
{
    public class SideBySide : IDoAuth
    {
        private readonly IAuthorisation _auth;

        public SideBySide(IAuthorisation auth)
        {
            _auth = auth;
        }
        public Outcome Assert(Action action)
        {
            return _auth.Assert(action);
        }

        public Task<Outcome> AssertAsync(Action action)
        {
            return _auth.AssertAsync(action);
        }
    }
}