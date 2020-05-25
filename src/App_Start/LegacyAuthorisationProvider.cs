using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xero.Authorisation.Integration.Common;

namespace SimpleMVC3App.App_Start
{
    public class LegacyAuthorisationProvider : ILegacyAuthorisationProvider
    {
        public Task<Xero.Authorisation.Integration.Common.Outcome> AssertAsync(Xero.Authorisation.Integration.Common.Action action)
        {
            return Task.FromResult(Xero.Authorisation.Integration.Common.Outcome.Denied(Xero.Authorisation.Integration.Common.DenyReason.Unspecified));
        }

        public Task<Dictionary<Xero.Authorisation.Integration.Common.Action, Xero.Authorisation.Integration.Common.Outcome>> QueryAsync(AuthNamespace authNamespace)
        {
            throw new Exception();
        }
    }
}