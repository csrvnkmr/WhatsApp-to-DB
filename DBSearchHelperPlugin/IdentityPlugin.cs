using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;


namespace Persona.Plugin
{
    public class IdentityPersonaPlugin
    {        
        private static readonly string[] FirstPersonTriggers =
                ["my ", "i ", "i'", " me ", "mine", "myself", "i am", "i have", "i need", "i want", "do i", "am i", "can i"];

        [KernelFunction("ResolveUserContext")]
        [Description("Rewrites a user query by injecting the active user's identity when first-person references are detected. Call this before generating SQL for any user query.")]
        public string ResolveUserContext(Kernel kernel,
            [Description("The raw user query to resolve")] string query)
        {
            var queryLower = query.ToLowerInvariant();
            bool hasFirstPerson = FirstPersonTriggers.Any(t => queryLower.Contains(t));

            if (!hasFirstPerson)
                return query; // No personal reference — pass through

            // We use the Kernel.Data dictionary as the 'Bridge' 
            // The Host app (WhatsAppToDB) will put the IdentityContext here.
            if (!kernel.Data.TryGetValue("UserIdentity", out var identityObj))
            {
                return "No identity context available in the current session.";
            }

            // Using reflection/dynamic to avoid a hard project reference to your Abstractions DLL
            // This keeps the DLLs truly decoupled.
            dynamic identity = identityObj;

            var userrole = $"{identity.Role}" ;
            var userid= $"{identity.InternalUserId}";
            var sessionContextKey = $"{identity.SessionContextKey}";
            // Build the resolved hint
            var hint = BuildResolutionHint(userrole, userid, sessionContextKey );
            return $"{query} {hint}";
        }

        private string BuildResolutionHint(string userrole, string userid, string sessionContextKey)
        {
            // Role-specific hints so the LLM knows how to join to Person.Person
            var personJoinHint = userrole switch
            {
                "SalesPerson" =>
                    $"The user is a SalesPerson. Use SalesPersonID = {userid}. " +
                    $"Join Sales.SalesPerson to Person.Person ON BusinessEntityID = {userid}.",
                "Employee" =>
                    $"The user is an Employee. Use EmployeeID = {userid}. " +
                    $"Join HumanResources.Employee to Person.Person ON BusinessEntityID = {userid}.",
                "Customer" =>
                    $"The user is a Customer. Use CustomerID = {userid}. " +
                    $"Link Sales.Customer to Person.Person via PersonID = {userid}.",
                _ =>
                    $"The user has {sessionContextKey} = {userid}."
            };

            // Wrap clearly as system metadata, not user content
            return $"{personJoinHint}]";
        }
    }
}

