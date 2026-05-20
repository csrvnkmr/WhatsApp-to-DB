using DBSearchHelperPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace SecurityPlugin
{
    public class SecurityExtension : IModulePrompt, ISqlInterceptor, ISqlTemplateExtension
    {
        public Task<string> GetModuleConstraintAsync(IdentityContext identity, string moduleName, string userQuestion)
        {
            Console.WriteLine($"[SecurityExtension] GetModuleConstraintAsync called with UserName: " +
                $"{identity.UserName}, ModuleName: {moduleName}, UserQuestion: {userQuestion}");
            //return "";
            return Task.FromResult($"");
            /*
            var securityConstraint = $"### MANDATORY FILTER: For table 'Sales.SalesOrderHeader', you ARE PROHIBITED " +
                $"from returning any rows where 'SalesPersonID' is not 277. Every SELECT must include 'WHERE SalesPersonID = 277'";
            return Task.FromResult($"{securityConstraint}");
            */
        }

        public async Task<string> GetSqlTemplateAsync(IdentityContext identity, string moduleName, string userQuestion)
        {
            try
            {
                var shp = new SearchHelperPlugin();
                var searchResult = await shp.FuzzySearchCode("FEWSHOTQUERIES", userQuestion, "QueryText");
                if (!string.IsNullOrWhiteSpace(searchResult))
                {
                    return $"\n[REQUIRED TEMPLATE]: {searchResult}"; ;
                }
                return "";
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[SecurityExtension] Error in GetSqlTemplateAsync: {ex}");
                return "";
            }
        }

        public Task<string> OnBeforeExecuteAsync(IdentityContext identity, string UserName, string sql)
        {
            Console.WriteLine($"[SecurityExtension] OnBeforeExecuteAsync called with UserName: {identity.UserName}, SQL: {sql}");
            return Task.FromResult(sql);
        }
    }
}
