using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    public class Sqls
    {

        private const string UpdateDateCondition = @"
            (a.UpdateDate > b.LastSyncTs OR (
	        (convert(date, a.UpdateDate)=convert(date, b.LastSyncTs)) and a.UpdateTS > replace( convert(varchar, b.LastSyncTs, 24), ':', '')))
            ";

        public const string GetDataToSync = @"
            select a.$primarykeycolumn, a.UpdateDate, a.UpdateTS, b.LastSyncTs 
            from $table a left join $tablesync b on a.$primarykeycolumn=b.PrimaryKey where (1=1) and ( b.PrimaryKey is null or " + UpdateDateCondition + ")";
        //public static readonly string GetItemsToSync = GetDataToSync.Replace("$table", "oitm").Replace("$primarykeycolumn", "ItemCode");
        //public static readonly string GetBusinessPartnersToSync = GetDataToSync.Replace("$table", "ocrd").Replace("$primarykeycolumn", "CardCode");


        public const string GetItemsToSync = @"
            select a.ItemCode, a.ItemName, a.UpdateDate, a.UpdateTS, b.LastSyncTs 
            from oitm a left join oitmsync b on a.ItemCode=b.PrimaryKey where b.PrimaryKey is null or " + UpdateDateCondition;
        
        public const string GetBusinessPartnersToSync = @"
            select a.CardCode,a.CardName, a.CardType, a.UpdateDate, a.UpdateTS, b.LastSyncTs 
            from OCRD a left join ocrdsync b on a.CardCode=b.PrimaryKey where b.PrimaryKey is null or " + UpdateDateCondition;

        public const string UpsertSyncSuccess = @"
            MERGE INTO $tablename AS target
            USING (SELECT @primarykey as PrimaryKey) AS source
            ON target.PrimaryKey = source.PrimaryKey
            WHEN MATCHED THEN UPDATE SET LastSyncTs = getdate(), SyncMessage = 'success'
            WHEN NOT MATCHED THEN INSERT (PrimaryKey, LastSyncTs, SyncMessage) VALUES (@primarykey, getdate(), 'Success');";

        public const string UpsertSyncError = @"
            MERGE INTO $tablename AS target
            USING (SELECT @primarykey as PrimaryKey, @syncmessage as SyncMessage) AS source
            ON target.PrimaryKey = source.PrimaryKey
            WHEN MATCHED THEN UPDATE SET SyncMessage = source.SyncMessage
            WHEN NOT MATCHED THEN INSERT (PrimaryKey, LastSyncTs, SyncMessage) VALUES (@primarykey, getdate(), @syncmessage);";


        public static readonly string UpsertItemSyncSuccess = UpsertSyncSuccess.Replace("$tablename", "oitmsync");
        public static readonly string UpsertBusinessPartnerSyncSuccess = UpsertSyncSuccess.Replace("$tablename", "ocrdsync");
        public static readonly string UpsertItemSyncError = UpsertSyncError.Replace("$tablename", "oitmsync");
        public static readonly string UpsertBusinessPartnerSyncError = UpsertSyncError.Replace("$tablename", "ocrdsync");

    }
}
