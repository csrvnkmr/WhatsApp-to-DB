using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IUserAuditService
    {
        Task LogAsync(
            string userId,
            string actionType,
            string? actionValue = null);

        Task<string?> GetLatestValueAsync(
            string userId,
            string actionType);
    }
}
