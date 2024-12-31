using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ScriptRunner.Plugins.AdaptiveRecord.Interfaces;
using ScriptRunner.Plugins.Interfaces;
using ScriptRunner.Plugins.Models;

namespace ScriptRunner.Plugins.AdaptiveRecord;

/// <summary>
/// Provides functionality for managing adaptive records, including dynamic class generation,
/// data manipulation, and schema validation.
/// </summary>
public class AdaptiveRecord : IAdaptiveRecord
{
    private readonly IDynamicClassBuilder _classBuilder;
    private readonly DataTable _dataTable = new();
    private readonly Dictionary<long, DataRow> _formInstanceDataMap = new();
    private long _currentId = 1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdaptiveRecord" /> class.
    /// </summary>
    /// <param name="classBuilder">The dynamic class builder to use for creating classes.</param>
    public AdaptiveRecord(IDynamicClassBuilder classBuilder)
    {
        _classBuilder = classBuilder;
    }

    /// <summary>
    ///     Delegate to fetch data from an external source.
    /// </summary>
    public FetchDataCallback? FetchData { get; set; }

    /// <summary>
    ///     Delegate to apply an update operation on a data row.
    /// </summary>
    public UpdateRowCallback? UpdateRow { get; set; }

    /// <summary>
    ///     Delegate to apply an add operation on a data row.
    /// </summary>
    public AddRowCallback? AddRow { get; set; }

    /// <summary>
    ///     Delegate to apply a delete operation on a data row.
    /// </summary>
    public DeleteRowCallback? DeleteRow { get; set; }

    /// <summary>
    ///     Gets the dynamically generated class type based on the schema defined in JSON.
    /// </summary>
    /// <remarks>
    ///     This property provides access to the generated type after the
    ///     <see cref="CreateClassFromJson" /> method is invoked, allowing
    ///     other components to retrieve metadata about the generated structure.
    /// </remarks>
    /// <value>
    ///     The <see cref="Type" /> representing the dynamically created class,
    ///     or <c>null</c> if the class has not yet been generated.
    /// </value>
    public Type? DynamicType { get; private set; }

    /// <summary>
    ///     Creates a dynamic class from a JSON string that defines the properties.
    /// </summary>
    /// <param name="jsonString">The JSON string describing class properties.</param>
    /// <returns>The <see cref="Type" /> of the dynamically created class.</returns>
    /// <exception cref="ArgumentException">Thrown if the JSON format is invalid.</exception>
    public Type CreateClassFromJson(string jsonString)
    {
        // Deserialize the JSON into a list of PropertyDefinition objects
        var properties = JsonSerializer.Deserialize<List<PropertyDefinition>>(jsonString);
        if (properties == null)
            throw new ArgumentException("Invalid JSON format for properties.");

        ValidateSchema(properties);

        // Add properties to the dynamic class using IDynamicClassBuilder
        foreach (var property in properties) _classBuilder.AddProperty(property);

        // Build and store the dynamic Type
        DynamicType = _classBuilder.Build();
        return DynamicType;
    }

    /// <summary>
    /// Fetches data rows from an external source and populates the internal DataTable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the fetch callback is not set.</exception>
    public void FetchDataRows()
    {
        if (FetchData == null)
            throw new InvalidOperationException("FetchData callback is not set.");

        _dataTable.Clear();

        var fetchedData = FetchData.Invoke();
        _dataTable.Merge(fetchedData);

        foreach (DataRow row in _dataTable.Rows)
        {
            var id = _currentId++;
            row["Id"] = id;

            var instance = Activator.CreateInstance(DynamicType!);
            if (instance == null) continue;

            foreach (var prop in DynamicType!.GetProperties())
            {
                if (!row.Table.Columns.Contains(prop.Name)) continue;

                var value = row[prop.Name] != DBNull.Value ? row[prop.Name] : null;

                value = value switch
                {
                    // Handle DateTimeOffset or DateTime properties separately
                    string dateString when prop.PropertyType == typeof(DateTimeOffset) &&
                                           DateTimeOffset.TryParse(dateString, out var parsedDateOffset) =>
                        parsedDateOffset,
                    string dateString when prop.PropertyType == typeof(DateTime) &&
                                           DateTime.TryParse(dateString, out var parsedDateTime) => parsedDateTime,
                    string => null,
                    DateTime dbDate when prop.PropertyType == typeof(DateTimeOffset) => new DateTimeOffset(dbDate),
                    _ => value
                };

                // For other types, attempt conversion
                if (value != null && !prop.PropertyType.IsInstanceOfType(value))
                    value = Convert.ChangeType(value, prop.PropertyType);

                prop.SetValue(instance, value);
            }

            _formInstanceDataMap[id] = row;
        }
    }

    /// <summary>
    /// Updates a row in the DataTable with values from the specified instance.
    /// </summary>
    /// <param name="instance">The object instance containing updated property values.</param>
    public void AddDataRow(object instance)
    {
        var row = _dataTable.NewRow();
        var id = _currentId++;
        row["Id"] = id;

        var idProperty = GetIdProperty();
        idProperty?.SetValue(instance, Convert.ChangeType(id, idProperty.PropertyType));

        foreach (var prop in DynamicType!.GetProperties())
        {
            if (!_dataTable.Columns.Contains(prop.Name))
            {
                continue;
            }

            var value = prop.GetValue(instance);
            row[prop.Name] = value ?? DBNull.Value;
        }

        _dataTable.Rows.Add(row);
        _formInstanceDataMap[id] = row;
        AddRow?.Invoke(row);
    }

    /// <summary>
    /// Updates a row in the DataTable with values from the specified instance.
    /// </summary>
    /// <param name="instance">The object instance containing updated property values.</param>
    public void UpdateDataRow(object instance)
    {
        if (instance == null || !DynamicType!.IsInstanceOfType(instance))
            throw new ArgumentException("Instance does not match the expected type.", nameof(instance));

        var idProp = GetIdProperty();
        var idPropValue = idProp?.GetValue(instance);
        if (idPropValue == null || !_formInstanceDataMap.TryGetValue(Convert.ToInt64(idPropValue), out var row))
            throw new InvalidOperationException("Instance not found in data map.");

        foreach (var prop in DynamicType!.GetProperties())
        {
            var newValue = prop.GetValue(instance) ?? DBNull.Value;

            if (newValue is DateTime newDateValue)
                row[prop.Name] = newDateValue == default ? "1900-01-01" : newDateValue.ToString("yyyy-MM-dd");
            else if (newValue == DBNull.Value && prop.PropertyType == typeof(DateTime))
                row[prop.Name] = "1900-01-01";
            else if (!Equals(row[prop.Name], newValue)) row[prop.Name] = newValue;
        }

        UpdateRow?.Invoke(row);
    }

    /// <summary>
    ///     Deletes a row from the DataTable associated with the specified instance.
    /// </summary>
    /// <param name="instance">The object instance whose corresponding row should be deleted.</param>
    /// <exception cref="InvalidOperationException">Thrown if the instance is not found in the data map.</exception>
    public void DeleteDataRow(object instance)
    {
        var idProp = GetIdProperty();
        var idPropValue = idProp?.GetValue(instance);
        if (idPropValue == null || !_formInstanceDataMap.TryGetValue(Convert.ToInt64(idPropValue), out var row))
            throw new InvalidOperationException("Instance not found in data map.");

        DeleteRow?.Invoke(row);
        _dataTable.Rows.Remove(row);
        _formInstanceDataMap.Remove(Convert.ToInt64(idPropValue));
    }

    /// <summary>
    ///     Retrieves the DataTable managed by this service.
    /// </summary>
    /// <returns>The <see cref="DataTable" /> containing data.</returns>
    public DataTable GetDataTable()
    {
        return _dataTable;
    }

    /// <summary>
    ///     Retrieves the DataRow associated with a given object instance.
    /// </summary>
    /// <param name="id">The id of the DataRow required.</param>
    /// <returns>The corresponding <see cref="DataRow" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the instance is not found in the data map.</exception>
    public DataRow GetDataRowById(long id)
    {
        return _formInstanceDataMap.TryGetValue(id, out var row)
            ? row
            : throw new InvalidOperationException("Row not found.");
    }

    /// <summary>
    ///     Inspects the structure of the dynamically generated class, providing details about its properties
    ///     and any custom attributes applied to them.
    /// </summary>
    /// <returns>
    ///     A formatted string containing information about the generated class, including its name,
    ///     properties, and any relevant metadata from custom attributes such as <see cref="FieldWithAttributes" />.
    ///     If no class has been generated, returns a message indicating that no class is available for inspection.
    /// </returns>
    /// <remarks>
    ///     This method is useful for examining the generated class structure at runtime, allowing insight into
    ///     the property types and associated UI metadata that were defined through JSON configuration.
    /// </remarks>
    public string InspectClassStructure()
    {
        var sb = new StringBuilder();

        if (DynamicType == null)
        {
            sb.AppendLine("No class generated yet.");
            return sb.ToString();
        }

        sb.AppendLine($"Generated Class: {DynamicType.Name}");
        foreach (var prop in DynamicType.GetProperties())
        {
            sb.AppendLine($"Property: {prop.Name}, Type: {prop.PropertyType}");

            // Check for custom attributes and display them
            var attribute = prop.GetCustomAttribute<FieldWithAttributes>();
            if (attribute == null) continue;

            sb.AppendLine($"  - ControlType: {attribute.ControlType}");
            sb.AppendLine($"  - Placeholder: {attribute.Placeholder}");
            sb.AppendLine($"  - IsRequired: {attribute.IsRequired}");
            sb.AppendLine($"  - Options: {string.Join(", ", attribute.Options)}");
            sb.AppendLine($"  - ControlParameters: {JsonSerializer.Serialize(attribute.ControlParameters)}");
        }

        return sb.ToString();
    }
    
    /// <summary>
    ///     Retrieves the property representing the unique identifier field of the dynamic type,
    ///     typically named "ID".
    ///     If a property with a different casing is found (e.g., "id" or "ID"),
    ///     a warning is logged indicating the correct convention is to use "ID".
    /// </summary>
    /// <returns>
    ///     The <see cref="PropertyInfo" /> for the identifier field if it exists; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     The identifier field is conventionally named "ID" to maintain compatibility with SQLite,
    ///     which is case-sensitive with primary keys. This method will still return identifier fields
    ///     named "id" or "ID" but logs a warning to encourage exact naming.
    /// </remarks>
    private PropertyInfo? GetIdProperty()
    {
        var idProperty = DynamicType?.GetProperties().FirstOrDefault(prop =>
            string.Equals(prop.Name, "Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty != null && !string.Equals(idProperty.Name, "Id"))
        {
            // Log.Error("Warning: 'Id' property should be named exactly as 'Id'.");
        }
        return idProperty;
    }

    /// <summary>
    ///     Validates that the schema defined by <see cref="PropertyDefinition" /> objects meets required conventions,
    ///     particularly that it includes an exact "ID" property,
    ///     which is of type <see cref="System.Int32" /> or
    ///     <see cref="System.Int64" />.
    ///     Additional validation ensures all required properties are configured correctly for use in the application.
    /// </summary>
    /// <param name="properties">
    ///     A <see cref="List{T}" /> of <see cref="PropertyDefinition" /> objects that describe the structure of the dynamic
    ///     class.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the schema does not include a properly configured "ID" field,
    ///     or if required fields are misconfigured.
    /// </exception>
    /// <remarks>
    ///     This method enforces specific naming conventions, type compatibility, and configuration requirements:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The "ID" property must exist, be named exactly as "ID",
    ///                 and be of a compatible type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Each property must have a valid <see cref="PropertyDefinition.Name" /> and
    ///                 <see cref="PropertyDefinition.TypeName" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Properties marked as required should not be read-only and must specify a
    ///                 <see cref="PropertyDefinition.ControlType" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>ComboBox properties require a non-empty set of <see cref="PropertyDefinition.Options" />.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static void ValidateSchema(List<PropertyDefinition> properties)
    {
        // Ensure an 'ID' property exists with the exact name 'ID' and compatible type
        var idField = properties.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
        if (idField == null)
            throw new ArgumentException("Schema must include an 'Id' property with the correct naming ('Id').");

        if (idField.Type != typeof(int) && idField.Type != typeof(long))
            throw new ArgumentException(
                "The 'Id' property must be of type int or long to be compatible with SQLite primary keys.");

        if (!string.Equals(idField.Name, "Id", StringComparison.Ordinal))
            throw new ArgumentException("The 'Id' property must be named exactly as 'Id' for SQLite compatibility.");

        // Additional validation for other properties in the schema
        foreach (var property in properties)
        {
            // Check that each property has a valid Name and TypeName
            if (string.IsNullOrWhiteSpace(property.Name) || string.IsNullOrWhiteSpace(property.TypeName))
                throw new ArgumentException($"Property '{property.Name}' must have a valid Name and TypeName.");

            // Validate ControlType presence and configuration
            if (string.IsNullOrWhiteSpace(property.ControlType))
                throw new ArgumentException($"Property '{property.Name}' must specify a ControlType.");

            // Check that required fields are not set to read-only, except for 'ID'
            if (property.IsRequired &&
                !string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase) &&
                property.ControlParameters != null &&
                property.ControlParameters.TryGetValue("IsReadOnly", out var isReadOnly))
            {
                // Safely convert JsonElement to bool
                var isReadOnlyBool = isReadOnly is JsonElement { ValueKind: JsonValueKind.True };
                if (isReadOnlyBool)
                    throw new ArgumentException($"Required field '{property.Name}' cannot be read-only.");
            }

            // Enforce specific constraints for ComboBox controls
            if (property.ControlType == "ComboBox" && (property.Options == null || !property.Options.Any()))
                throw new ArgumentException($"Property '{property.Name}' of type 'ComboBox' must include Options.");
        }
    }
}