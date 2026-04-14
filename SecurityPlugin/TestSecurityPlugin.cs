using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace SecurityPlugin
{
    public class TestSecurityPlugin : IModulePrompt, ISqlInterceptor
    {
        public Task<string> GetModuleConstraintAsync(string whatsAppNumber, string moduleName, string userQuestion)
        {
            Console.WriteLine($"[TestSecurityPlugin] GetModuleConstraintAsync called with WhatsAppNumber: " +
                $"{whatsAppNumber}, ModuleName: {moduleName}, UserQuestion: {userQuestion}");
            var securityConstraint = $"### MANDATORY FILTER: For table 'Sales.SalesOrderHeader', you ARE PROHIBITED " +
                $"from returning any rows where 'SalesPersonID' is not 277. Every SELECT must include 'WHERE SalesPersonID = 277'";
            return Task.FromResult($"{securityConstraint}");
        }

        public Task<string> OnBeforeExecuteAsync(string whatsAppNumber, string sql)
        {
            Console.WriteLine($"[TestSecurityPlugin] OnBeforeExecuteAsync called with WhatsAppNumber: {whatsAppNumber}, SQL: {sql}");
            return Task.FromResult(sql);
        }
    }
}
