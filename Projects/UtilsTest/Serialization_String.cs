using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Armat.Serialization;

public class Serialization_String
{
	public class PackableRectangle : IPackable
	{
		public Int32 Width { get; set; }
		public Int32 Height { get; set; }

		private Int32 _area = Int32.MinValue;
		public Int32 Area
		{
			get
			{
				if (_area == Int32.MinValue)
					_area = Width * Height;
				return _area;
			}
		}

		public IPackage Pack()
		{
			return new PackedRectangle()
			{
				HSize = Width,
				VSize = Height,
				CachedArea = Area
			};
		}

		public class PackedRectangle : IPackage
		{
			public Int32 HSize { get; set; }
			public Int32 VSize { get; set; }
			public Int32 CachedArea { get; set; }

			public IPackable Unpack()
			{
				PackableRectangle rect = new PackableRectangle();
				rect.Width = HSize;
				rect.Height = VSize;
				rect._area = CachedArea;

				return rect;
			}
		}
	}

	[Fact]
	public void XmlSerialization()
	{
		PackableRectangle originalRect = new PackableRectangle()
		{
			Width = 2,
			Height = 3
		};

		String serializedRect = XmlSerializer.ToString<PackableRectangle>(originalRect);
		PackableRectangle? deserializedRect = XmlSerializer.FromString<PackableRectangle>(serializedRect);

		Assert.NotNull(deserializedRect);
		Assert.True(originalRect.Width == deserializedRect.Width);
		Assert.True(originalRect.Height == deserializedRect.Height);
		Assert.True(originalRect.Area == deserializedRect.Area);
	}

	[Fact]
	public void JsonSerialization()
	{
		PackableRectangle originalRect = new PackableRectangle()
		{
			Width = 2,
			Height = 3
		};

		String serializedRect = JsonSerializer.ToString<PackableRectangle>(originalRect);
		PackableRectangle? deserializedRect = JsonSerializer.FromString<PackableRectangle>(serializedRect);

		Assert.NotNull(deserializedRect);
		Assert.True(originalRect.Width == deserializedRect.Width);
		Assert.True(originalRect.Height == deserializedRect.Height);
		Assert.True(originalRect.Area == deserializedRect.Area);
	}

	[Fact]
	public void PackageXmlSerialization()
	{
		PackableRectangle originalRect = new PackableRectangle()
		{
			Width = 2,
			Height = 3
		};

		IPackage? package = originalRect.Pack();
		String serializedRect = XmlSerializer.ToString(package);
		package = XmlSerializer.FromString<IPackage>(serializedRect);
		PackableRectangle? deserializedRect = package?.Unpack<PackableRectangle>();

		Assert.NotNull(deserializedRect);
		Assert.True(originalRect.Width == deserializedRect.Width);
		Assert.True(originalRect.Height == deserializedRect.Height);
		Assert.True(originalRect.Area == deserializedRect.Area);
	}

	[Fact]
	public void PackageJsonSerialization()
	{
		PackableRectangle originalRect = new PackableRectangle()
		{
			Width = 2,
			Height = 3
		};

		IPackage? package = originalRect.Pack();
		String serializedRect = JsonSerializer.ToString(package);
		package = JsonSerializer.FromString<IPackage>(serializedRect);
		PackableRectangle? deserializedRect = package?.Unpack<PackableRectangle>();

		Assert.NotNull(deserializedRect);
		Assert.True(originalRect.Width == deserializedRect.Width);
		Assert.True(originalRect.Height == deserializedRect.Height);
		Assert.True(originalRect.Area == deserializedRect.Area);
	}
}
