using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface ISqlTemplateExtension
    {
        Task<string> GetSqlTemplateAsync(IdentityContext identity, string moduleName, string userQuestion);
    }
}
