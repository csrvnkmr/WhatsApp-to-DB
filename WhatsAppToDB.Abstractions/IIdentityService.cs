using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IIdentityService
    {
        Task<IdentityContext> GetIdentityAsync(string mobileNumber);
        void HydrateRolePermissions(IdentityContext identity);
    }
}
