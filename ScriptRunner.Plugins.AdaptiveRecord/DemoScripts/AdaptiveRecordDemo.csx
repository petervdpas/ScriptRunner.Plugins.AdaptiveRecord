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