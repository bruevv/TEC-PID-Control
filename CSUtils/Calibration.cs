using System;
using Samples.Data;
using System.IO;

namespace Calibration
{
  public interface ICalibration
  {
    double Transform(double val);
    double TransformBack(double val);
  }
  public class LinearCalibration : ICalibration
  {
    public double A { get; private set; } = 0;
    public double B { get; private set; } = 1;

    double x1 = 0, x2 = 1, y1 = 0, y2 = 1;

    public double X1 {
      get => x1;
      set { x1 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double X2 {
      get => x2;
      set { x2 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double Y1 {
      get => y1;
      set { y1 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double Y2 {
      get => y2;
      set { y2 = value; ABfromXY(x1, x2, y1, y2); }
    }

    void ABfromXY(double X1, double X2, double Y1, double Y2)
    {
      B = (Y2 - Y1) / (X2 - X1);
      A = Y1 - X1 * B;
    }

    public void InitfromXY(double X1, double X2, double Y1, double Y2)
    {
      if(Y1 == Y2 || X1 == X2)
        throw new ArgumentException("Wrong points");
      ABfromXY(X1, X2, Y1, Y2);
    }

    public double Transform(double X) => A + B * X;
    public double TransformBack(double Y) => (Y - A) / B;
  }
  public class TableCalibration : ICalibration
  {
    ValueArray XX = null;
    ValueArray YY = null;
    private string filename;

    public double XOfset { get; set; } = 0;
    public double YOfset { get; set; } = 0;

    public TableCalibration(double[] xx, double[] yy)
    {
      XX = (ValueArray)xx;
      YY = (ValueArray)yy;
    }
    /// <summary>
    /// csv file
    /// </summary>
    /// <param name="filename"></param>
    public TableCalibration(string filename = "Calibration.csv")
    {
      if (!File.Exists(filename)) {
        var files = Directory.GetFiles(".", "*.csv");
        if ((files?.Length ?? 0) == 0) throw new FileNotFoundException($"calibration in csv format missing ({filename})");
        filename = files[0];
      }
      XX = new ValueArray();
      YY = new ValueArray();
      using (var sr = new StreamReader(filename)) {
        while (!sr.EndOfStream) {
          string line = sr.ReadLine();
          string[] words = line.Split(',');
          if (words.Length >= 2) {
            if (
              double.TryParse(words[0], out double x) &&
              double.TryParse(words[1], out double y)) {
              XX.Add(x);
              YY.Add(y);
            }
          }
        }
        if (XX.Length < 2) throw new FileFormatException("Calibration csv File should have first two columns in numbeer format without commas");
      }
      //      if (!XX.IsSorted) ValueArray.Sort(XX, YY);
    }
    public double Transform(double x)
    {
      if (!XX.IsSorted) ValueArray.Sort(XX, YY);
      return YY[XX.GetIndexAprox(x - XOfset)] + YOfset;
    }
    public double TransformBack(double y)
    {
      if (!YY.IsSorted) ValueArray.Sort(YY, XX);
      return XX[YY.GetIndexAprox(y - YOfset)] + XOfset;
    }
  }
}