using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Armat.Collections;

public class IndexedList_Indexing
{
	public IndexedList_Indexing()
	{
	}

	private class Info
	{
		public Info()
		{
			_time = DateTime.Now;
			_text = DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat)
				+ "+" + (++_counter).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
		}
		public Info(DateTime dt, String txt)
		{
			_time = dt;
			_text = txt;
		}

		public static Int32 _counter = 0;

		private DateTime _time;
		private String _text;

		public DateTime Time { get => _time; set => _time = value; }
		public String Text { get => _text; set => _text = value; }

		public override Boolean Equals(Object? obj)
		{
			return obj is Info info &&
				   Time == info.Time &&
				   Text == info.Text;
		}

		public override Int32 GetHashCode()
		{
			return HashCode.Combine(Time, Text);
		}

		public override String ToString()
		{
			return $"[Info Time: {Time}, Text: {Text}]";
		}
	}

	private class Data
	{
		private Data()
		{
			_info = new Info();
			_num = 0;
			_text = String.Empty;
			_data = null;
		}
		public Data(Info info, Int32 num, String txt)
		{
			_info = info;
			_num = num;
			_text = txt;
			_data = Empty;
		}
		public Data(Info info, Int32 num, String txt, Data data)
		{
			_info = info;
			_num = num;
			_text = txt;
			_data = data;
		}

		public static Data Empty = new();

		private Info _info;
		private Int32 _num;
		private String _text;
		private Data? _data;

		public Info Info { get => _info; set => _info = value; }
		public Int32 Number { get => _num; set => _num = value; }
		public String Text { get => _text; set => _text = value; }
		public Data? NestedData { get => _data; set => _data = value; }
		public Data NestedDataOrThis { get => _data ?? this; }

		public override Boolean Equals(Object? obj)
		{
			return obj is Data data &&
				   EqualityComparer<Info>.Default.Equals(Info, data.Info) &&
				   Number == data.Number &&
				   Text == data.Text &&
				   EqualityComparer<Data>.Default.Equals(NestedData, data.NestedData);
		}
		public override Int32 GetHashCode()
		{
			return HashCode.Combine(Info, Number, Text, NestedData);
		}
		public override String ToString()
		{
			return $"[Data Info: {Info}, Int: {Number}, String: {Text}, Data: {NestedData}]";
		}
	}

	private IndexedList<Data> CreateList()
	{
		IndexedList<Data> list = new ();

		list.CreateHashIndex<Info>("Info", data => data.Info);
		list.CreateHashIndex<Int32>("Int32", data => data.Number);
		list.CreateHashIndex<String>("String", data => data.Text);
		list.CreateMultiHashIndex<Data>("Data", data => data.NestedDataOrThis);

		return list;
	}

	[Fact]
	public void TestAppend()
	{
		Append(CreateList());
	}
	private void Append(IndexedList<Data> list)
	{
		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		Int32 index = 0;

		list.Add(new Data(new Info(), 1, "1"));
		list.Add(new Data(new Info(), 2, "2"));
		list.Add(new Data(new Info(), 4, "4"));
		list.Add(new Data(new Info(), 8, "8"));
		// 1, 2, 4, 8

		Assert.True(list.Count == 4);
		Assert.True(list[index++].Info != null);
		Assert.True(list[index++].Number == 2);
		Assert.True(list[index++].Text == "4");
		Assert.True(list[index++].NestedData == Data.Empty);

		Assert.True(indexInfo.GetValueOrDefault(new Info()) == null);
		Assert.True(indexInfo.GetValueOrDefault(list[0].Info) == list[0]);
		Assert.True(indexInt.GetValueOrDefault(2) != null);
		Assert.True(indexInt.GetValueOrDefault(2)?.Text == "2");
		Assert.True(indexString.GetValueOrDefault("4") != null);
		Assert.True(indexString.GetValueOrDefault("4")?.Number == 4);
		Assert.True(indexData.GetValueOrDefault(Data.Empty)?.NestedData == Data.Empty);
		Assert.True(indexData.GetCountByKey(Data.Empty) == 4);

		list.Add(new Data(new Info(), 1000_1, "1000_1", list[list.Count - 4]));
		list.Add(new Data(new Info(), 1000_2, "1000_2", list[list.Count - 4]));
		list.Add(new Data(new Info(), 1000_4, "1000_4", list[list.Count - 4]));
		list.Add(new Data(new Info(), 1000_8, "1000_8", list[list.Count - 4]));
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8

		Assert.True(list.Count == 8);
		Assert.True(list[index++].Info != null);
		Assert.True(list[index++].Number == 1000_2);
		Assert.True(list[index++].Text == "1000_4");
		Assert.True(list[index++].NestedData == list[3]);

		Assert.True(indexInfo.GetValueOrDefault(list[4].Info)?.Number == 1000_1);
		Assert.True(Object.Equals(indexInt.GetValueOrDefault(1000_2)?.NestedData, list[1]));
		Assert.True(!Object.Equals(indexString.GetValueOrDefault("1000_4")?.NestedData, Data.Empty));
		Assert.True(Object.Equals(indexData.GetValueOrDefault(new Data(list[3].Info, 8, "8")), list[7]));

		indexInfo.Add(new KeyValuePair<Info, Data>(
			new Info(new DateTime(1000_1000_1), "1000_1000_1"),
			new Data(new Info(new DateTime(1000_1000_1), "1000_1000_1"), 1000_1000_1, "1000_1000_1", list[list.Count - 4])));
		indexInt.Add(new KeyValuePair<Int32, Data>(
			1000_1000_2,
			new Data(new Info(), 1000_1000_2, "1000_1000_2", list[list.Count - 4])));
		indexString.Add(new KeyValuePair<String, Data>(
			"1000_1000_4",
			new Data(new Info(), 1000_1000_4, "1000_1000_4", list[list.Count - 4])));
		indexData.Add(new KeyValuePair<Data, Data>(
			list[list.Count - 4],
			new Data(new Info(), 1000_1000_8, "1000_1000_8", list[list.Count - 4])));
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		Assert.True(list.Count == 12);
		Assert.True(list[index++].Info.Equals(new Info(new DateTime(1000_1000_1), "1000_1000_1")));
		Assert.True(list[index++].Number == 1000_1000_2);
		Assert.True(list[index++].Text == "1000_1000_4");
		Assert.True(list[index++].NestedData == list[list.Count - 5]);

		Assert.True(indexInfo.IndexOf(list[8].Info) == 8);
		Assert.True(list[indexInt.IndexOf(list[9].Number)].Text == "1000_1000_2");
		Assert.True(list[indexString.IndexOf("1000_1000_4")].Info.Equals(list[10].Info));
		Assert.True(new List<Data>(indexData.GetValuesByKey(list[list.Count - 5].NestedDataOrThis)).Count == 1 &&
			new List<Data>(indexData.GetValuesByKey(list[list.Count - 5].NestedDataOrThis))[0].Number == 1000_8);
	}

	[Fact]
	public void TestRemoveTail()
	{
		RemoveTail(CreateList());
	}
	private void RemoveTail(IndexedList<Data> list)
	{
		// preparation for the test
		Append(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		Assert.True(indexInfo.Remove(list[list.Count - 1].Info));
		Assert.True(indexInt.Remove(new KeyValuePair<Int32, Data>(list[list.Count - 1].Number, list[list.Count - 1])));
		Data d1 = list[list.Count - 1];
		Assert.True(indexString.TryGetValue(list[list.Count - 1].Text, out Data? d2) && indexString.Remove(list[list.Count - 1].Text) && d2 == d1);
		Assert.True(indexData.Remove(list[list.Count - 1].NestedDataOrThis));
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8

		Int32 index = list.Count - 1;
		Assert.True(list.Count == 8);
		Assert.True(list[index++].Number == 1000_8);

		indexInfo.Add(new KeyValuePair<Info, Data>(
			new Info(new DateTime(1000_1000_1), "1000_1000_1"),
			new Data(new Info(new DateTime(1000_1000_1), "1000_1000_1"), 1000_1000_1, "1000_1000_1", list[list.Count - 4].NestedDataOrThis)));
		indexInt.Add(new KeyValuePair<Int32, Data>(
			1000_1000_2,
			new Data(new Info(), 1000_1000_2, "1000_1000_2", list[list.Count - 4])));
		indexString.Add(new KeyValuePair<String, Data>(
			"1000_1000_4",
			new Data(new Info(), 1000_1000_4, "1000_1000_4", list[list.Count - 4])));
		indexData.Add(new KeyValuePair<Data, Data>(
			list[list.Count - 4],
			new Data(new Info(), 1000_1000_8, "1000_1000_8", list[list.Count - 4])));
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		Assert.True(list.Count == 12);
		Assert.True(list[index++].Info.Equals(new Info(new DateTime(1000_1000_1), "1000_1000_1")));
		Assert.True(list[index++].Number == 1000_1000_2);
		Assert.True(list[index++].Text == "1000_1000_4");
		Assert.True(list[index++].NestedData == list[list.Count - 5]);

		Assert.True(indexInfo.IndexOf(list[8].Info) == 8);
		Assert.True(list[indexInt.IndexOf(list[9].Number)].Text == "1000_1000_2");
		Assert.True(list[indexString.IndexOf("1000_1000_4")].Info.Equals(list[10].Info));
		Assert.True(new List<Data>(indexData.GetValuesByKey(list[list.Count - 5].NestedDataOrThis)).Count == 1 &&
			new List<Data>(indexData.GetValuesByKey(list[list.Count - 5].NestedDataOrThis))[0].Number == 1000_8);

		list.RemoveAt(list.Count - 1);
		list.RemoveAt(list.Count - 1);
		Assert.True(list[list.Count - 1].Number == 1000_1000_2);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2
	}

	[Fact]
	public void TestUpdate()
	{
		Update(CreateList());
	}
	private void Update(IndexedList<Data> list)
	{
		// preparation for the test
		Append(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		list[4] = new Data(new Info(), 2000_1, "2000_1", list[0]);
		indexInt[1000_8] = new Data(new Info(), 1000_8, "2000_8", list[3]);
		indexString["1000_1000_1"] = new Data(new Info(new DateTime(2000_2000_1), "2000_2000_1"), 2000_2000_1, "1000_1000_1", list[4]);
		list[11] = new Data(new Info(), 2000_2000_8, "2000_2000_8", list[7]);

		Assert.True(list.Count == 12);
		Assert.True(list[3].Number == 8);
		Assert.True(list[4].Number == 2000_1);
		Assert.True(list[5].Number == 1000_2);
		Assert.True(list[6].Number == 1000_4);
		Assert.True(list[7].Number == 1000_8 && list[7].Text == "2000_8");
		Assert.True(list[8].Text == "1000_1000_1" && list[8].Number == 2000_2000_1);
		Assert.True(list[9].Number == 1000_1000_2);
		Assert.True(list[10].Number == 1000_1000_4);
		Assert.True(list[11].Number == 2000_2000_8);
	}

	[Fact]
	public void TestInsert()
	{
		Insert(CreateList());
	}
	private void Insert(IndexedList<Data> list)
	{
		// preparation for the test
		Append(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		// remove mid elements
		list.RemoveAt(4);
		list.RemoveAt(4);
		list.RemoveAt(4);
		list.RemoveAt(4);
		// 1, 2, 4, 8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8

		Assert.True(list.Count == 8);
		Assert.True(list[3].Number == 8 && indexInt[8] == list[3]);
		Assert.True(list[4].Text == "1000_1000_1" && indexString.IndexOf("1000_1000_1") == 4);
		Assert.True(list[7].NestedDataOrThis.Number == 1000_8);

		// insert two in the middle and two from the back
		list.Insert(4, new Data(new Info(), 1000_1, "1000_1", list[0]));
		((IList)list).Insert(5, new Data(new Info(), 1000_2, "1000_2", list[list.Count - 4]));
		list.Add(new Data(new Info(), 1000_4, "1000_4", list[list.Count - 4]));
		list.Add(new Data(new Info(), 1000_8, "1000_8", list[list.Count - 4]));
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

		Assert.True(list.Count == 12);
		Assert.True(list[3].Number == 8);
		Assert.True(list[4].Number == 1000_1);
		Assert.True(list[5].Number == 1000_2);
		Assert.True(list[6].Number == 1000_1000_1);
		Assert.True(list[9].Number == 1000_1000_8);
		Assert.True(list[10].Number == 1000_4);
		Assert.True(list[11].Number == 1000_8);
	}

	[Fact]
	public void TestInsertAndRemove()
	{
		InsertAndRemove(CreateList());
	}
	private void InsertAndRemove(IndexedList<Data> list)
	{
		// preparation for the test
		Insert(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		// insert elements in several positions and remove them back
		IndexedList<Data> listOrig = new(list);
		String strOriginal = list.ToString();

		list.Insert(12, new Data(new Info(), 2000_1000_1, "2000_1000_1"));
		list.Insert(3, new Data(new Info(), 2000_1000_2, "2000_1000_2"));
		list.Insert(9, new Data(new Info(), 2000_1000_4, "2000_1000_4"));
		list.Insert(0, new Data(new Info(), 2000_1000_8, "2000_1000_8"));
		// 2000_1000_8, 1, 2, 4, 2000_1000_2, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 2000_1000_4, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8, 2000_1000_1

		Assert.True(indexInfo[list[15].Info] == list[15]);
		Assert.True(indexInt.IndexOf(2000_1000_2) == 4);
		Assert.True(indexString["2000_1000_4"].Number == 2000_1000_4);
		Assert.True(indexData.GetCountByKey(Data.Empty) == 8);

		indexInt.Remove(2000_1000_1);
		list.Remove(new Data(list[4].Info, 2000_1000_2, "2000_1000_2"));
		list.RemoveAt(indexString.IndexOf("2000_1000_4"));
		list.RemoveAt(0);
		String strAfterRemove = list.ToString();
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

		Assert.True(indexData.GetCountByKey(Data.Empty) == 4);
		Assert.True(list.GetHashCode() == listOrig.GetHashCode());
		Assert.True(strOriginal == strAfterRemove);
		Assert.True(list.Equals(listOrig));
	}

	[Fact]
	public void TestMove()
	{
		Move(CreateList());
	}
	private void Move(IndexedList<Data> list)
	{
		// preparation for the test
		Insert(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8

		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		// move elements back and forth
		list.Move(2, 3);
		list.Move(6, 1);
		list.Move(0, 11);
		list.Move(8, 0);
		// 1000_1000_8, 1000_1000_1, 2, 8, 4, 1000_1, 1000_2, 1000_1000_2, 1000_1000_4, 1000_4, 1000_8, 1

		Assert.True(indexInfo[list[0].Info] == list[0]);
		Assert.True(indexInt.IndexOf(1000_1000_1) == 1);
		Assert.True(indexString["4"].Number == 4);
		Assert.True(indexData.GetCountByKey(Data.Empty) == 4);
	}

	[Fact]
	public void TestClear()
	{
		Clear(CreateList());
	}
	private void Clear(IndexedList<Data> list)
	{
		// preparation for the test
		var indexInfo = list.GetIndex<Info>("Info")!;
		var indexInt = list.GetIndex<Int32>("Int32")!;
		var indexString = list.GetIndex<String>("String")!;
		var indexData = (IMultiIndex<Data, Data>)list.GetIndex("Data")!;

		Append(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8
		while (list.Count > 0)
			list.RemoveAt(0);
		// (empty)
		Assert.True(list.Count == 0);
		Assert.True(indexInfo.Count == 0);
		Assert.True(indexInt.Count == 0);
		Assert.True(indexString.Count == 0);
		Assert.True(indexData.Count == 0);

		Insert(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8
		while (list.Count > 0)
			list.RemoveAt(list.Count - 1);
		// (empty)
		Assert.True(list.Count == 0);
		Assert.True(indexInfo.Count == 0);
		Assert.True(indexInt.Count == 0);
		Assert.True(indexString.Count == 0);
		Assert.True(indexData.Count == 0);

		InsertAndRemove(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8, 1000_4, 1000_8
		list.Clear();
		// (empty)
		Assert.True(list.Count == 0);
		Assert.True(indexInfo.Count == 0);
		Assert.True(indexInt.Count == 0);
		Assert.True(indexString.Count == 0);
		Assert.True(indexData.Count == 0);

		InsertAndRemove(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8
		while (indexInfo.Count > 0)
			indexInfo.Remove(new List<Info>(indexInfo.Keys)[0]);
		// (empty)
		Assert.True(list.Count == 0);
		Assert.True(indexInfo.Count == 0);
		Assert.True(indexInt.Count == 0);
		Assert.True(indexString.Count == 0);
		Assert.True(indexData.Count == 0);

		InsertAndRemove(list);
		// 1, 2, 4, 8, 1000_1, 1000_2, 1000_4, 1000_8, 1000_1000_1, 1000_1000_2, 1000_1000_4, 1000_1000_8
		indexData.Clear();
		// (empty)
		Assert.True(list.Count == 0);
		Assert.True(indexInfo.Count == 0);
		Assert.True(indexInt.Count == 0);
		Assert.True(indexString.Count == 0);
		Assert.True(indexData.Count == 0);
	}
}
