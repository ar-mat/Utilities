using System;

namespace Armat.Utils.Extensions;

public static class ByteArray
{
	public static Boolean ContentsEquals(this Byte[] current, Byte[] other)
	{
		// arrays of different lengths are never equal
		if (current.Length != other.Length)
			return false;

		return ContentsEquals(current, 0, other, 0, current.Length);
	}

	public static Boolean ContentsEquals(this Byte[] current, Int32 thisIndex, Byte[] other, Int32 otherIndex, Int32 length)
	{
		if (current == other && thisIndex == otherIndex)
			return true;
		if (length == 0)
			return true;
		if (thisIndex < 0 || otherIndex < 0 || length < 0)
			return false;
		if (current.Length < thisIndex + length || other.Length < otherIndex + length)
			return false;

		// check if buffers match
		return current.AsSpan(thisIndex, length).SequenceEqual(other.AsSpan(otherIndex, length));
	}

	public static void ContentsCopy(this Byte[] current, Byte[] other)
	{
		ContentsCopy(current, 0, other, 0, current.Length);
	}

	public static void ContentsCopy(this Byte[] current, Int32 thisIndex, Byte[] other, Int32 otherIndex, Int32 length)
	{
		if (length == 0)
			return;
		if (thisIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(thisIndex));
		if (otherIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(otherIndex));
		if (length < 0 || current.Length < thisIndex + length || other.Length < otherIndex + length)
			throw new ArgumentOutOfRangeException(nameof(length));

		// copy the contents in a single block operation
		Array.Copy(other, otherIndex, current, thisIndex, length);
	}

	public static void ContentsAnd(this Byte[] current, Byte[] other)
	{
		ContentsAnd(current, 0, other, 0, current.Length);
	}

	public static void ContentsAnd(this Byte[] current, Int32 thisIndex, Byte[] other, Int32 otherIndex, Int32 length)
	{
		if (length == 0)
			return;
		if (thisIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(thisIndex));
		if (otherIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(otherIndex));
		if (length < 0 || current.Length < thisIndex + length || other.Length < otherIndex + length)
			throw new ArgumentOutOfRangeException(nameof(length));

		// check if buffers match
		Int32 tailIdx = length - length % sizeof(Int64);

		// check in 8 byte chunks
		for (Int32 i = 0; i < tailIdx; i += sizeof(Int64))
		{
			Int64 currentBlock = BitConverter.ToInt64(current, thisIndex + i);
			Int64 otherBlock = BitConverter.ToInt64(other, otherIndex + i);
			Int64 resultBlock = currentBlock & otherBlock;

			Span<Byte> span = new(current, thisIndex + i, sizeof(Int64));
			BitConverter.TryWriteBytes(span, resultBlock);
		}

		// check the remainder of the array, always shorter than 8 bytes
		for (Int32 i = tailIdx; i < length; i++)
		{
			Byte currentBlock = current[thisIndex + i];
			Byte otherBlock = other[otherIndex + i];
			Byte resultBlock = (Byte)(currentBlock & otherBlock);

			current[thisIndex + i] = resultBlock;
		}
	}

	public static void ContentsOr(this Byte[] current, Byte[] other)
	{
		ContentsOr(current, 0, other, 0, current.Length);
	}

	public static void ContentsOr(this Byte[] current, Int32 thisIndex, Byte[] other, Int32 otherIndex, Int32 length)
	{
		if (length == 0)
			return;
		if (thisIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(thisIndex));
		if (otherIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(otherIndex));
		if (length < 0 || current.Length < thisIndex + length || other.Length < otherIndex + length)
			throw new ArgumentOutOfRangeException(nameof(length));

		// check if buffers match
		Int32 tailIdx = length - length % sizeof(Int64);

		// check in 8 byte chunks
		for (Int32 i = 0; i < tailIdx; i += sizeof(Int64))
		{
			Int64 currentBlock = BitConverter.ToInt64(current, thisIndex + i);
			Int64 otherBlock = BitConverter.ToInt64(other, otherIndex + i);
			Int64 resultBlock = currentBlock | otherBlock;

			Span<Byte> span = new(current, thisIndex + i, sizeof(Int64));
			BitConverter.TryWriteBytes(span, resultBlock);
		}

		// check the remainder of the array, always shorter than 8 bytes
		for (Int32 i = tailIdx; i < length; i++)
		{
			Byte currentBlock = current[thisIndex + i];
			Byte otherBlock = other[otherIndex + i];
			Byte resultBlock = (Byte)(currentBlock | otherBlock);

			current[thisIndex + i] = resultBlock;
		}
	}

	public static void ContentsXor(this Byte[] current, Byte[] other)
	{
		ContentsXor(current, 0, other, 0, current.Length);
	}

	public static void ContentsXor(this Byte[] current, Int32 thisIndex, Byte[] other, Int32 otherIndex, Int32 length)
	{
		if (length == 0)
			return;
		if (thisIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(thisIndex));
		if (otherIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(otherIndex));
		if (length < 0 || current.Length < thisIndex + length || other.Length < otherIndex + length)
			throw new ArgumentOutOfRangeException(nameof(length));

		// check if buffers match
		Int32 tailIdx = length - length % sizeof(Int64);

		// check in 8 byte chunks
		for (Int32 i = 0; i < tailIdx; i += sizeof(Int64))
		{
			Int64 currentBlock = BitConverter.ToInt64(current, thisIndex + i);
			Int64 otherBlock = BitConverter.ToInt64(other, otherIndex + i);
			Int64 resultBlock = currentBlock ^ otherBlock;

			Span<Byte> span = new(current, thisIndex + i, sizeof(Int64));
			BitConverter.TryWriteBytes(span, resultBlock);
		}

		// check the remainder of the array, always shorter than 8 bytes
		for (Int32 i = tailIdx; i < length; i++)
		{
			Byte currentBlock = current[thisIndex + i];
			Byte otherBlock = other[otherIndex + i];
			Byte resultBlock = (Byte)(currentBlock ^ otherBlock);

			current[thisIndex + i] = resultBlock;
		}
	}

	public static void ContentsNot(this Byte[] current)
	{
		ContentsNot(current, 0, current.Length);
	}

	public static void ContentsNot(this Byte[] current, Int32 index, Int32 length)
	{
		if (length == 0)
			return;
		if (index < 0)
			throw new ArgumentOutOfRangeException(nameof(index));
		if (length < 0 || current.Length < index + length)
			throw new ArgumentOutOfRangeException(nameof(length));

		// check if buffers match
		Int32 tailIdx = length - length % sizeof(Int64);

		// check in 8 byte chunks
		for (Int32 i = 0; i < tailIdx; i += sizeof(Int64))
		{
			Int64 currentBlock = BitConverter.ToInt64(current, index + i);
			Int64 resultBlock = ~currentBlock;

			Span<Byte> span = new(current, index + i, sizeof(Int64));
			BitConverter.TryWriteBytes(span, resultBlock);
		}

		// check the remainder of the array, always shorter than 8 bytes
		for (Int32 i = tailIdx; i < length; i++)
		{
			Byte currentBlock = current[index + i];
			Byte resultBlock = (Byte)(~currentBlock);

			current[index + i] = resultBlock;
		}
	}
}
