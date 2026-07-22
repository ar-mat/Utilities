using System;
using System.Xml;

namespace Armat.Serialization;

// This is a simple utility class to read and write an XML element
// within a given file path and at a given XML element path
public class XmlFileElementReference
{
	public XmlFileElementReference(String filePath, String elementPath)
	{
		FilePath = filePath;
		ElementPath = elementPath;
	}

	// path to a file
	public String FilePath { get; set; } = String.Empty;
	// XPath to Xml element
	public String ElementPath { get; set; } = String.Empty;

	// deserialization from a string
	public static XmlFileElementReference FromString(String value)
	{
		if (String.IsNullOrEmpty(value))
			throw new FormatException();

		Int32 sepIndex = value.IndexOf(';', StringComparison.InvariantCulture);
		if (sepIndex == -1)
			throw new FormatException();

		String filePath = sepIndex > 0 ? value[..sepIndex] : String.Empty;
		String elementPath = sepIndex < value.Length - 1 ? value[(sepIndex + 1)..] : String.Empty;

		return new XmlFileElementReference(filePath, elementPath);
	}
	// serialization into a string
	public override String ToString()
	{
		return FilePath + ";" + ElementPath;
	}

	// load the XmlElement from the referred FilePath and ElementPath
	// will return null if not found
	public XmlElement? LoadXmlElement()
	{
		// check if the file exists
		if (!System.IO.File.Exists(FilePath))
			return null;

		// load the document
		XmlDocument xmlDoc = new();
		xmlDoc.Load(FilePath);

		// find the referenced element
		XmlNode? xmlElement = xmlDoc.SelectSingleNode(ElementPath);
		return (XmlElement?)xmlElement;
	}
	// save the XmlElement into the referred FilePath and ElementPath
	// providing a null argument will remove the appropriate Xml element from file
	// xmlElement name must match to the ElementPath
	public void SaveXmlElement(XmlElement? xmlElement)
	{
		XmlDocument xmlDoc = new();

		// load the document if exists
		if (System.IO.File.Exists(FilePath))
			xmlDoc.Load(FilePath);

		// path levels; ignore empty entries caused by a leading '/'
		String[] xmlLevels = ElementPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (xmlLevels.Length == 0)
			throw new FormatException("ElementPath is empty");

		// check whether the given element name matches the last XPath level
		if (xmlElement != null && xmlElement.Name != xmlLevels[^1])
			throw new FormatException("Xml Element name does not match the XPath");

		Boolean isDocChanged = false;

		// remove the existing element if found
		XmlNode? xmlElementCurrent = xmlDoc.SelectSingleNode(ElementPath);
		if (xmlElementCurrent != null && xmlElementCurrent.ParentNode != null)
		{
			xmlElementCurrent.ParentNode.RemoveChild(xmlElementCurrent);
			isDocChanged = true;
		}

		if (xmlElement != null)
		{
			// find / create the parent node to attach the element to
			XmlNode xmlElementParent;

			if (xmlLevels.Length == 1)
			{
				// the element is the document root
				xmlElementParent = xmlDoc;
			}
			else
			{
				// ensure the root element exists and matches the first path level
				XmlElement? xmlCursor = xmlDoc.DocumentElement;
				if (xmlCursor == null)
				{
					xmlCursor = xmlDoc.CreateElement(xmlLevels[0]);
					xmlDoc.AppendChild(xmlCursor);
					isDocChanged = true;
				}
				else if (xmlCursor.Name != xmlLevels[0])
				{
					throw new FormatException("Xml document root does not match the XPath");
				}

				// walk / create intermediate levels (excluding the root and the element itself)
				for (Int32 level = 1; level < xmlLevels.Length - 1; level++)
				{
					XmlNode? xmlNext = xmlCursor.SelectSingleNode(xmlLevels[level]);
					if (xmlNext == null)
					{
						xmlNext = xmlDoc.CreateElement(xmlLevels[level]);
						xmlCursor.AppendChild(xmlNext);
						isDocChanged = true;
					}
					xmlCursor = (XmlElement)xmlNext;
				}

				xmlElementParent = xmlCursor;
			}

			// include the given element under the parent one
			xmlElement = (XmlElement)xmlDoc.ImportNode(xmlElement, true);
			xmlElementParent.AppendChild(xmlElement);
			isDocChanged = true;
		}

		// create / update the file
		if (isDocChanged)
			xmlDoc.Save(FilePath);
	}
}
