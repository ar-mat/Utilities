
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

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
		String result = String.Empty;
		if (data == null)
			return result;

		// write into a memory stream
		System.IO.MemoryStream stream = new();
		Boolean succeeded = ToStream(stream, data, objectType, options);

		// convert the result to string
		if (succeeded)
			result = System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (Int32)stream.Position);

		return result;
	}
	public static Boolean ToFile<T>(String filePath, T? data, JsonWriterOptions options = default)
	{
		// create / reset the file
		FileStream stream = File.OpenWrite(filePath);

		// write into the file
		return ToStream(stream, data, options);
	}
	public static Boolean ToFile(String filePath, Object? data, Type objectType, JsonWriterOptions options = default)
	{
		// create / reset the file
		FileStream stream = File.OpenWrite(filePath);

		// write into the file
		return ToStream(stream, data, objectType, options);
	}
	public static Boolean ToStream<T>(Stream stream, T? data, JsonWriterOptions options = default)
	{
		Type objectType = data == null ? typeof(T) : data.GetType();
		return ToStream(stream, (Object?)data, objectType, options);
	}
	public static Boolean ToStream(Stream stream, Object? data, Type objectType, JsonWriterOptions options = default)
	{
		if (data == null)
			return false;

		// determine object assembly name and the type
		String? assemblyName = objectType.Assembly.GetName().FullName;
		String? typeName = objectType.FullName;
		if (assemblyName == null || typeName == null)
			throw new TypeAccessException();

		// seriaize the data into JSON format
		String jsonData = System.Text.Json.JsonSerializer.Serialize(data, objectType);

		// create JSON writer stream
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

		return true;
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

		using JsonDocument doc = JsonDocument.Parse(json, options);
		JsonSerializerOptions serializeOptions = Convert(options);

		return FromDocument(doc, typeLocator, serializeOptions);
	}
	public static T? FromFile<T>(String filePath, JsonDocumentOptions options = default)
	{
		return FromFile<T>(filePath, DefaultTypeLocator.Instance, options);
	}
	public static T? FromFile<T>(String filePath, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		return (T?)FromFile(filePath, typeLocator, options);
	}
	public static Object? FromFile(String filePath, JsonDocumentOptions options = default)
	{
		return FromFile(filePath, DefaultTypeLocator.Instance, options);
	}
	public static Object? FromFile(String filePath, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		FileStream stream = File.OpenRead(filePath);
		return FromStream(stream, typeLocator, options);
	}
	public static T? FromStream<T>(Stream stream, JsonDocumentOptions options = default)
	{
		return FromStream<T>(stream, DefaultTypeLocator.Instance, options);
	}
	public static T? FromStream<T>(Stream stream, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		return (T?)FromStream(stream, typeLocator, options);
	}
	public static Object? FromStream(Stream stream, JsonDocumentOptions options = default)
	{
		return FromStream(stream, DefaultTypeLocator.Instance, options);
	}
	public static Object? FromStream(Stream stream, ITypeLocator typeLocator, JsonDocumentOptions options = default)
	{
		using JsonDocument doc = JsonDocument.Parse(stream, options);
		JsonSerializerOptions serializeOptions = Convert(options);

		return FromDocument(doc, typeLocator, serializeOptions);
	}
	public static T? FromDocument<T>(JsonDocument doc, JsonSerializerOptions? options = null)
	{
		return FromDocument<T>(doc, DefaultTypeLocator.Instance, options);
	}
	public static T? FromDocument<T>(JsonDocument doc, ITypeLocator typeLocator, JsonSerializerOptions? options = null)
	{
		return (T?)FromDocument(doc, typeLocator, options);
	}
	public static Object? FromDocument(JsonDocument doc, JsonSerializerOptions? options = null)
	{
		return FromDocument(doc, DefaultTypeLocator.Instance, options);
	}
	public static Object? FromDocument(JsonDocument doc, ITypeLocator typeLocator, JsonSerializerOptions? options = null)
	{
		String assemblyName = doc.RootElement.GetProperty("Assembly").ToString();
		String typeName = doc.RootElement.GetProperty("TypeName").ToString();

		// locate the data type based on the assembly name and type name
#pragma warning disable IDE0270 // Use coalesce expression
		Type? objectType = typeLocator.GetType(typeName, assemblyName);
		if (objectType == null)
			throw new TypeLoadException();
#pragma warning restore IDE0270 // Use coalesce expression

		// extract JSON data
		JsonElement jsonDataElement = doc.RootElement.GetProperty("JsonData");
		String jsonData = jsonDataElement.GetRawText();
		if (jsonData.Length == 0)
			return null;

		// deserialize object from JSON string
		Object? result = System.Text.Json.JsonSerializer.Deserialize(jsonData, objectType, options);

		return result;
	}

	private static JsonSerializerOptions Convert(JsonDocumentOptions documentOptions)
	{
		JsonSerializerOptions serializerOptions = new ()
		{
			// Assuming similar behavior for AllowTrailingCommas
			AllowTrailingCommas = documentOptions.AllowTrailingCommas,

			// Assuming similar behavior for CommentHandling
			ReadCommentHandling = documentOptions.CommentHandling switch
			{
				JsonCommentHandling.Disallow => JsonCommentHandling.Disallow,
				JsonCommentHandling.Skip => JsonCommentHandling.Skip,
				JsonCommentHandling.Allow => JsonCommentHandling.Allow,
				_ => JsonCommentHandling.Disallow
			},

			// Assuming similar behavior for MaxDepth
			MaxDepth = documentOptions.MaxDepth
		};

		return serializerOptions;
	}
}
