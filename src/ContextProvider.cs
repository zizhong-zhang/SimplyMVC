using System;
using Xero.Authorisation.Integration.Common;

namespace SimpleMVC3App
{
    public class ContextProvider : IContextProvider
    {

        public ContextProvider()
        {
        }

        public UserId GetUserId()
        {
            try
            {
                return new UserId(new Guid());
            }
            catch
            {
                return null;
            }
        }

        public TenantId GetTenantId()
        {
            try
            {
                return new TenantId(new Guid());

                return null;
            }
            catch
            {
                return null;
            }
        }

        string IContextProvider.GetCorrelationId()
        {
            return "";
        }
    }
}
