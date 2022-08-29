using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.H.EF.Relational;
using Com.H.Threading.Scheduler.VP;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;


namespace Com.H.Threading.Scheduler.VP.Sql
{
    public class SqlValueProcessor : IValueProcessor
    {
        #region DbContext
        private static DbContext GetDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new DbContext(optionsBuilder.Options);
        }
        #endregion

        public ValueProcessorItem GetProcessor(ValueProcessorItem valueItem, 
            CancellationToken? token = null)
        {
            if (valueItem.IsValid("sql") == false) return valueItem;
            valueItem.Value ??= valueItem.Item.RawValue;
            if (string.IsNullOrWhiteSpace(valueItem.Item.Attributes?["connection_string"]))
                throw new MissingFieldException(
                    $"Missing 'connection_string' attribute for tag '{valueItem.Item.FullName}");
            try
            {
                DbContext db = GetDbContext(valueItem.Item.Attributes?["connection_string"]);
                valueItem.Data = token == null?
                    db.ExecuteQuery(valueItem.Value, null)
                    : Cancellable.CancellableRun<IEnumerable<dynamic>>(
                    () => db.ExecuteQuery(valueItem.Value, null),
                    (CancellationToken)token);
                return valueItem;
            }
            catch (Exception ex)
            {
                throw new FormatException($"SQL Error executing query for tag {valueItem.Item.FullName}:\r\n"
                    + $"SQL query:\r\n{valueItem.Value}\r\n\r\n"
                    + $"Error msg: \r\n{ex.Message}");
            }

        }

    }
}
