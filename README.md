# ScriptRunner.Plugins.AdaptiveRecord

![License](https://img.shields.io/badge/license-MIT-green)  
![Version](https://img.shields.io/badge/version-1.0.0-blue)

A versatile plugin for **ScriptRunner**, empowering dynamic data interactions through adaptive record management.
This plugin provides advanced features such as runtime class generation, SQL integration,
and UI dialogs for streamlined record handling in your scripting environment.

---

## 🚀 Features

- **Dynamic Record Management**: Generate and interact with records based on JSON-defined schemas.
- **UI Integration**: Modal dialogs for record selection and editing using Avalonia.
- **SQL Integration**: Includes helpers for SQL generation, mapping, and database interaction.
- **Delegate Support**: Customize fetch, insert, update, and delete operations using delegates.
- **Extensibility**: Fully customizable, supporting additional functionality via scripting or extensions.
- **ScriptRunner Integration**: Designed to seamlessly fit into the ScriptRunner ecosystem.

---

## 📦 Installation

### Plugin Activation
Place the `ScriptRunner.Plugins.AdaptiveRecord` plugin assembly in the `Plugins` folder of your ScriptRunner project.
The plugin will be automatically discovered and activated.

---

## 📖 Usage

### Writing a Script

Here’s an example script to demonstrate creating and managing records:

```csharp
/*
{
    "TaskCategory": "Plugins",
    "TaskName": "Adaptive Record Demo",
    "TaskDetail": "This script demonstrates the usage of Adaptive Record Plugin."
}
*/

var fileHelper = new FileHelper();
string jsonFileName = fileHelper.RelativeToCurrentDirectory("AdaptiveRecordDemo.json");
string jsonSchema = fileHelper.ReadFile(jsonFileName);

// Initialize the MultiRecordService and define the structure from the JSON
var adaptiveRecord = new AdaptiveRecord(new DynamicClassBuilder("TestingClass"));
adaptiveRecord.CreateClassFromJson(jsonSchema);

var sqlGenerator = new SqlGenerator();
sqlGenerator.SetType(adaptiveRecord.DynamicType!);
sqlGenerator.SetTableName("Users");

// var classStructureInfo = adaptiveRecord.InspectClassStructure();
// Dump(classStructureInfo);

var db = new SqliteDatabase();
db.Setup("Data Source=:memory:");
db.OpenConnection();

string createTableQuery = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT,
    Age INTEGER,
    Country TEXT,
    DateOfBirth DATE,
    Status BOOLEAN
);";
db.ExecuteNonQuery(createTableQuery);

string insertUsersQuery = @"
INSERT INTO Users (Username, Age, Country, DateOfBirth, Status) VALUES 
('John Doe', 29, 'USA', '1993-05-15', 1),
('Jane Smith', 34, 'Canada', '1988-07-22', 1),
('Bob Dobbs', 63, 'Canada', '1960-07-22', 1),
('Alice Brown', 42, 'United Kingdom', '1981-03-12', 1),
('Bob White', 25, 'Australia', '1998-11-04', 1),
('Carol Green', 28, 'Germany', '1995-01-20', 1),
('David Black', 50, 'France', '1973-06-08', 1),
('Emma Gray', 32, 'USA', '1991-04-30', 1),
('Frank Yellow', 45, 'Canada', '1978-08-15', 1),
('Grace Blue', 38, 'United Kingdom', '1985-02-10', 1);
";
db.ExecuteNonQuery(insertUsersQuery);

// Set up the delegates
adaptiveRecord.FetchData = () =>
{
    var selectQuery = sqlGenerator.GenerateSelectQuery();
    var dataTable = db.ExecuteQuery(selectQuery);
    return dataTable;
};

adaptiveRecord.AddRow = row =>
{
    var insertQuery = sqlGenerator.GenerateInsertQuery();
    var parameters = sqlGenerator.MapParameters(row);
    db.ExecuteNonQuery(insertQuery, parameters);

    row["Id"] = db.ExecuteScalar("SELECT last_insert_rowid()");
};

adaptiveRecord.UpdateRow = row =>
{
    var updateQuery = sqlGenerator.GenerateUpdateQuery();
    var parameters = sqlGenerator.MapParameters(row); 
    db.ExecuteNonQuery(updateQuery, parameters);
};

adaptiveRecord.DeleteRow = row =>
{
    var deleteQuery = sqlGenerator.GenerateDeleteQuery();
    var parameters = new Dictionary<string, object> { { "@Id", row["Id"] } };
    db.ExecuteNonQuery(deleteQuery, parameters);
};

// Initialize data in the service
adaptiveRecord.FetchDataRows();

var controlFactory = GetControlFactory();
var dialogService = new AdaptiveRecordDialogService(controlFactory);
await dialogService.GetAdaptiveRecordAsync(adaptiveRecord, "Try Adaptive Record Dialog", 960, 600);

var usersTable = db.ExecuteQuery("SELECT * FROM Users");
DumpTable("Users:", usersTable);

// Close the database connection
db.CloseConnection();

return "Adaptive Record interaction complete.";
```

---

## 🔧 Configuration

### JSON Schema Example

Define your record schema in a JSON file for use with the plugin:

```json
[
  {
    "Name": "Id",
    "TypeName": "System.Int64",
    "ControlType": "GeneratedIdTextBox",
    "Placeholder": "Generated Id",
    "IsRequired": true,
    "IsDisplayField": false,
    "Options": null,
    "ControlParameters": {
      "IsReadOnly": true
    },
    "DataSetControls": null
  },
  {
    "Name": "Username",
    "TypeName": "System.String",
    "ControlType": "TextBox",
    "Placeholder": "Enter username",
    "IsRequired": true,
    "IsDisplayField": true,
    "Options": null,
    "ControlParameters": {
      "MaxLength": 50
    },
    "DataSetControls": null
  },
  {
    "Name": "Age",
    "TypeName": "System.Int64",
    "ControlType": "NumericUpDown",
    "Placeholder": "Enter age",
    "IsRequired": true,
    "IsDisplayField": false,
    "Options": null,
    "ControlParameters": {
      "Min": 0,
      "Max": 120
    },
    "DataSetControls": {
      "IsAggregator": true,
      "AggregateFunction": "Average"
    }
  },
  {
    "Name": "Country",
    "TypeName": "System.String",
    "ControlType": "ComboBox",
    "Placeholder": "Select country",
    "IsRequired": true,
    "IsDisplayField": false,
    "Options": [
      "USA",
      "Canada",
      "Mexico",
      "Germany",
      "France",
      "United Kingdom",
      "Australia"
    ],
    "ControlParameters": null,
    "DataSetControls": {
      "IsGroupable": true,
      "Filterable": true
    }
  },
  {
    "Name": "DateOfBirth",
    "TypeName": "System.DateTime",
    "ControlType": "DatePicker",
    "Placeholder": "Birthday",
    "IsRequired": false,
    "IsDisplayField": false,
    "Options": null,
    "ControlParameters": null,
    "DataSetControls": {
      "DateAggregation": "Year"
    }
  },
  {
    "Name": "Status",
    "TypeName": "System.Boolean",
    "ControlType": "CheckBox",
    "Placeholder": "Active status",
    "IsRequired": false,
    "IsDisplayField": false,
    "Options": null,
    "ControlParameters": {
      "UseNumericBoolean": true
    },
    "DataSetControls": {
      "Filterable": true
    }
  }
]
```

### Database Integration

Use the plugin with SQL integration for seamless data storage and retrieval:
- Generate `CREATE`, `SELECT`, `INSERT`, `UPDATE`, and `DELETE` queries with `SqlGenerator`.
- Map records to SQL parameters for dynamic query execution.

---

### Delegates for Custom Data Operations

The `AdaptiveRecord` plugin provides flexibility for data management via the following delegates:
- **FetchData**: Load data from a source into the adaptive record.
- **AddRow**: Handle the insertion of a new record.
- **UpdateRow**: Update existing records in the data source.
- **DeleteRow**: Delete records from the data source.

For example:
```csharp
adaptiveRecord.FetchData = () =>
{
    var dataTable = new DataTable();
    dataTable.Columns.Add("Id", typeof(long));
    dataTable.Columns.Add("Name", typeof(string));
    dataTable.Rows.Add(1, "John Doe");
    return dataTable;
};

adaptiveRecord.AddRow = row =>
{
    Console.WriteLine($"Adding row: {row["Name"]}");
};
```

---

## 🌟 Advanced Features

### Custom Control Factory

Integrate custom UI controls for your records
by extending the `IAvaloniaControlFactory` service to suit specific application requirements.

### Plugin Execution

The plugin is fully configurable,
allowing you to register additional services
and hooks through `IServiceCollection` in ScriptRunner's dependency injection container.


### SQL Generation

Use the `SqlGenerator` for creating queries dynamically:
- `GenerateCreateTableQuery`
- `GenerateInsertQuery`
- `GenerateSelectQuery`
- `GenerateUpdateQuery`
- `GenerateDeleteQuery`

### UI Integration

Customize the `IAvaloniaControlFactory` to adapt controls to your specific UI needs.

---

## 📄 Contributing

1. Fork this repository.
2. Create a feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add YourFeature'`).
4. Push the branch (`git push origin feature/YourFeature`).
5. Open a pull request.

---

## Author

Developed with **🧡 passion** by **Peter van de Pas**.

For any questions or feedback, feel free to open an issue or contact me directly!

---

## 🔗 Links

- [ScriptRunner Plugins Repository](https://github.com/petervdpas/ScriptRunner.Plugins)

---

## License

This project is licensed under the [MIT License](./LICENSE). 