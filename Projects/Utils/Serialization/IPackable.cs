using System;
using System.Collections.Generic;

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
		// enumerate the source exactly once - it may not be repeatable
		List<IPackage> packages = packables is ICollection<IPackable> collection
			? new List<IPackage>(collection.Count)
			: new List<IPackage>();

		foreach (IPackable element in packables)
			packages.Add(element.Pack());

		return packages.ToArray();
	}
	public static IPackage[] PackAll<TPackable>(this IEnumerable<TPackable> packables)
		where TPackable : IPackable
	{
		// enumerate the source exactly once - it may not be repeatable
		List<IPackage> packages = packables is ICollection<TPackable> collection
			? new List<IPackage>(collection.Count)
			: new List<IPackage>();

		foreach (TPackable element in packables)
			packages.Add(element.Pack());

		return packages.ToArray();
	}
	public static TPackage[] PackAll<TPackable, TPackage>(this IEnumerable<TPackable> packables)
		where TPackable : IPackable
		where TPackage : IPackage
	{
		// enumerate the source exactly once - it may not be repeatable
		List<TPackage> packages = packables is ICollection<TPackable> collection
			? new List<TPackage>(collection.Count)
			: new List<TPackage>();

		foreach (TPackable element in packables)
			packages.Add(element.Pack<TPackage>());

		return packages.ToArray();
	}

	public static TPackable Unpack<TPackable>(this IPackage package)
		where TPackable : IPackable
	{
		return (TPackable)package.Unpack();
	}
	public static IPackable[] UnpackAll(this IEnumerable<IPackage> packages)
	{
		// enumerate the source exactly once - it may not be repeatable
		List<IPackable> packables = packages is ICollection<IPackage> collection
			? new List<IPackable>(collection.Count)
			: new List<IPackable>();

		foreach (IPackage package in packages)
			packables.Add(package.Unpack());

		return packables.ToArray();
	}
	public static TPackable[] UnpackAll<TPackable>(this IEnumerable<IPackage> packages)
		where TPackable : IPackable
	{
		// enumerate the source exactly once - it may not be repeatable
		List<TPackable> packables = packages is ICollection<IPackage> collection
			? new List<TPackable>(collection.Count)
			: new List<TPackable>();

		foreach (IPackage package in packages)
			packables.Add(package.Unpack<TPackable>());

		return packables.ToArray();
	}
	public static TPackable[] UnpackAll<TPackage, TPackable>(this IEnumerable<TPackage> packages)
		where TPackage : IPackage
		where TPackable : IPackable
	{
		// enumerate the source exactly once - it may not be repeatable
		List<TPackable> packables = packages is ICollection<TPackage> collection
			? new List<TPackable>(collection.Count)
			: new List<TPackable>();

		foreach (TPackage package in packages)
			packables.Add(package.Unpack<TPackable>());

		return packables.ToArray();
	}
}
