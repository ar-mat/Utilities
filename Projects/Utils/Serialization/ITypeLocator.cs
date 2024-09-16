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
	Type? GetType(String className);
	Type? GetType(String className, String assemblyName);
}

internal class DefaultTypeLocator : ITypeLocator
{
	public static ITypeLocator Instance { get; } = new DefaultTypeLocator();

	public virtual Type? GetType(String className)
	{
		return Type.GetType(className, false);
	}

	public virtual Type? GetType(String className, String assemblyName)
	{
		Assembly? foundAssembly = null;

		// find the right assembly
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foundAssembly = assemblies.FirstOrDefault(asm =>
			{
				return asm.FullName == assemblyName || asm.GetName().Name == assemblyName;
			});
		if (foundAssembly == null)
			return null;

		// find an existing class
		return foundAssembly.GetType(className, false);
	}
}
