using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Armat.Serialization;

// object needing serialization to implement this interface
public interface IPackable
{
	// return a serializable object with the necessary fields only
	IPackage Pack();
}

// serialized packages to implement this interface
public interface IPackage
{
	// de-serialize the package and instantiate the DTO object within
	IPackable Unpack();
}

// represents helper extension method for IPackable and IPackage classes
public static class SerializationExtensions
{
	public static TPackage Pack<TPackage>(this IPackable package) where TPackage : IPackage
	{
		return (TPackage)package.Pack();
	}
	public static IPackage[] PackAll(this IEnumerable<IPackable> packables)
	{
		IPackage[] packages = new IPackage[packables.Count()];

		Int32 index = 0;
		foreach (IPackable element in packables)
			packages[index++] = element.Pack();

		return packages;
	}
	public static IPackage[] PackAll<TPackable>(this IEnumerable<TPackable> packables)
		where TPackable : IPackable
	{
		IPackage[] packages = new IPackage[packables.Count()];

		Int32 index = 0;
		foreach (TPackable element in packables)
			packages[index++] = element.Pack();

		return packages;
	}
	public static TPackage[] PackAll<TPackable, TPackage>(this IEnumerable<TPackable> packables)
		where TPackable : IPackable
		where TPackage : IPackage
	{
		TPackage[] packages = new TPackage[packables.Count()];

		Int32 index = 0;
		foreach (IPackable element in packables)
			packages[index++] = element.Pack<TPackage>();

		return packages;
	}

	public static TPackable Unpack<TPackable>(this IPackage package)
		where TPackable : IPackable
	{
		return (TPackable)package.Unpack();
	}
	public static IPackable[] UnpackAll(this IEnumerable<IPackage> packages)
	{
		IPackable[] packables = new IPackable[packages.Count()];

		Int32 index = 0;
		foreach (IPackage package in packages)
			packables[index++] = package.Unpack();

		return packables;
	}
	public static TPackable[] UnpackAll<TPackable>(this IEnumerable<IPackage> packages)
		where TPackable : IPackable
	{
		TPackable[] packables = new TPackable[packages.Count()];

		Int32 index = 0;
		foreach (IPackage package in packages)
			packables[index++] = package.Unpack<TPackable>();

		return packables;
	}
	public static TPackable[] UnpackAll<TPackage, TPackable>(this IEnumerable<TPackage> packages)
		where TPackage : IPackage
		where TPackable : IPackable
	{
		TPackable[] packables = new TPackable[packages.Count()];

		Int32 index = 0;
		foreach (TPackage package in packages)
			packables[index++] = package.Unpack<TPackable>();

		return packables;
	}
}
