using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync.VectorDBService
{
    public class VectorDBServiceFactory
    {
        public static IVectorDBService CreateVectorDBService(Settings settings)
        {
            IVectorDBService vdbs;
            if (settings.VectorDBSettings.Type.ToLower()=="chroma")
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
