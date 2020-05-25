using System;
using Xero.Authorisation.Integration.Common;

namespace SimpleMVC3App.App_Start
{
    internal class ContextProvider : IContextProvider
    {
        public UserId GetUserId()
        {
            return new UserId(Guid.NewGuid());
        }

        public TenantId GetTenantId()
        {
            return new TenantId(Guid.NewGuid());
        }

        public string GetCorrelationId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}