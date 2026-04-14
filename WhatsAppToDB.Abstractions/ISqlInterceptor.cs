using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface ISqlInterceptor
    {
        /// <summary>
        /// Final gatekeeper to modify or validate SQL before it hits the database.
        /// </summary>
        Task<string> OnBeforeExecuteAsync(string whatsAppNumber, string generatedSql);
    }
}
