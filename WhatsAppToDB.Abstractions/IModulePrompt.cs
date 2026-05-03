using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IModulePrompt
    {
        /// <summary>
        /// Injected after schema retrieval to add user-specific constraints.
        /// </summary>
        Task<string> GetModuleConstraintAsync(IdentityContext identity, string moduleName, string userQuestion);
    }
}
