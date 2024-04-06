using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Armat.Collections
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Naming is ok for Unit Test classes")]
	public class IndexedListUnitTest_Simple
	{
		public IndexedListUnitTest_Simple()
		{
		}

		private IList<Int32> CreateList(Type type)
		{
			IList<Int32>? list = Activator.CreateInstance(type) as IList<Int32>;
			if (list == null)
				throw new ArgumentException("Invalid list type is specified", nameof(type));

			return list;
		}

		//private void ResetList()
		//{
		//	_list = null;
		//}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestAppend(Type type)
		{
			Append(CreateList(type));
		}
		private void Append(IList<Int32> list)
		{
			Int32 index = 0;

			list.Add(1);
			list.Add(2);
			list.Add(4);
			list.Add(8);
			// 1, 2, 4, 8

			Assert.True(list.Count == 4);
			Assert.True(list[index++] == 1);
			Assert.True(list[index++] == 2);
			Assert.True(list[index++] == 4);
			Assert.True(list[index++] == 8);

			list.Insert(list.Count, 1000_1);
			list.Insert(list.Count, 1000_2);
			list.Insert(list.Count, 1000_4);
			list.Insert(list.Count, 1000_8);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8

			Assert.True(list.Count == 8);
			Assert.True(list[index++] == 1000_1);
			Assert.True(list[index++] == 1000_2);
			Assert.True(list[index++] == 1000_4);
			Assert.True(list[index++] == 1000_8);

			list.Add(1000_1000_1);
			list.Add(1000_1000_2);
			list.Add(1000_1000_4);
			list.Add(1000_1000_8);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			Assert.True(list.Count == 12);
			Assert.True(list[index++] == 1000_1000_1);
			Assert.True(list[index++] == 1000_1000_2);
			Assert.True(list[index++] == 1000_1000_4);
			Assert.True(list[index++] == 1000_1000_8);
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestRemoveTail(Type type)
		{
			RemoveTail(CreateList(type));
		}
		private void RemoveTail(IList<Int32> list)
		{
			// preparation for the test
			Append(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			list.RemoveAt(list.Count - 1);
			list.RemoveAt(list.Count - 1);
			list.RemoveAt(list.Count - 1);
			list.RemoveAt(list.Count - 1);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8

			Int32 index = list.Count - 1;
			Assert.True(list.Count == 8);
			Assert.True(list[index++] == 1000_8);

			list.Add(1000_1000_1);
			list.Insert(list.Count, 1000_1000_2);
			list.Add(1000_1000_4);
			((IList)list).Add(1000_1000_8);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			Assert.True(list.Count == 12);
			Assert.True(list[index++] == 1000_1000_1);
			Assert.True(list[index++] == 1000_1000_2);
			Assert.True(list[index++] == 1000_1000_4);
			Assert.True(list[index++] == 1000_1000_8);

			list.RemoveAt(list.Count - 1);
			list.RemoveAt(list.Count - 1);
			Assert.True(list[list.Count - 1] == 1000_1000_2);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestUpdate(Type type)
		{
			Update(CreateList(type));
		}
		private void Update(IList<Int32> list)
		{
			// preparation for the test
			Append(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			list[4] = 2000_1;
			list[7] = 2000_8;
			list[8] = 2000_2000_1;
			list[11] = 2000_2000_8;

			Assert.True(list.Count == 12);
			Assert.True(list[3] == 8);
			Assert.True(list[4] == 2000_1);
			Assert.True(list[5] == 1000_2);
			Assert.True(list[6] == 1000_4);
			Assert.True(list[7] == 2000_8);
			Assert.True(list[8] == 2000_2000_1);
			Assert.True(list[9] == 1000_1000_2);
			Assert.True(list[10] == 1000_1000_4);
			Assert.True(list[11] == 2000_2000_8);
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestInsert(Type type)
		{
			Insert(CreateList(type));
		}
		private void Insert(IList<Int32> list)
		{
			// preparation for the test
			Append(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			// remove mid elements
			list.RemoveAt(4);
			list.RemoveAt(4);
			list.RemoveAt(4);
			list.RemoveAt(4);
			// 1, 2, 4, 8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			Assert.True(list.Count == 8);
			Assert.True(list[3] == 8);
			Assert.True(list[4] == 1000_1000_1);
			Assert.True(list[7] == 1000_1000_8);

			// insert two in teh middle and two from the back
			list.Insert(4, 1000_1);
			((IList)list).Insert(5, 1000_2);
			list.Add(1000_4);
			list.Add(1000_8);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

			Assert.True(list.Count == 12);
			Assert.True(list[3] == 8);
			Assert.True(list[4] == 1000_1);
			Assert.True(list[5] == 1000_2);
			Assert.True(list[6] == 1000_1000_1);
			Assert.True(list[9] == 1000_1000_8);
			Assert.True(list[10] == 1000_4);
			Assert.True(list[11] == 1000_8);
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestInsertAndRemove(Type type)
		{
			InsertAndRemove(CreateList(type));
		}
		private void InsertAndRemove(IList<Int32> list)
		{
			// preparation for the test
			Insert(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

			// insert elements in several positions and remove them
			IList<Int32> listOrig = (IList<Int32>)Activator.CreateInstance(list.GetType(), new Object[] { (ICollection<Int32>)list })!;
			String? strOriginal = list.ToString();

			list.Insert(12, 2000_1000_1);
			list.Insert(3, 2000_1000_2);
			list.Insert(9, 2000_1000_4);
			list.Insert(0, 2000_1000_8);
			// 2000_1000_8, 1, 2, 4, 2000_1000_2, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 2000_1000_4, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8, 2000_1000_1

			list.Remove(2000_1000_1);
			list.Remove(2000_1000_2);
			list.Remove(2000_1000_4);
			list.Remove(2000_1000_8);
			String? strAfterRemove = list.ToString();
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

			Assert.True(list.GetHashCode() == listOrig.GetHashCode());
			Assert.True(String.Equals(strOriginal, strAfterRemove));
			Assert.True(list.Equals(listOrig));
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestMove(Type type)
		{
			Move(CreateList(type));
		}
		private void Move(IList<Int32> list)
		{
			// preparation for the test
			Insert(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

			// move elements back and forth
			if (list is IndexedList<Int32> indexedList)
			{
				indexedList.Move(2, 3);
				indexedList.Move(6, 1);
				indexedList.Move(0, 11);
				indexedList.Move(8, 0);
			}
			else
			{
				Int32 tmp;

				tmp = list[2];
				list.RemoveAt(2);
				list.Insert(3, tmp);

				tmp = list[6];
				list.RemoveAt(6);
				list.Insert(1, tmp);

				tmp = list[0];
				list.RemoveAt(0);
				list.Insert(11, tmp);

				tmp = list[8];
				list.RemoveAt(8);
				list.Insert(0, tmp);
			}
			// 1000_1000_8, 1000_1000_1, 2, 8, 4, 1000_1, 1000_2, 1000_1000_2, 1000_1000_4, 1000_4, 1000_8, 1

			Assert.True(list[0] == 1000_1000_8);
			Assert.True(list[1] == 1000_1000_1);
			Assert.True(list.IndexOf(2) == 2);
			Assert.True(list.IndexOf(8) == 3);
			Assert.True(list.IndexOf(4) == 4);
			Assert.True(list[5] == 1000_1);
			Assert.True(list[9] == 1000_4);
			Assert.True(list[10] == 1000_8);
			Assert.True(list[11] == 1);
		}

		[Theory]
		[InlineData(typeof(IndexedList<Int32>))]
		[InlineData(typeof(IndigentList<Int32>))]
		[InlineData(typeof(ConcurrentList<Int32>))]
		public void TestClear(Type type)
		{
			Clear(CreateList(type));
		}
		private void Clear(IList<Int32> list)
		{
			// preparation for the test
			Append(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

			while (list.Count > 0)
				list.RemoveAt(0);
			// (empty)
			Assert.True(list.Count == 0);

			Insert(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8
			while (list.Count > 0)
				list.RemoveAt(list.Count - 1);
			// (empty)
			Assert.True(list.Count == 0);

			InsertAndRemove(list);
			// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8
			list.Clear();
			// (empty)
			Assert.True(list.Count == 0);
		}
	}
}
