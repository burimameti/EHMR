using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Backups
{
    using System.Security.Cryptography;
    using System.Text;





    public static class BackupFileHelper
    {

         
        public static string CreateBackupFileName()
        {
            return $"EHMR_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        }

        public static string CreateZipFileName()
        {
            return $"EHMR_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        }

        public static string CreateEncryptedFileName()
        {
            return $"EHMR_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
        }
    }
}
