using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Armat.Serialization;

// This is a simple utility class to read and write and XML element
// within a given file path and at a given XML elemnt path
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

		// find / create the parent element to add the given one below
		XmlElement? xmlElementParent = null;
		Boolean isDocChanged = false;

		// try to find the element in the loaded document
		XmlNode? xmlElementCurrent = xmlDoc.SelectSingleNode(ElementPath);
		if (xmlElementCurrent != null)
		{
			// check whether the given element name matches the found one
			if (xmlElement != null && xmlElement.Name != xmlElementCurrent.Name)
				throw new FormatException("Xml Element name does not match the XPath");

			// remove the current element
			xmlElementParent = (XmlElement?)xmlElementCurrent.ParentNode;
			if (xmlElementParent != null)
			{
				xmlElementParent.RemoveChild(xmlElementCurrent);
				isDocChanged = true;
			}
		}

		// create the element, it's not found
		if (xmlElementParent == null)
		{
			xmlElementParent = xmlDoc.DocumentElement!;
			String[] xmlLevels = ElementPath.Split('/');

			// check whether the given element name matches the xpath
			if (xmlElement != null && xmlElement.Name != xmlLevels[xmlLevels.Length])
				throw new FormatException("Xml Element name does not match the XPath");

			for (Int32 level = 0; level < xmlLevels.Length - 1; level++)
			{
				// find next level element
				String levelElementName = xmlLevels[level];
				xmlElementCurrent = xmlElementParent.SelectSingleNode(levelElementName);

				// create if not found
				if (xmlElementCurrent == null)
				{
					xmlElementCurrent = xmlDoc.CreateElement(levelElementName);
					xmlElementParent.AppendChild(xmlElementCurrent);
					isDocChanged = true;
				}

				// move to the new level
				xmlElementParent = (XmlElement)xmlElementCurrent;
			}
		}

		// include the given element under the parent one
		if (xmlElement != null)
		{
			xmlElement = (XmlElement)xmlDoc.ImportNode(xmlElement, true);
			xmlElementParent.AppendChild(xmlElement);
			isDocChanged = true;
		}

		// create / update the file
		if (isDocChanged)
			xmlDoc.Save(FilePath);
	}
}
