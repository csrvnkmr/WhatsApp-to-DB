using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    [Route("api/schema/{database}")]
    public class SchemaController : ControllerBase
    {
        private readonly DatabaseRegistry _databaseRegistry;
        private readonly DbProviderFactory _dbProviderFactory;

        public SchemaController(
            DatabaseRegistry databaseRegistry,
            DbProviderFactory dbProviderFactory)
        {
            _databaseRegistry = databaseRegistry;
            _dbProviderFactory = dbProviderFactory;
        }

        // Helper to resolve providers and establish a connection
        private (ISchemaProvider schemaProvider, System.Data.IDbConnection connection) ResolveDatabase(string databaseName)
        {
            var dbConfig = _databaseRegistry.GetDatabaseConfig(databaseName);
            if (dbConfig == null)            {
                throw new ArgumentException($"Database '{databaseName}' is not registered.");
            }
            var schemaProvider = _dbProviderFactory.GetSchemaProvider(dbConfig.DbProvider);
            var dbProvider = _dbProviderFactory.GetDbProvider(dbConfig.DbProvider);

            if (schemaProvider == null || dbProvider == null)
            {
                throw new ArgumentException($"Database provider '{databaseName}' is not registered or supported.");
            }

            // Fetches Connection Strings dynamically from appsettings.json named after the database provider
            string connectionString = dbConfig.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string for '{databaseName}' missing in configuration.");
            }

            var connection = dbProvider.GetConnection(connectionString);
            return (schemaProvider, connection);
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables(string database, [FromQuery] string filter = null)
        {
            try
            {
                var (provider, connection) = ResolveDatabase(database);
                using (connection)
                {
                    var tables = await provider.GetTablesAsync(connection, filter);
                    return Ok(tables);
                }
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("columns/{table}")]
        public async Task<IActionResult> GetColumns(string database, string table, [FromQuery] string filter = null)
        {
            try
            {
                var (provider, connection) = ResolveDatabase(database);
                using (connection)
                {
                    var columns = await provider.GetColumnsAsync(connection, table, filter);
                    return Ok(columns);
                }
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("foreignkeys/{table}")]
        public async Task<IActionResult> GetForeignKeys(string database, string table)
        {
            try
            {
                var (provider, connection) = ResolveDatabase(database);
                using (connection)
                {
                    var foreignKeys = await provider.GetForeignKeysAsync(connection, table);
                    return Ok(foreignKeys);
                }
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
    }
}