#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms.DataVisualization.Charting;
using CSUtils;

namespace WPFControls
{
  using static Math;
  public class NSeries : Series, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    object sample = null;
    public object Sample {
      get => sample;
      set {
        if(sample != value) {
          if(sample is INotifyPropertyChanged inpc_old)
            inpc_old.PropertyChanged -= Sample_PropertyChanged;

          sample = value;
          if(sample is INotifyPropertyChanged inpc)
            inpc.PropertyChanged += Sample_PropertyChanged;

          dynamic dynsample = value;
          try {
            IsLoaded = dynsample.IsLoaded;
            Name = dynsample.Key;
            SampleName = dynsample.Name;
          } catch { }
        }
      }
    }
    void Sample_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      dynamic dynsender = sender;
      try {
        switch(e.PropertyName) {
          case nameof(IsLoaded):
            if(IsLoaded != dynsender.IsLoaded) IsLoaded = dynsender.IsLoaded;
            break;
          case nameof(Name):
            if(SampleName != dynsender.Name) SampleName = dynsender.Name;
            break;
          case nameof(NeedUpdate):
            NeedUpdate = true;
            break;
        }
      } catch { }
    }

    public bool IsEmpty { get; private set; } = false;
    public bool HasZeros { get; private set; } = true;
    public bool ReplaceZeroWithNaN { get; set; } = true;
    public double ReplaceZerosWith { get; set; } = 0.01;
    public void UpdateSample(IList<double> X, IList<double> Y)
    {
      if(!IsLoaded) throw new Exception("Sample is not loaded");

      if((X == null && Y == null) || (X.Count == 0 && Y.Count == 0)) {
        Clear();
      } else if(X.Count != Y.Count) {
        var e = new ArgumentException("X and Y sizes should match");
        Logger.Log(e, Logger.Mode.Error, nameof(NSeries));
      } else {
        Points.SuspendUpdates();
        Clear();
        for(int i = 0; i < X.Count; i++) AddPoint(X[i], Y[i]);

        Points.ResumeUpdates();
      }
    }
    public void Clear()
    {
      Points.Clear();
      IsEmpty = true;
      HasZeros = false;
    }
    const double MaxNumber = 1e20;

    public void AddPoint(double x, double y)
    {
      if(ReplaceZeroWithNaN) y = y == 0 ? ReplaceZerosWith : y;

      x = Normalize(x);
      y = Normalize(y);

      if(!double.IsNaN(y) && y != 0) IsEmpty = false;
      else HasZeros = true;

      Points.AddXY(x, y);
    }

    static double Normalize(double d) => d > MaxNumber || d < -MaxNumber ? double.NaN : d;

    NSeries linkedSeries = null;
    public NSeries LinkedSeries {
      get => linkedSeries;
      set {
        if(linkedSeries != null) linkedSeries.linkedSeries = null;
        linkedSeries = value;
        if(linkedSeries != null) {
          //        linkedSeries.Name = Name;
          //       linkedSeries.Color = Color;
          //        linkedSeries.Enabled = Enabled;
          //        linkedSeries.Sample = Sample;// TODO add update event?
          linkedSeries.linkedSeries = this;
        }
      }
    }

    protected void OnPropertyChanged(object s, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(s, e);

    public NSeries(string keyname, string name=null, object sample = null, bool isloaded = true) 
      : base(keyname)
    {
      sampleName = name ?? keyname;
      Enabled = isloaded;
      IsLoaded = isloaded;
      Sample = sample;
    }
    public void UpdateProperty(string pn = null) => OnPropertyChanged(this, new PropertyChangedEventArgs(pn));

    string sampleName = null;
    public string SampleName {
      get => sampleName ?? base.Name;
      set {
        if(sampleName != value) {
          sampleName = value;
          if(LinkedSeries != null) LinkedSeries.SampleName = SampleName;
          UpdateProperty(nameof(SampleName));
        }
      }
    }

    bool isLoaded = true;
    public bool IsLoaded {
      get => isLoaded;
      protected set {
        if(isLoaded != value) {
          isLoaded = value;
          if(LinkedSeries != null) LinkedSeries.IsLoaded = value;
          UpdateProperty(nameof(IsLoaded));
        }
      }
    }
    bool needUpdate = false;
    public bool NeedUpdate {
      get => needUpdate;
      protected set {
        if(value && Enabled) {
          TryUpdate(); // it updates 'NeedUpdate' inside
          return;
        }
        if(needUpdate != value) {
          needUpdate = value;
          UpdateProperty(nameof(NeedUpdate));
        }
      }
    }
    public bool IsIntrinsic { get; set; } = false;
    public void TryUpdate()
    {
      bool res = Update?.Invoke(sample, this) ?? true;
      if(!res)
        Logger.Log($"Cannot update sample {sample}", Logger.Mode.Error, nameof(NSeries));
      else
        NeedUpdate = false;
    }

    public delegate bool UpdateDelegate(object sample, NSeries thisseries);
    UpdateDelegate update = null;
    public UpdateDelegate Update {
      get => update;
      set {
        update = value;
  //      NeedUpdate = true; TODO check?????
      }
    }
    public new bool Enabled {
      get => base.Enabled;
      set {
        if(base.Enabled != value) {
          if(value && NeedUpdate) TryUpdate();
          base.Enabled = value;
          if(LinkedSeries != null) LinkedSeries.Enabled = Enabled;
          //         if(!Enabled && Selected) Selected = false; TODO delete Feedback point?
          UpdateProperty(nameof(Enabled));
        }
      }
    }
    bool selected = false;
    public bool Selected {
      get => selected;
      set {
        if(selected != value) {
          selected = value;
          if(LinkedSeries != null) LinkedSeries.Selected = Selected;
          BorderWidth = selected ? 3 : 1;
          UpdateProperty(nameof(Selected));
        }
      }
    }
    public new Color Color {
      get => base.Color;
      set {
        if(value == Color.Empty) {
          value = LinkedSeries?.Color ?? Palette[IncrementPaletteIndex()];
        }

        if(base.Color != value) {
          base.Color = value;
          UpdateProperty(nameof(Color));
          if(LinkedSeries != null) LinkedSeries.Color = Color;
        }
      }
    }
    public void AutoColor() => Color = Color.Empty;

    public double GetIndexAprox(double pos)
    {
      if(Points.Count < 2) return double.NaN;

      double err, lasterr, ia;
      err = lasterr = pos - Points[0].XValue;
      if(lasterr == 0.0) return 0.0;
      int i;
      for(i = 1; i < Points.Count; i++) {
        if(double.IsNaN(Points[i].XValue)) continue;
        err = pos - Points[i].XValue;
        if(err == lasterr) continue;
        if(err == 0.0) return i;
        if(Sign(err) == Sign(lasterr)) {
          lasterr = err;
        } else {
          ia = i - 1.0 + Abs(lasterr) / (Abs(err - lasterr));
          return ia < -1.0 || ia > Points.Count + 1.0 ? double.NaN : ia;
        }
      }
      lasterr = pos - Points[i - 2].XValue;
      ia = i - 1.0 + Abs(err) / Abs(err - lasterr);
      return ia < -1.0 || ia > Points.Count + 1.0 ? double.NaN : ia;
    }

    #region Palettes
    static int PaletteIndex = 0;
    static int IncrementPaletteIndex() => PaletteIndex = (PaletteIndex + 1) % Palette.Length;
    public static void ResetPalette() => PaletteIndex = 0;

    public new static readonly Color[] Palette = new Color[] {
      Color.Black,
      Color.Lime,
      Color.Blue,
      Color.Red,
      Color.Violet,
      Color.Gray,
      Color.Orange,
      Color.DarkBlue,
      Color.Brown,
      Color.Green,
      Color.Goldenrod,
      Color.DarkCyan,
      Color.YellowGreen,
      Color.DarkMagenta,
      Color.Indigo,
      Color.Tomato,
      Color.Olive,
      Color.DarkKhaki,
 //     Color.FromArgb(255,0,80,0),
 //     Color.FromArgb(255,80,0,0),
 //     Color.FromArgb(255,0,80,80),
    };
    /*   Color[] Palette = new Color[] {
         Color.Purple,
         Color.Black,
         Color.Blue,
         Color.Green,
         Color.Red,
         Color.Gold,
         Color.Gray,
         Color.DarkBlue,
         Color.DarkGreen,
         Color.DarkRed,
         Color.DarkOrange,
       };*/
    #endregion Palettes
  }
}
