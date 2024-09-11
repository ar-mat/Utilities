
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Armat.Serialization;

public static class JsonSerializer
{
	public static String ToString<T>(T? data, JsonWriterOptions options = default)
	{
		Type objectType = data == null ? typeof(T) : data.GetType();
		return ToString((Object?)data, objectType, options);
	}
	public static String ToString(Object? data, Type objectType, JsonWriterOptions options = default)
	{
		if (data == null)
			return String.Empty;

		// determine object assembly name and the type
		String? assemblyName = objectType.Assembly.GetName().FullName;
		String? typeName = objectType.FullName;
		if (assemblyName == null || typeName == null)
			throw new TypeAccessException();

		// seriaize the data into JSON format
		String jsonData = System.Text.Json.JsonSerializer.Serialize(data, objectType);

		// create JSON writer stream
		System.IO.MemoryStream stream = new();
		using Utf8JsonWriter jsonWriter = new(stream, options);

		// format JSON
		jsonWriter.WriteStartObject();
		{
			jsonWriter.WriteString("Assembly", assemblyName);
			jsonWriter.WriteString("TypeName", typeName);

			jsonWriter.WritePropertyName("JsonData");
			jsonWriter.WriteRawValue(jsonData);
		}
		jsonWriter.WriteEndObject();
		jsonWriter.Flush();

		// convert the result to string
		return System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (Int32)stream.Position);
	}

	public static T? FromString<T>(String json, JsonDocumentOptions options = default)
	{
		return FromString<T>(json, DefaultTypeLocator.Instance, options);
	}
	public static T? FromString<T>(String json, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		return (T?)FromString(json, typeLocator, options);
	}
	public static Object? FromString(String json, JsonDocumentOptions options = default)
	{
		return FromString(json, DefaultTypeLocator.Instance, options);
	}
	public static Object? FromString(String json, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		if (json.Length == 0)
			return null;
		if (typeLocator == null)
			throw new ArgumentNullException(nameof(typeLocator));

		using JsonDocument doc = JsonDocument.Parse(json, options);

		String assemblyName = doc.RootElement.GetProperty("Assembly").ToString();
		String typeName = doc.RootElement.GetProperty("TypeName").ToString();

		// locate the data type based on the assembly name and type name
		Type? objectType = typeLocator.GetType(typeName, assemblyName);
		if (objectType == null)
			throw new TypeLoadException();

		// extract JSON data
		JsonElement jsonDataElement = doc.RootElement.GetProperty("JsonData");
		String jsonData = jsonDataElement.GetRawText();
		if (jsonData.Length == 0)
			return null;

		// deserialize object from JSON string
		Object? obj = System.Text.Json.JsonSerializer.Deserialize(jsonData, objectType);

		return obj;
	}
}
