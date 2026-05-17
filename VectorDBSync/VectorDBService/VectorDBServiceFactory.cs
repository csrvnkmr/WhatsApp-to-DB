using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace VectorDBSync.VectorDBService
{
    public class VectorDBServiceFactory
    {
        public static IVectorDBService CreateVectorDBService(VectorDBSettings settings)
        {
            IVectorDBService vdbs;
            if (settings.VectorDBProvider.Type.ToLower() == "chroma")
            {
                vdbs = new ChromaDBService(settings);
            }
            else
            {
                vdbs = new SQLiteVectorDBService(settings);
            }
            return vdbs;
        }
    }
}
