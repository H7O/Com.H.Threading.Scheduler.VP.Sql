using System;
using System.Data.Common;
using System.Threading;
using Com.H.Data.Common;
using Com.H.Threading.Scheduler.VP;
using Microsoft.Data.SqlClient;


namespace Com.H.Threading.Scheduler.VP.Sql
{
    public class SqlValueProcessor : IValueProcessor
    {
        public ValueProcessorItem GetProcessor(ValueProcessorItem valueItem, 
            CancellationToken? token = null)
        {
            if (valueItem.IsValid("sql") == false) return valueItem;
            valueItem.Value ??= valueItem.Item?.RawValue;
            if (string.IsNullOrWhiteSpace(valueItem.Item?.Attributes?["connection_string"]))
                throw new MissingFieldException(
                    $"Missing 'connection_string' attribute for tag '{valueItem.Item?.FullName}");
            try
            {
                DbConnection db = new SqlConnection(valueItem.Item.Attributes["connection_string"]);
                valueItem.Data = db.ExecuteQuery(
                    valueItem.Value ?? "",
                    closeConnectionOnExit: true,
                    cToken: token ?? default);
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
