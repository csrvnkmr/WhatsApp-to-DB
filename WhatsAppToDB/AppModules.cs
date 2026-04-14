using System.Reflection;

namespace WhatsAppToDB
{
    public class TableDetail
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public string TableColumns { get; set; }

        public TableDetail(string tableName, string tableDescription, string tableColumns)
        {
            TableName = tableName;
            TableDescription = tableDescription;
            TableColumns = tableColumns;
        }

        public override string ToString()
        {
            return $"{TableName} - {TableDescription}, Columns {TableColumns}";
        }
    }

    public class ModuleDetail
    {
        public string ModuleName { get; set; }
        public string ModuleJoins { get; set; }
        public List<TableDetail> TableDetails { get; set; } = new List<TableDetail>();

        public ModuleDetail AddTableDetail(string tableName, string tableDescription, string tableColumns)
        {            
            var tableDetail = new TableDetail(tableName, tableDescription, tableColumns);
            TableDetails.Add(tableDetail);
            return this;
        }

        public string GetSchemaDetails()
        {
            if (TableDetails != null)
            {
                var schemaDetails = "";
                TableDetails.ForEach(x => schemaDetails += x.ToString() + " ");
                schemaDetails += $", Joins {ModuleJoins}";
                return schemaDetails;
            }
            return "";
        }
    }

    public class AppModules
    {

        private string MasterModuleName = "Master Data";

        private static AppModules? _instance;
        public static AppModules Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppModules();
                    _instance.Init();
                }
                return _instance;
            }
        }


        Dictionary<string, ModuleDetail> moduleDetails = new Dictionary<string, ModuleDetail>();

        private const string DocumentJoins = "[HeaderTable].DocEntry=[LinesTable].DocEntry, [HeaderTable].CardCode=OCRD.CardCode, [LinesTable].Account=OACT.AcctCode, " +
                "[LinesTable].WhsCode=OWHS.WhsCode, [LinesTable].ItemCode=OITM.ItemCode";
        private const string DocumentColumns = "DocEntry, DocNum (Document Number), DocDate, CardCode, CardName, DocTotal (Document Total), VatSum (Tax Amount), " +
            "DocDueDate, CANCELED, PaidToDate (Paid Amount), DocStatus (O - Open, C - Closed) ";
        private const string DocumentLineColumns = "DocEntry, ItemCode, Account, Quantity, Price, LineTotal, VatSum (Tax Amount)";

        private AppModules AddDocumentTables(ModuleDetail module, string headerTable, string lineTable, string tableDesc)
        {
            module.AddTableDetail(headerTable, $"{tableDesc} Header", DocumentColumns)
                .AddTableDetail(lineTable, $"{tableDesc} Lines", DocumentLineColumns);
            return this;
        }

        private void AddMasterDataModule()
        {
            /*
             * { "MasterData", "Tables: OCRD (Business Partner - Customers and Vendors), OITM (Items), OITB (ItemGroups), OCRG (BP Groups), OTER (Territory)." +
                "OACT (Chart of Accounts), OSLP (Sales Employees). "},
             */
            var module = new ModuleDetail()
            {
                ModuleName = MasterModuleName,
                ModuleJoins = "OITM.ItmsGrpCod=OITB.ItmsGrpCod, OCRD.GroupCode=OCRG.GroupCode, OCRD.SlpCode=OSLP.SlpCode, " +
                    "ODSC.BankCode=DSC1.BankCode, DSC1.GLAccount=OACT.AcctCode "
            };
            module.AddTableDetail("ODSC", "Banks (Header)", "BankCode, BankName");
            module.AddTableDetail("DSC1", "Banks (Lines)", "BankCode, GLAccount (Account Code), Account (Bank Account number)");
            module.AddTableDetail("OCRD", "Business Partners(Customers & Suppliers)", "CardCode, CardName, SlpCode, Balance (Balance to be paid/received), " +
                "GroupCode, CardType ='C' (Customers), CardType='S' (Suppliers) ");
            module.AddTableDetail("OITM", "Items", "ItemCode, ItemName, OnHand (In Stock), Committed (Committed stock through Sales Orders/Production orders), " +
                "ItmsGrpCod ");
            module.AddTableDetail("OCRG", "Business Partner Groups", "GroupCode, GroupName");
            module.AddTableDetail("OITB", "Item Groups", "ItmsGrpCod (Item Group Code), ItmsGrpNam (Item Group Name)");
            module.AddTableDetail("OSLP", "Sales Persons", "SlpCode (Sales Person Code), SlpName (Sales Person Name)");
            module.AddTableDetail("OACT", "Chart of Accounts", "AcctCode (Account Code), AcctName (Account Name), CurrTotal (Account Balance), " +
                "ActType (Account Type - E(Expenses), I(Income), N (Others)");
            module.AddTableDetail("OWHS", "Warehouse Master", "WhsCode (Warehouse Code), WhsName (Warehouse Name)");
            moduleDetails.Add(module.ModuleName, module);
        }

        private void AddSalesModule()
        {
            var module = new ModuleDetail() { ModuleName = "Sales", ModuleJoins = DocumentJoins };
            AddDocumentTables(module, "OINV", "INV1", "Sales Invoice")
                .AddDocumentTables(module, "ORDR", "RDR1", "Sales Order")
                .AddDocumentTables(module, "ODLN", "DLN1", "Delivery")
                .AddDocumentTables(module, "ORIN", "RIN1", "Credit Note");

            moduleDetails.Add(module.ModuleName, module);
        }
        private void AddPurchaseModule()
        {
            var module = new ModuleDetail() { ModuleName = "Purchase", ModuleJoins = DocumentJoins };
            AddDocumentTables(module, "OPCH", "PCH1", "Purchase Invoice")
                .AddDocumentTables(module, "ORPC", "RPC1", "Purchase Returns/Debit Notes")
                .AddDocumentTables(module, "OPDN", "PDN1", "Purchase Delivery (Goods Received from Vendors)")
                .AddDocumentTables(module, "OPOR", "POR1", "Purchase Order")
                .AddDocumentTables(module, "OPRQ", "PRQ1", "Purchase Request");

            moduleDetails.Add(module.ModuleName, module);
        }

        private void AddAccountsModule()
        {
            var module = new ModuleDetail()
            {
                ModuleName = "Financing/Accouting",
                ModuleJoins = "OJDT.TransId=JDT1.TransId"
            };
            var ojdt = new TableDetail("OJDT", "Journal Entry Header", "TransId, RefDate (Transaction date)");
            var jdt1 = new TableDetail("JDT1", "Journal Entry Lines", "TransId, AcctCode (Account Code), Debit, Credit, ShortName (Customer/Supplier Code)");

            module.TableDetails.Add(ojdt);
            module.TableDetails.Add(jdt1);

            moduleDetails.Add(module.ModuleName, module);
        }

        private void AddInventoryModule()
        {
            var module = new ModuleDetail() { ModuleName = "Inventory", ModuleJoins = DocumentJoins };
            AddDocumentTables(module, "OIGN", "IGN1", "Goods Entry")
                .AddDocumentTables(module, "OIGE", "IGE1", "Goods Exit");

            moduleDetails.Add(module.ModuleName, module);
        }

        public string GetModules()
        {
            return string.Join(',', moduleDetails.Keys);
        }

        public string GetModuleDetails(string moduleName)
        {
            if (moduleName ==MasterModuleName)
            {
                return masterSchema;
            }
            // send master schema for all requests, as the masters are mostly used in more than one module
            var schemaDetails = masterSchema;
            if (moduleDetails.ContainsKey(moduleName))
            {

                schemaDetails += " " + moduleDetails[moduleName].GetSchemaDetails();
            }
            return schemaDetails;
        }

        string masterSchema = "";

        private void Init()
        {
            AddMasterDataModule();
                
            AddSalesModule();
            AddPurchaseModule();
            AddAccountsModule();
            AddInventoryModule();

            masterSchema = moduleDetails[MasterModuleName].GetSchemaDetails();

        }

    }
}
