using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xero.Authorisation.Integration.Common;

namespace SimpleMVC3App
{
    public class LegacyAuthorisationProvider : ILegacyAuthorisationProvider
    {
        public Task<Outcome> AssertAsync(Xero.Authorisation.Integration.Common.Action action)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<Xero.Authorisation.Integration.Common.Action, Outcome>> QueryAsync(AuthNamespace authNamespace)
        {
            throw new NotImplementedException();
        }
    }
}
