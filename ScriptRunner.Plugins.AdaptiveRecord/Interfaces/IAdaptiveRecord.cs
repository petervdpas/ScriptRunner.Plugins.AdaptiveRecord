using System;
using System.Data;

namespace ScriptRunner.Plugins.AdaptiveRecord.Interfaces;

/// <summary>
///     Interface for managing adaptive records with dynamic class generation and data manipulation capabilities.
/// </summary>
public interface IAdaptiveRecord
{
    /// <summary>
    ///     Gets the dynamically generated class type based on the schema defined in JSON.
    /// </summary>
    /// <value>
    ///     The <see cref="Type" /> representing the dynamically created class,
    ///     or <c>null</c> if the class has not yet been generated.
    /// </value>
    Type? DynamicType { get; }

    /// <summary>
    ///     Creates a dynamic class from a JSON string that defines the properties.
    /// </summary>
    /// <param name="jsonString">The JSON string describing class properties.</param>
    /// <returns>The <see cref="Type" /> of the dynamically created class.</returns>
    /// <exception cref="ArgumentException">Thrown if the JSON format is invalid.</exception>
    Type CreateClassFromJson(string jsonString);

    /// <summary>
    ///     Fetches data rows from an external source and populates the internal DataTable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the fetch callback is not set.</exception>
    void FetchDataRows();

    /// <summary>
    ///     Updates a row in the DataTable with values from the specified instance.
    /// </summary>
    /// <param name="instance">The object instance containing updated property values.</param>
    /// <exception cref="ArgumentException">Thrown if the instance does not match the expected type.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the instance is not found in the data map.</exception>
    void UpdateDataRow(object instance);

    /// <summary>
    ///     Adds a new row to the DataTable using values from the specified instance.
    /// </summary>
    /// <param name="instance">The object instance containing data for the new row.</param>
    void AddDataRow(object instance);

    /// <summary>
    ///     Deletes a row from the DataTable associated with the specified instance.
    /// </summary>
    /// <param name="instance">The object instance whose corresponding row should be deleted.</param>
    /// <exception cref="InvalidOperationException">Thrown if the instance is not found in the data map.</exception>
    void DeleteDataRow(object instance);

    /// <summary>
    ///     Retrieves the DataTable managed by this adaptive record instance.
    /// </summary>
    /// <returns>The <see cref="DataTable" /> containing data.</returns>
    DataTable GetDataTable();

    /// <summary>
    ///     Retrieves the DataRow associated with a given identifier.
    /// </summary>
    /// <param name="id">The identifier of the DataRow to retrieve.</param>
    /// <returns>The corresponding <see cref="DataRow" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the identifier is not found in the data map.</exception>
    DataRow GetDataRowById(long id);

    /// <summary>
    ///     Inspects the structure of the dynamically generated class, providing details about its properties
    ///     and any custom attributes applied to them.
    /// </summary>
    /// <returns>
    ///     A formatted string containing information about the generated class, including its name,
    ///     properties, and any relevant metadata from custom attributes.
    /// </returns>
    string InspectClassStructure();
}

/// <summary>
///     Delegate for fetching data from an external source.
/// </summary>
/// <returns>A <see cref="DataTable" /> containing the fetched data.</returns>
public delegate DataTable FetchDataCallback();

/// <summary>
///     Delegate for updating a row in the DataTable.
/// </summary>
/// <param name="row">The <see cref="DataRow" /> to update.</param>
public delegate void UpdateRowCallback(DataRow row);

/// <summary>
///     Delegate for adding a row to the DataTable.
/// </summary>
/// <param name="row">The <see cref="DataRow" /> to add.</param>
public delegate void AddRowCallback(DataRow row);

/// <summary>
///     Delegate for deleting a row from the DataTable.
/// </summary>
/// <param name="row">The <see cref="DataRow" /> to delete.</param>
public delegate void DeleteRowCallback(DataRow row);