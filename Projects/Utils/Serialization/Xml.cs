
using System;
using System.IO;
using System.Xml;

namespace Armat.Serialization
{
	public static class XmlSerializer
	{
		public static String ToString<T>(T? data, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlElement? xmlRoot = ToElement<T>(data, xmlElementName, settings);
			if (xmlRoot == null)
				return String.Empty;

			return xmlRoot.OuterXml;
		}
		public static String ToString(Object? data, Type objectType, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlElement? xmlRoot = ToElement(data, objectType, xmlElementName, settings);
			if (xmlRoot == null)
				return String.Empty;

			return xmlRoot.OuterXml;
		}
		public static Boolean ToFile<T>(String filePath, T? data, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlDocument? xmlDoc = ToDocument<T>(data, xmlElementName, settings);
			if (xmlDoc == null)
				return false;

			xmlDoc.Save(filePath);
			return true;
		}
		public static Boolean ToFile(String filePath, Object? data, Type objectType, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlDocument? xmlDoc = ToDocument(data, objectType, xmlElementName, settings);
			if (xmlDoc == null)
				return false;

			xmlDoc.Save(filePath);
			return true;
		}
		public static XmlDocument? ToDocument<T>(T? data, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlElement? xmlRoot = ToElement<T>(data, xmlElementName, settings);
			if (xmlRoot == null)
				return null;

			return xmlRoot.OwnerDocument;
		}
		public static XmlDocument? ToDocument(Object? data, Type objectType, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			XmlElement? xmlRoot = ToElement(data, objectType, xmlElementName, settings);
			if (xmlRoot == null)
				return null;

			return xmlRoot.OwnerDocument;
		}
		public static XmlElement? ToElement<T>(T? data, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			Type objectType = data == null ? typeof(T) : data.GetType();
			return ToElement((Object?)data, objectType, xmlElementName, settings);
		}
		public static XmlElement? ToElement(Object? data, Type objectType, String xmlElementName = "", XmlWriterSettings? settings = null)
		{
			if (data == null)
				return null;

			// determine XML element name if not explicitly specified
			if (xmlElementName.Length == 0)
			{
				xmlElementName = objectType.Name;

				// arrays handling
				xmlElementName = xmlElementName.Replace("[]", "s", StringComparison.Ordinal);
			}

			// determine object assembly name and the type
			String? assemblyName = objectType.Assembly.GetName().FullName;
			String? typeName = objectType.FullName;
			if (assemblyName == null || typeName == null)
				throw new TypeAccessException();

			// add object assembly and type information
			XmlDocument xmlDoc = new();

			XmlElement xmlRoot = xmlDoc.CreateElement(xmlElementName);
			xmlRoot.SetAttribute("Assembly", assemblyName);
			xmlRoot.SetAttribute("TypeName", typeName);

			// do not write XML declaration tags as it will be nested in another XML document
			settings ??= new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;

			// serialize XML data
			using StringWriter stringWriter = new();
			using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);

			System.Xml.Serialization.XmlSerializer serializer = new(data.GetType());
			serializer.Serialize(xmlWriter, data);
			xmlWriter.Flush();

			// convert the result to string and append to the resulting XML
			String xmlData = stringWriter.ToString();
			xmlRoot.InnerXml = xmlData;
			xmlDoc.AppendChild(xmlRoot);

			return xmlRoot;
		}

		public static T? FromString<T>(String xml, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromString<T>(xml, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static T? FromString<T>(String xml, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return (T?)FromString(xml, typeLocator, xmlElementName, settings);
		}
		public static Object? FromString(String xml, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromString(xml, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static Object? FromString(String xml, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			if (xml.Length == 0)
				return null;

			XmlDocument xmlDoc = new();
			xmlDoc.LoadXml(xml);

			return FromDocument(xmlDoc, typeLocator, xmlElementName, settings);
		}

		public static T? FromFile<T>(String filePath, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromFile<T>(filePath, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static T? FromFile<T>(String filePath, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return (T?)FromFile(filePath, typeLocator, xmlElementName, settings);
		}
		public static Object? FromFile(String filePath, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromFile(filePath, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static Object? FromFile(String filePath, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			if (filePath.Length == 0)
				throw new FileNotFoundException();

			XmlDocument xmlDoc = new();
			xmlDoc.Load(filePath);

			return FromDocument(xmlDoc, typeLocator, xmlElementName, settings);
		}

		public static T? FromDocument<T>(XmlDocument? xmlDoc, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromDocument<T?>(xmlDoc, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static T? FromDocument<T>(XmlDocument? xmlDoc, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return (T?)FromDocument(xmlDoc, typeLocator, xmlElementName, settings);
		}
		public static Object? FromDocument(XmlDocument? xmlDoc, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromDocument(xmlDoc, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static Object? FromDocument(XmlDocument? xmlDoc, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			if (xmlDoc == null)
				return null;

			XmlElement? xmlRoot = xmlDoc.DocumentElement;

			return FromElement(xmlRoot, typeLocator, xmlElementName, settings);
		}

		public static T? FromElement<T>(XmlElement? xmlRoot, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromElement<T>(xmlRoot, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static T? FromElement<T>(XmlElement? xmlRoot, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return (T?)FromElement(xmlRoot, typeLocator, xmlElementName, settings);
		}
		public static Object? FromElement(XmlElement? xmlRoot, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			return FromElement(xmlRoot, DefaultTypeLocator.Instance, xmlElementName, settings);
		}
		public static Object? FromElement(XmlElement? xmlRoot, ITypeLocator typeLocator, String xmlElementName = "", XmlReaderSettings? settings = null)
		{
			if (xmlRoot == null)
				return null;
			if (typeLocator == null)
				throw new ArgumentNullException(nameof(typeLocator));

			// verify XML element name
			if (!String.IsNullOrEmpty(xmlElementName) && xmlRoot.Name != xmlElementName)
				throw new FormatException("Xml element name mismatch");

			// get assembly and 
			String assemblyName = xmlRoot.GetAttribute("Assembly");
			String typeName = xmlRoot.GetAttribute("TypeName");

			// locate the data type based on the assembly name and type name
			Type? objectType = typeLocator.GetType(typeName, assemblyName);
			if (objectType == null)
				throw new TypeLoadException();

			// extract XML data
			String xmlData = xmlRoot.InnerXml;
			if (xmlData.Length == 0)
				return null;

			// de-serialize object from XML string
			using StringReader stringReader = new(xmlData);
			using XmlReader xmlReader = XmlReader.Create(stringReader, settings);

			System.Xml.Serialization.XmlSerializer serializer = new(objectType);
			Object? obj = serializer.Deserialize(xmlReader);

			return obj;
		}
	}
}
