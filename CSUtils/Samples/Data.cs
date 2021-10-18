using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Linq;

namespace Samples.Data
{
  using static Math;

  /// <summary> Base class for Sample Data </summary>
  public abstract class DataColumn : INotifyPropertyChanged, ICloneable
  {
    public readonly string Name;
    public readonly string Units;

    HashSet<string> SuspendedNotifications = new HashSet<string>();
    bool suspendNotify = false;
    public bool SuspendNotify {
      get => suspendNotify;
      set {
        suspendNotify = value;
        if (!value) {
          foreach (string n in SuspendedNotifications) RaisePropertyChanged(n);
          SuspendedNotifications.Clear();
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs ea) => PropertyChanged?.Invoke(this, ea);

    protected void RaisePropertyChanged([CallerMemberName] string property = null)
    {
      if (PropertyChanged == null) return;
      if (SuspendNotify) {
        SuspendedNotifications.Add(property);
      } else {
        OnPropertyChanged(new PropertyChangedEventArgs(property));
      }
    }

    protected int length;
    public int Length {
      get => length;
      protected set {
        if (length != value) {
          length = value;
        }
      }
    }

    public abstract string GetDataAsString(int i);
    object ICloneable.Clone() => Clone();
    public abstract DataColumn Clone();
    public abstract void Clear();


    protected DataColumn(string name, string units)
    {
      Name = name;
      Units = units;
    }
  }

  /// <summary> Variable size double array. Like List(double) </summary>
  public class ValueArray : DataColumn, IList<double>
  {
    double[] values;

    public double FirstValue { get => values[0]; set => values[0] = value; }
    public double LastValue { get => values[Length - 1]; set => values[Length - 1] = value; }

    double normalize = 1.0;
    public double Normalize {
      get => normalize;
      set => normalize = double.IsNaN(value) || value == 0 || double.IsInfinity(value) ? 1.0 : value;
    }

    public int Capacity => values.Length;
    const int MaxCapacity = 0x1000000;
    const int InitialCapacity = 1024;
    const int MinimumCapacity = 16;

    /// <summary>
    /// Copy producing Array with minimum needed Capacity
    /// </summary>
    override public DataColumn Clone() => Clone(0);
    /// <summary>
    /// Copy producing Array with specific Capacity
    /// </summary>
    /// <param name="cap">0 - minimum needed Capacity</param>
    public DataColumn Clone(int cap)
    {
      ValueArray nva = new ValueArray(Name, Units, cap > Length ? cap : Length);
      nva.Length = Length;
      Array.Copy(values, nva.values, Length);
      return nva;
    }
    public void Clone(ValueArray toFill)
    {
      toFill.Length = Length;
      Array.Copy(values, toFill.values, Length);
    }
    public static ValueArray operator +(ValueArray a, ValueArray b)
    {
      ValueArray nva = (ValueArray)a.Clone(a.Length + b.Length);
      nva.Add(double.NaN);
      for (int i = 0; i < b.Length; i++)
        nva.Add(b[i]);
      return nva;
    }
    bool? isSortedUp;
    bool? isSortedDown;
    public bool IsSortedUp {
      get {
        if (isSortedUp == null) CheckSorted();
        return (bool)isSortedUp;
      }
    }
    public bool IsSortedDown {
      get {
        if (isSortedDown == null) CheckSorted();
        return (bool)isSortedDown;
      }
    }
    void CheckSorted()
    {
      if (values.Length < 2) {
        isSortedDown = isSortedUp = true;
        return;
      }

      int c = values[1].CompareTo(values[0]);
      if (c == 0) {
        isSortedDown = isSortedUp = false;
        return;
      }
      bool b = c > 0;
      for (int i = 2; i < Length; i++) {
        if (b ^ values[i] > values[i - 1]) {
          isSortedDown = isSortedUp = false;
          return;
        }
      }
      isSortedUp = b;
      isSortedDown = !b;
    }
    public bool IsSorted => IsSortedUp || IsSortedDown;

    public void Add(double v)
    {
      if (Length + 1 > MaxCapacity)
        throw new IndexOutOfRangeException($"MaxCapacity ({MaxCapacity}) exceeded");

      if (Length >= Capacity)
        Array.Resize(ref values, Min(Capacity * 2, MaxCapacity));

      if (Length == 0) {
        minimumValue = maximumValue = v;
        isSortedUp = isSortedDown = true;
      } else if (Length == 1) {
        if (v == values[0]) {
          isSortedUp = isSortedDown = false;
        } else if (v > values[0]) {
          maximumValue = v;
          isSortedUp = true;
          isSortedDown = false;
        } else if (v < values[0]) {
          minimumValue = v;
          isSortedUp = false;
          isSortedDown = true;
        }
      } else {
        if (maximumValue != null) {
          if (v > MaximumValue) maximumValue = v;
          else if (v < MinimumValue) minimumValue = v;
        }
        if (isSortedDown != null) {
          if (IsSortedDown && v >= values[Length - 1]) isSortedDown = false;
          else if (IsSortedUp && v <= values[Length - 1]) isSortedUp = false;
        }
      }
      values[Length++] = v;
    }
    public void AddRange(IList<double> vr)
    {
      if (vr == null || vr.Count == 0) return;

      if (Length + vr.Count > MaxCapacity)
        throw new IndexOutOfRangeException($"MaxCapacity ({MaxCapacity}) exceeded");

      if (Capacity == 0 && vr.Count >= MinimumCapacity)
        values = new double[vr.Count];

      if (Length + vr.Count > Capacity) {
        int newcap = Capacity * 2;
        while (Length + vr.Count > newcap)
          newcap *= 2;
        if (newcap > MaxCapacity) newcap = MaxCapacity;
        Array.Resize(ref values, newcap);
      }
      vr.CopyTo(values, Length);
      Length = Length + vr.Count;

      maximumValue = null;
      minimumValue = null;
      isSortedUp = null;
      isSortedDown = null;
    }

    public static void Sort(ValueArray x) => Array.Sort(x.values, 0, x.Length);
    public static void Sort(ValueArray x, ValueArray y)
    {
      if (x.Length > y.Length) throw new ArgumentException();

      Array.Sort(x.values, y.values, 0, x.Length);
    }
    public override void Clear() => Length = 0;
    public override string GetDataAsString(int i) => this[i].ToString();
    public double this[int i] {
      get {
        if (i < 0 || i >= Length)
          throw new IndexOutOfRangeException();
        return values[i];
      }
      set {
        if (i < 0 || i >= Length)
          throw new IndexOutOfRangeException();
        if (values[i] != value) {
          values[i] = value;
          RaisePropertyChanged("Item[]");
        }
      }
    }
    /// <summary> Linear interpolation based on floating point quasi-index </summary>
    /// <param name="i">(double) quasi-index</param>
    public double this[double i] {
      get {
        if (Length == 0) throw new ApplicationException();
        if (Length == 1) return values[0];

        if (i < -1.0 || i > Length + 1.0 || double.IsNaN(i)) return double.NaN;

        if (i < 0.0) {
          return values[0] + (values[1] - values[0]) * i;
        } else if (i > Length - 1.0) {
          return values[Length - 1] + (values[Length - 1] - values[Length - 2]) * (i - (Length - 1));
        } else {
          return values[(int)i] + (i - (int)i) * (values[(int)i + 1] - values[(int)i]);
        }
      }
    }
    public ValueArray(string name = null, string units = null, int capacity = InitialCapacity) : base(name, units)
    {
      if (MaxCapacity < capacity)
        throw new ArgumentOutOfRangeException();
      Length = 0;
      if (capacity != 0) values = new double[capacity];

      NormalizedData = new NormalizedArray(this);
    }
    public ValueArray(string name, string units, IList<double> data)
      : this(name, units, data.Count) => data.CopyTo(values, 0);
    public ValueArray(IList<double> data) : this(null, null, data) { }
    public static explicit operator ValueArray(double[] data) => new ValueArray(data);

    double? maximumValue = null;
    double? minimumValue = null;

    public double MaximumValue => (double)(maximumValue = maximumValue ?? values.Max());
    public double MinimumValue => (double)(minimumValue = minimumValue ?? values.Min());
    public double GetIndexAprox(double pos)
    {
      if (Length < 2 || double.IsNaN(pos)) return double.NaN;

      int bs;
      if (IsSorted) {
        if (IsSortedUp)
          bs = Array.BinarySearch(values, 0, Length, pos);
        else
          bs = Array.BinarySearch(values, 0, Length, pos, ReverseComparer<double>.Default);

        if (bs >= 0) {
          return bs;
        } else if (~bs == Length || ~bs == 0) {
          return double.NaN;
        } else {
          return ~bs - 1 + (pos - values[~bs - 1]) / (values[~bs] - values[~bs - 1]);
        }

      } else {

        IList<double> c;
        double err, lasterr, ia;
        err = lasterr = pos - values[0];
        if (lasterr == 0.0) return 0.0;
        int i;
        for (i = 1; i < Length; i++) {
          if (double.IsNaN(values[i])) continue;
          err = pos - values[i];
          if (err == lasterr) continue;
          if (err == 0.0) return i;
          if (Sign(err) == Sign(lasterr)) {
            lasterr = err;
          } else {
            ia = i - 1.0 + Abs(lasterr) / (Abs(err - lasterr));
            return ia < -1.0 || ia > Length + 1.0 ? double.NaN : ia;
          }
        }
        lasterr = pos - values[i - 2];
        ia = i - 1.0 + Abs(err) / Abs(err - lasterr);
        return ia < -1.0 || ia > Length + 1.0 ? double.NaN : ia;
      }
    }

    #region IList implementation
    public IEnumerator<double> GetEnumerator()
    {
      for (int i = 0; i < Length; i++) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    int ICollection<double>.Count => Length;
    bool ICollection<double>.IsReadOnly => false;
    int IList<double>.IndexOf(double item)
    {
      for (int i = 0; i < Length; i++)
        if (item == this[i]) return i;
      return -1;
    }
    void IList<double>.Insert(int index, double item) => throw new NotSupportedException();
    void IList<double>.RemoveAt(int index) => throw new NotSupportedException();
    bool ICollection<double>.Contains(double item)
    {
      for (int i = 0; i < Length; i++)
        if (item == this[i]) return true;
      return false;
    }
    void ICollection<double>.CopyTo(double[] array, int arrayIndex)
    {
      if (array.Length < Length + arrayIndex)
        throw new ArgumentException("array is too small");
      for (int i = 0; i < Length; i++)
        array[arrayIndex + i] = this[i];
    }
    bool ICollection<double>.Remove(double item) => throw new NotImplementedException();
    #endregion IList implementation

    public IList<double> NormalizedData { get; }

    class NormalizedArray : IList<double>
    {
      ValueArray parrent;

      public NormalizedArray(ValueArray p) => parrent = p;

      public double this[int index] {
        get => parrent[index] / parrent.Normalize;
        set => throw new NotSupportedException();
      }
      public int Count => parrent.Length;

      #region IList implementation
      public void CopyTo(double[] array, int arrayIndex)
      {
        if (array.Length < Count + arrayIndex)
          throw new ArgumentException("array is too small");
        for (int i = 0; i < Count; i++)
          array[arrayIndex + i] = this[i];
      }

      public IEnumerator<double> GetEnumerator()
      {
        for (int i = 0; i < Count; i++) yield return this[i];
      }
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
      public bool IsReadOnly => true;

      public void Add(double item) => throw new NotSupportedException();
      public void Clear() => throw new NotSupportedException();
      public bool Contains(double item)
      {
        for (int i = 0; i < Count; i++)
          if (item == this[i]) return true;
        return false;
      }
      public int IndexOf(double item)
      {
        for (int i = 0; i < Count; i++)
          if (item == this[i]) return i;
        return -1;
      }
      public void Insert(int index, double item) => throw new NotSupportedException();
      public bool Remove(double item) => throw new NotSupportedException();
      public void RemoveAt(int index) => throw new NotSupportedException();
      #endregion IList implementation
    }
  }

  public class ValueMatrix : DataColumn
  {
    double[][] rows;

    public static string DataDelimeter = "\t";

    public int NumColumns => rows?[0]?.Length ?? 0;
    public int NumRows => Length;
    public bool AutoCalc { get; set; } = true;

    public ValueArray XAxis { get; set; } = null;
    public ValueArray YAxis { get; set; } = null;

    public double GetDataAtXY(double X, double Y)
    {
      int x = (int)((XAxis?.GetIndexAprox(X) ?? X) + 0.5);
      int y = (int)((YAxis?.GetIndexAprox(Y) ?? Y) + 0.5);

      if (x < 0 || x >= NumColumns || y < 0 || y >= Length) return 0;

      return rows[y][x];
    }

    public double IntegrateData(Rect r) => throw new NotImplementedException();

    int firstCol = int.MinValue;
    int lastCol = int.MaxValue;
    int firstRow = int.MinValue;
    int lastRow = int.MaxValue;

    public int? FirstColI {
      get => firstCol == int.MinValue ? (int?)null : firstCol;
      set {
        if (value <= 0 || value > NumColumns - 1) value = null;
        if (FirstColI != value) {
          firstCol = value ?? int.MinValue;
          if (AutoCalc) RecalculateAccCol();
        }
      }
    }
    public int? LastColI {
      get => lastCol == int.MaxValue ? (int?)null : lastCol;
      set {
        if (value < 0 || value >= NumColumns - 1) value = null;
        if (LastColI != value) {
          lastCol = value ?? int.MaxValue;
          if (AutoCalc) RecalculateAccCol();
        }
      }
    }
    public int? FirstRowI {
      get => firstRow == int.MinValue ? (int?)null : firstRow;
      set {
        if (value <= 0 || value > NumRows - 1) value = null;
        if (firstRow != value) {
          firstRow = value ?? int.MinValue;
          if (AutoCalc) RecalculateAccRow();
        }
      }
    }
    public int? LastRowI {
      get => lastRow == int.MaxValue ? (int?)null : lastRow;
      set {
        if (value < 0 || value >= NumRows - 1) value = null;
        if (lastRow != value) {
          lastRow = value ?? int.MaxValue;
          if (AutoCalc) RecalculateAccRow();
        }
      }
    }

    public ValueArray AccRow { get; protected set; }
    public ValueArray AccColumn { get; protected set; }
    public ValueArray FullRow { get; protected set; }
    public ValueArray FullColumn { get; protected set; }

    public void RecalculateAccCol()
    {
      int imin = Max(firstCol, 0);
      int imax = Min(lastCol, NumColumns - 1);

      AccColumn.Clear();
      if (imin <= imax) {

        for (int j = 0; j < NumRows; j++)
          AccColumn.Add(rows[j][imin]);

        for (int i = imin + 1; i <= imax; i++) {
          for (int j = 0; j < NumRows; j++)
            AccColumn[j] += rows[j][i];
        }
      }
      RaisePropertyChanged(nameof(AccColumn));
    }
    public void RecalculateAccRow()
    {
      int imin = Max(firstRow, 0);
      int imax = Min(lastRow, NumRows - 1);

      AccRow.Clear();
      if (imin <= imax) {

        for (int j = 0; j < NumColumns; j++)
          AccRow.Add(rows[imin][j]);

        for (int i = imin + 1; i <= imax; i++) {
          for (int j = 0; j < NumColumns; j++)
            AccRow[j] += rows[i][j];
        }
      }
      RaisePropertyChanged(nameof(AccRow));
    }

    int RowCapacity => rows?.Length ?? 0;
    const int MaxRowCapacity = 0x10000;

    /*  /// <summary>
      /// Copy producing Array with minimum needed Capacity
      /// </summary>
      object ICloneable.Clone() => Clone(0);
      /// <summary>
      /// Copy producing Array with specific Capacity
      /// </summary>
      /// <param name="cap">0 - minimum needed Capacity</param>
      public ValueMatrix Clone(int cap = 0)
      {
        ValueMatrix nva = new ValueMatrix(Name, Units, cap > NumRows ? cap : NumRows);
        nva.NumRows = NumRows;
        Array.Copy(rows, nva.rows, NumRows);
        return nva;
      }
      public void Clone(ValueMatrix toFill)
      {
        toFill.NumRows = this.NumRows;
        Array.Copy(this.rows, toFill.rows, this.NumRows);
      }*/

    //public static ValueMatrix operator +(ValueMatrix a, ValueMatrix b)
    //{
    //  ValueMatrix nva = a.Clone();
    //  nva.Add(double.NaN);
    //  for(int i = 0; i < b.NumRows; i++)
    //    nva.Add(b[i]);
    //  return nva;
    //}
    public static ValueMatrix operator +(ValueMatrix a, double[] v) => a.AddRow(v);
    public ValueMatrix AddRow(double[] v)
    {
      if (Length >= MaxRowCapacity)
        throw new IndexOutOfRangeException();
      if (Length >= RowCapacity) {
        double[][] nv = new double[Min(RowCapacity * 2, MaxRowCapacity)][];
        rows.CopyTo(nv, 0);
        rows = nv;
      }
      if (NumColumns != 0 && NumColumns != v.Length)
        throw new IndexOutOfRangeException("Array Sizes do not Match");

      rows[Length] = v;
      // FirstRowAdded
      if (Length == 0) {
        AccRow = new ValueArray("Accamulated Row", Units, v.Length);
        AccColumn = new ValueArray("Accamulated Column", Units, DefaultCapacity);
        FullRow = new ValueArray("Full Row", Units, v.Length);
        FullColumn = new ValueArray("Full Column", Units, DefaultCapacity);
        for (int i = 0; i < v.Length; i++) {
          AccRow.Add(0);
          FullRow.Add(0);
        }
      }
      if (AutoCalc) {
        FullRow.SuspendNotify = true;
        AccRow.SuspendNotify = true;
        double fullc = 0, accc = 0;
        for (int i = 0; i < v.Length; i++) {
          FullRow[i] += v[i];
          fullc += v[i];
          if (firstCol <= i && i <= lastCol)
            accc += v[i];
          if (firstRow <= Length && Length <= lastRow)
            AccRow[i] += v[i];
        }
        FullColumn.Add(fullc);
        AccColumn.Add(accc);
        FullRow.SuspendNotify = false;
        AccRow.SuspendNotify = false;
      }
      Length++;
      return this;
    }

    public override void Clear() => Length = 0;

    public override string GetDataAsString(int row)
    {
      if (NumColumns == 0) return "";

      StringBuilder sb = new StringBuilder();

      sb.Append(rows[row][0].ToString());

      for (int i = 1; i < NumColumns; i++) {
        sb.Append(DataDelimeter);
        sb.Append(rows[row][i].ToString());
      }

      return sb.ToString();
    }

    /// <summary> Copy producing Array with minimum needed Capacity </summary>
    override public DataColumn Clone() => Clone(null);
    public ValueMatrix Clone(int? capacity)
    {
      int cap = Max(capacity ?? 0, Length);
      ValueMatrix nva = new ValueMatrix(Name, Units, cap) {
        XAxis = XAxis,
        YAxis = YAxis
      };
      if (NumColumns is int numcollumns) {
        nva.Length = Length;
        for (int i = 0; i < Length; i++) {
          nva.rows[i] = new double[numcollumns];
          Array.Copy(rows[i], nva.rows[i], numcollumns);
        }
      }
      return nva;
    }

    public static int DefaultCapacity = 200;

    public double[] this[int i] {
      get {
        if (i < 0 || i >= Length) throw new IndexOutOfRangeException();
        return rows[i];
      }
      set {
        if (i < 0 || i >= Length) throw new IndexOutOfRangeException();
        rows[i] = value;
      }
    }

    public ValueMatrix(string name = null, string units = null, int? capacity = null) : base(name, units)
    {
      int cap = capacity ?? DefaultCapacity;
      if (MaxRowCapacity < cap) throw new ArgumentOutOfRangeException();
      Length = 0;
      rows = new double[cap][];
    }
    public static implicit operator double[][](ValueMatrix VA)
    {
      double[][] da = new double[VA.Length][];
      Array.Copy(VA.rows, da, VA.Length);
      return da;
    }
  }
  public class ReverseComparer<T> : IComparer<T>
  {
    public static ReverseComparer<T> Default = new ReverseComparer<T>();
    public int Compare(T x, T y)
    {
      return Comparer<T>.Default.Compare(y, x);
    }
  }
}