---
Title: The Adaptive Record Plugin
Subtitle: Dynamic Data Management with Adaptive Record in ScriptRunner
Category: Cookbook
Author: Peter van de Pas
keywords: [CookBook, AdaptiveRecord, Dynamic, Data]
table-use-row-colors: true
table-row-color: "D3D3D3"
toc: true
toc-title: Table of Content
toc-own-page: true
---

# Recipe: Dynamic Data Management with Adaptive Record in ScriptRunner

## Goal

Learn how to use the **Adaptive Record** plugin in ScriptRunner to dynamically manage records in a flexible schema, 
including fetching, adding, updating, and deleting records from a database.

This recipe covers defining dynamic classes from JSON schemas, fetching data dynamically, 
and interacting with the **Adaptive Record** plugin to manipulate the data within a database.

## Overview

This recipe demonstrates how to:
1. Define dynamic schemas using JSON and create corresponding classes.
2. Fetch data dynamically from a database.
3. Perform CRUD operations (Create, Read, Update, Delete) on records.
4. Display and interact with records using dialog services.

By the end of this tutorial, you'll have a working script that interacts with adaptive records and a database.

---

## Steps

### 1. Define Task Metadata

Add metadata to the script for identification and categorization:

```csharp
/*
{
    "TaskCategory": "Plugins",
    "TaskName": "Adaptive Record Demo",
    "TaskDetail": "This script demonstrates the usage of Adaptive Record Plugin.",
    "RequiredPlugins": ["Adaptive Record"]
}
*/
```

### 2. Load and Define the Schema

Define the schema for the **User** data model in a JSON file (for example, **AdaptiveRecordDemo.json**):

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

### 3. Initialize the AdaptiveRecord Plugin

Initialize the **AdaptiveRecord** plugin and create a dynamic class based on the loaded JSON schema:

```csharp
var fileHelper = new FileHelper();
string jsonFileName = fileHelper.RelativeToCurrentDirectory("AdaptiveRecordDemo.json");
string jsonSchema = fileHelper.ReadFile(jsonFileName);

var adaptiveRecord = new AdaptiveRecord(new DynamicClassBuilder("User"));
adaptiveRecord.CreateClassFromJson(jsonSchema);
```

### 4. Set Up Database

Set up an SQLite database and initialize a table for **Users**:

```csharp
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
```

### 5. Fetch and Manipulate Data

Define the delegates for **FetchData**, **AddRow**, **UpdateRow**, and **DeleteRow** to interact with the database:

```csharp
adaptiveRecord.FetchData = () =>
{
    var sqlGenerator = new SqlGenerator();
    sqlGenerator.SetType(adaptiveRecord.DynamicType!);
    sqlGenerator.SetTableName("Users");

    var selectQuery = sqlGenerator.GenerateSelectQuery();
    var dataTable = db.ExecuteQuery(selectQuery);
    return dataTable;
};

adaptiveRecord.AddRow = row =>
{
    var sqlGenerator = new SqlGenerator();
    sqlGenerator.SetType(adaptiveRecord.DynamicType!);
    sqlGenerator.SetTableName("Users");

    var insertQuery = sqlGenerator.GenerateInsertQuery();
    var parameters = sqlGenerator.MapParameters(row);
    db.ExecuteNonQuery(insertQuery, parameters);

    row["Id"] = db.ExecuteScalar("SELECT last_insert_rowid()");
};

adaptiveRecord.UpdateRow = row =>
{
    var sqlGenerator = new SqlGenerator();
    sqlGenerator.SetType(adaptiveRecord.DynamicType!);
    sqlGenerator.SetTableName("Users");

    var updateQuery = sqlGenerator.GenerateUpdateQuery();
    var parameters = sqlGenerator.MapParameters(row); 
    db.ExecuteNonQuery(updateQuery, parameters);
};

adaptiveRecord.DeleteRow = row =>
{
    var sqlGenerator = new SqlGenerator();
    sqlGenerator.SetType(adaptiveRecord.DynamicType!);
    sqlGenerator.SetTableName("Users");

    var deleteQuery = sqlGenerator.GenerateDeleteQuery();
    var parameters = new Dictionary<string, object> { { "@Id", row["Id"] } };
    db.ExecuteNonQuery(deleteQuery, parameters);
};
```

### 6. Display and Interact with Data

Use the **AdaptiveRecordDialogService** to display a dialog for selecting or interacting with records:

```csharp
var controlFactory = GetControlFactory();
var dialogService = new AdaptiveRecordDialogService(controlFactory);
await dialogService.GetAdaptiveRecordAsync(adaptiveRecord, "Try Adaptive Record Dialog", 960, 600);
```

### 7. View Results

Retrieve and display the **Users** data from the database:

```csharp
var usersTable = db.ExecuteQuery("SELECT * FROM Users");
DumpTable("Users:", usersTable);
```

### 8. Close Database Connection

Finally, close the database connection:

```csharp
db.CloseConnection();
return "Adaptive Record interaction complete.";
```

---

## Example Script

Here’s the complete script for reference:

```csharp
/*
{
    "TaskCategory": "Plugins",
    "TaskName": "Adaptive Record Demo",
    "TaskDetail": "This script demonstrates the usage of Adaptive Record Plugin.",
    "RequiredPlugins": ["Adaptive Record"]
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

## Expected Output

When executed, this script will:
1. Create and populate a table of users in an in-memory SQLite database.
2. Allow users to interact with the data dynamically through a dialog interface.
3. Perform CRUD operations on the data.
4. Display the resulting **Users** table.

---

## Tips & Notes

- **Dynamic Schema**: The **Adaptive Record** plugin allows for dynamic schema creation from JSON, making it highly flexible.
- **Interactive Dialogs**: Use the **AdaptiveRecordDialogService** to create rich, interactive forms for data entry and modification.
- **Customization**: Customize the dialog appearance and behavior by tweaking the parameters in the **GetAdaptiveRecordAsync** method.
- **Transactions**: Consider wrapping the database operations in transactions for better consistency when performing batch operations.
