# Com.H.Threading.Scheduler.VP.Sql

A SQL Server value processor plug-in for [Com.H.Threading.Scheduler](https://github.com/H7O/Com.H.Threading.Scheduler).

Adds a `sql` content type that lets any XML tag in the scheduler config — including `<repeat>` — execute a live SQL Server query and use the results as dynamic data, fully integrated with the scheduler's variable substitution, chaining, and caching pipeline.

## Quick example

```xml
<task>
  <sys>
    <time>08:00</time>
    <repeat content_type="sql"
            connection_string="Server=myserver;Database=mydb;Integrated Security=True"
            delay_interval="200">
      SELECT id, name, email FROM users WHERE active = 1
    </repeat>
  </sys>
  <message>Sending report to {var{name}} at {var{email}}</message>
</task>
```

```csharp
using Com.H.Threading.Scheduler.VP.Sql;

scheduler.AddValueProcessor(new SqlValueProcessor());

scheduler.TaskIsDue += async (object sender, HTaskEventArgs e, CancellationToken ct) =>
{
    Console.WriteLine(e["message"]);
};
```

For full documentation and examples, visit the [GitHub repository](https://github.com/H7O/Com.H.Threading.Scheduler.VP.Sql).

