using System;
using System.Data.Entity;

namespace OpenNos.DAL.EF.Context
{
    public class OpenNosConfig : DbConfiguration
    {
        public OpenNosConfig() =>
            SetExecutionStrategy("System.Data.SqlClient", () => new OpenNosExecutionStrategy(10, TimeSpan.FromSeconds(120)));
    }
}