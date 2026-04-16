# Com.H.Threading.Scheduler.VP.Sql

A SQL Server value processor plug-in for [Com.H.Threading.Scheduler](https://github.com/H7O/Com.H.Threading.Scheduler).

It adds a `sql` content type that lets any custom tag — including `<repeat>` — execute a SQL Server query and use the results as dynamic data inside the scheduler's XML configuration.

## Installation

```
dotnet add package Com.H.Threading.Scheduler.VP.Sql
```

## What it does

`Com.H.Threading.Scheduler` supports a `content_type` attribute on any XML tag. This package registers the `sql` content type, which:

1. Opens a connection to the SQL Server identified by the `connection_string` attribute on the tag.
2. Executes the tag's text content as a SQL query.
3. Returns a lazy `IEnumerable<dynamic>` for use in your code, or feeds each row as an iteration variable when used inside `<repeat>`.

Because it plugs into the content-type pipeline, `sql` can be chained with other processors (e.g. `uri > sql` to fetch a query from a URL, then run it).

## Registering the processor

Before starting the scheduler, register `SqlValueProcessor` as a value processor:

```csharp
using Com.H.Threading.Scheduler;
using Com.H.Threading.Scheduler.VP;
using Com.H.Threading.Scheduler.VP.Sql;

var configPath = Path.Combine(AppContext.BaseDirectory, "scheduler.xml");
var scheduler = new HTaskScheduler(configPath);

// Register the SQL value processor
scheduler.AddValueProcessor(new SqlValueProcessor());

scheduler.TaskIsDue += async (object sender, HTaskEventArgs e, CancellationToken ct) =>
{
    // your task logic here
};

await scheduler.StartAsync();
```

## Tag syntax

```xml
<your_tag content_type="sql"
          connection_string="Server=myserver;Database=mydb;Integrated Security=True">
  SELECT column1, column2 FROM your_table WHERE condition = 'value'
</your_tag>
```

| Attribute | Required | Description |
|-----------|----------|-------------|
| `content_type` | Yes | Must be `sql` (or a chain ending with `sql`) |
| `connection_string` | Yes | A valid SQL Server ADO.NET connection string |

---

## Examples

### Example 1 — Iterating SQL results with `<repeat>`

The most common use case: drive a repeat loop from a live SQL query. Each row becomes one iteration, with column names available through `{var{column_name}}` placeholders.

> scheduler.xml
```xml
<?xml version="1.0" encoding="utf-8" ?>
<tasks_list>
  <task>
    <sys>
      <time>08:00</time>
      <repeat content_type="sql"
              connection_string="Server=myserver;Database=mydb;Integrated Security=True"
              delay_interval="200">
        SELECT id, name, email FROM users WHERE active = 1
      </repeat>
    </sys>
    <message>Sending report to {var{name}} at {var{email}} (ID: {var{id}})</message>
  </task>
</tasks_list>
```

```csharp
scheduler.TaskIsDue += async (object sender, HTaskEventArgs e, CancellationToken ct) =>
{
    Console.WriteLine(e["message"]);
    // e.g. "Sending report to Alice at alice@example.com (ID: 1)"
};
```

---

### Example 2 — Accessing SQL results from a custom tag in code

When you need to process rows yourself, place the `sql` content type on a custom tag and read the results with `GetModel<IEnumerable<dynamic>>()`:

> scheduler.xml
```xml
<?xml version="1.0" encoding="utf-8" ?>
<tasks_list>
  <task>
    <sys>
      <time>06:00</time>
    </sys>
    <report_data content_type="sql"
                 connection_string="Server=myserver;Database=mydb;Integrated Security=True">
      SELECT name, total_sales
      FROM monthly_summary
      WHERE month = {now{MM}} AND year = {now{yyyy}}
    </report_data>
  </task>
</tasks_list>
```

```csharp
scheduler.TaskIsDue += async (object sender, HTaskEventArgs e, CancellationToken ct) =>
{
    var rows = e.GetItem("report_data")?.GetModel<IEnumerable<dynamic>>();
    foreach (var row in rows ?? [])
        Console.WriteLine($"{row.name}: {row.total_sales}");
};
```

> **Note:** The `{now{MM}}` and `{now{yyyy}}` placeholders are resolved by the scheduler before the SQL query is executed, so the query sent to SQL Server is fully formed with literal values.

---

### Example 3 — Chaining `uri > sql`

The query text can itself come from an external file. Here the `uri` processor loads the SQL from a `.sql` file on disk, then the `sql` processor executes it. This keeps long or frequently-changing queries out of the XML config:

> scheduler.xml
```xml
<?xml version="1.0" encoding="utf-8" ?>
<tasks_list>
  <task>
    <sys>
      <interval>300000</interval>
      <repeat content_type="uri > sql"
              connection_string="Server=myserver;Database=mydb;Integrated Security=True"
              delay_interval="500">
        {dir{uri}}/queries/active-users.sql
      </repeat>
    </sys>
    <message>User: {var{name}}</message>
  </task>
</tasks_list>
```

> queries/active-users.sql
```sql
SELECT id, name, email
FROM users
WHERE active = 1
```

`{dir{uri}}` resolves to the application's base directory as a `file:///` URI, so the path always stays relative to the executable regardless of where it is deployed.

---

### Example 4 — Dynamic connection string via variable

Connection strings can use built-in variables or custom vars, letting a single task definition target different databases at runtime:

> scheduler.xml
```xml
<?xml version="1.0" encoding="utf-8" ?>
<tasks_list>
  <task>
    <sys>
      <interval>60000</interval>
      <repeat content_type="sql"
              connection_string="Server=myserver;Database=mydb_{now{yyyyMM}};Integrated Security=True">
        SELECT id, name FROM orders WHERE status = 'pending'
      </repeat>
    </sys>
    <message>Pending order {var{id}}: {var{name}}</message>
  </task>
</tasks_list>
```

---

## Error handling

If the connection string is missing, a `MissingFieldException` is thrown (the scheduler routes this to the `TaskExecutionError` event when retry is configured). If the query fails, a `FormatException` is thrown with the tag name, the query text, and the underlying error message — making it easy to diagnose problems in logs.

```csharp
scheduler.TaskExecutionError += async (object sender, HTaskExecutionErrorEventArgs e, CancellationToken ct) =>
{
    Console.WriteLine($"Task error: {e.Exception.Message}");
};
```

## Requirements

- .NET 8, 9, or 10
- [Com.H.Threading.Scheduler](https://www.nuget.org/packages/Com.H.Threading.Scheduler/) (auto-installed as a dependency)
- Microsoft SQL Server (any version supported by `Microsoft.Data.SqlClient`)

