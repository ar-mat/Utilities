using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Armat.Serialization;

// the interface is used in deseriaizer methods to find / load the appropriate data type
public interface ITypeLocator
{
	Type? GetType(String className, String assemblyName);
}

public class DefaultTypeLocator : ITypeLocator
{
	public static ITypeLocator Instance { get; } = new DefaultTypeLocator();

	public Type? GetType(String className, String assemblyName)
	{
		// find an existing class
		Type? classType = Type.GetType(className, false);
		if (classType == null)
			return null;

		// ensure it belongs to the right assembly
		if (assemblyName.Length != 0)
		{
			Assembly classAsm = classType.Assembly;
			if (classAsm.FullName != assemblyName && classAsm.GetName().Name != assemblyName)
				return null;
		}

		return classType;
	}
}
