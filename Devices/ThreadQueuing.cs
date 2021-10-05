using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override 
namespace ThreadQueuing
{
  public class ThreadQueue : IDisposable
  {
    interface ITransaction
    {
      void Invoke();
    }
    readonly struct TAction : ITransaction
    {
      readonly Action action;
      public TAction(Action a) => action = a;
      public void Invoke() => action.Invoke();
      public override bool Equals(object obj) => obj is TAction t ? t.action == action : false;
    }
    readonly struct TCommand : ITransaction
    {
      readonly Action<string> action;
      readonly string str;
      public TCommand(Action<string> a, string s)
      {
        str = s;
        action = a;
      }
      public void Invoke() => action.Invoke(str);

      public override bool Equals(object obj) => obj is TCommand t ? t.action == action && t.str == str : false;
    }
    readonly struct TCommand<T> : ITransaction
    {
      readonly Action<T> action;
      readonly T t;
      public TCommand(Action<T> a, T t)
      {
        this.t = t;
        action = a;
      }
      public void Invoke() => action.Invoke(t);
      public override bool Equals(object obj) => obj is TCommand<T> tc ? tc.action == action && tc.t.Equals(t) : false;
    }
    readonly struct TCommand<T1, T2> : ITransaction
    {
      readonly Action<T1, T2> action;
      readonly T1 t1;
      readonly T2 t2;
      public TCommand(Action<T1, T2> a, T1 t1, T2 t2)
      {
        this.t1 = t1;
        this.t2 = t2;
        action = a;
      }
      public void Invoke() => action.Invoke(t1, t2);
    }
    readonly struct TCommand<T1, T2, T3> : ITransaction
    {
      readonly Action<T1, T2, T3> action;
      readonly T1 t1;
      readonly T2 t2;
      readonly T3 t3;
      public TCommand(Action<T1, T2, T3> a, T1 t1, T2 t2, T3 t3)
      {
        this.t1 = t1;
        this.t2 = t2;
        this.t3 = t3;
        action = a;
      }
      public void Invoke() => action.Invoke(t1, t2, t3);
    }
    readonly struct TRequest : ITransaction
    {
      readonly Func<string, string> action;
      readonly string str;
      readonly Action<string> callback;
      public TRequest(Func<string, string> a, string s, Action<string> cb)
      {
        str = s;
        action = a;
        callback = cb;
      }
      public void Invoke()
      {
        string ret;
        ret = action.Invoke(str);
        callback.Invoke(ret);
      }
    }

    Thread thread;
    readonly Action ThreadInit;

    public event Action ThreadIdle;
    public event Action ThreadIdleTimeOut;
    public event Action ExitIdle;
    public event Action ThreadDispose;

    protected void OnThreadInit() => ThreadInit?.Invoke();
    protected void OnThreadIdle() { IsIdle = true; ThreadIdle?.Invoke(); }
    int invocationIterator = 0;
    protected void OnThreadIdleTimeOut()
    {
      //Iterate if several
      Delegate[] dels = ThreadIdleTimeOut?.GetInvocationList();
      if ((dels?.Length ?? 0) > 1) {
        if (++invocationIterator >= dels.Length)
          invocationIterator = 0;
        ((Action)dels[invocationIterator]).Invoke();
      } else {
        ThreadIdleTimeOut?.Invoke();
      }
    }
    protected void OnThreadExitIdle() { IsIdle = false; ExitIdle?.Invoke(); }
    protected void OnThreadDispose() => ThreadDispose?.Invoke();

    public int QueueIdleWait = -1;
    bool IsIdle { get; set; } = false;

    static Dictionary<string, ThreadQueue> insts = new Dictionary<string, ThreadQueue>();

    static ThreadQueue FindInstance(string name)
    {
      if (insts.ContainsKey(name))
        return insts[name];
      else
        return null;
    }
    public object Owner { get; } = null;
    // TODO make isolated enumerator
    List<object> userObjects = new List<object>();
    public IEnumerable<object> UserObjects {
      get { foreach (object o in userObjects) { yield return o; } }
    }
    public string Name { get; }
    /*TODO public ThreadQueue ChangeNameOrThis(string newname)
     {
       if(name == newname) return this;

       if(insts.ContainsKey(value))
         throw new Exception("ThreadQueue with this name already exists");
       if(UserObjects.)
         insts.Remove(name);
       name = value;
       insts.Add(name, this);
     }*/

    /// <summary> Find the ThreadQueue with the same name or create new </summary>
    /// <param name="threadInit">Only runs if new ThreadQueue is created</param>
    public static ThreadQueue Create(string name, object owner, Action threadInit = null)
    {
      ThreadQueue tq = name == "noname" ? null : FindInstance(name);
      if (tq == null)
        tq = new ThreadQueue(owner, name, threadInit);
      else
        tq.userObjects.Add(owner);

      return tq;
    }

    ThreadQueue(object owner, string name, Action threadInit)
    {
      ThreadInit = threadInit;
      Owner = owner;
      Name = name;
      if (name != "noname")
        insts.Add(name, this);
      userObjects.Add(owner);
      thread = new Thread(Cycle) {
        Priority = ThreadPriority.AboveNormal,
        Name = $"ThreadQueue({name})",
        IsBackground = true
      };
    }
    ~ThreadQueue() => Dispose();

    public void Start()
    {
      if (thread.ThreadState.HasFlag(ThreadState.Unstarted)) thread.Start();
    }

    EventWaitHandle TSuspend = new EventWaitHandle(false, EventResetMode.AutoReset);
    bool TQueueExit = false;
    Queue<ITransaction> TQueue = new Queue<ITransaction>(100);

    void Cycle()
    {
      OnThreadInit();
      while (!TQueueExit) {
        if (TQueue.Count > 0) {
          ITransaction t;
          lock (TQueue) { t = TQueue.Dequeue(); }
          t.Invoke();
        } else {
          OnThreadIdle();
          if (!TSuspend.WaitOne(QueueIdleWait))
            OnThreadIdleTimeOut();
        }
      }
      OnThreadDispose();
    }

    void Enqueue(ITransaction a)
    {
      if (IsIdle) OnThreadExitIdle();
      lock (TQueue) { TQueue.Enqueue(a); }
      TSuspend.Set();
    }
    void EnqueueUnique(ITransaction a)
    {
      if (IsIdle) OnThreadExitIdle();
      lock (TQueue) { if (!TQueue.Contains(a)) TQueue.Enqueue(a); }
      TSuspend.Set();
    }
    void EnqueueReplace(ITransaction a)
    {
      if (IsIdle) OnThreadExitIdle();
      lock (TQueue) {
        if (TQueue.Count > 0) {
          TQueue.Clear();
        }
        TQueue.Enqueue(a);
      }
      TSuspend.Set();
    }
    public void Enqueue(Action a) => Enqueue(new TAction(a));
    public void Enqueue<T>(Action<T> a, T s) => Enqueue(new TCommand<T>(a, s));
    public void Enqueue<T1, T2>(Action<T1, T2> a, T1 s1, T2 s2) => Enqueue(new TCommand<T1, T2>(a, s1, s2));
    public void Enqueue<T1, T2, T3>(Action<T1, T2, T3> a, T1 s1, T2 s2, T3 s3) => Enqueue(new TCommand<T1, T2, T3>(a, s1, s2, s3));

    public void EnqueueUnique(Action a) => EnqueueUnique(new TAction(a));
    public void EnqueueUnique<T>(Action<T> a, T s) => EnqueueUnique(new TCommand<T>(a, s));
    public void EnqueueUnique<T1, T2>(Action<T1, T2> a, T1 s1, T2 s2) => EnqueueUnique(new TCommand<T1, T2>(a, s1, s2));
    public void EnqueueUnique<T1, T2, T3>(Action<T1, T2, T3> a, T1 s1, T2 s2, T3 s3) => EnqueueUnique(new TCommand<T1, T2, T3>(a, s1, s2, s3));

    public void EnqueueReplace(Action a) => EnqueueReplace(new TAction(a));
    public void EnqueueReplace<T>(Action<T> a, T s) => EnqueueReplace(new TCommand<T>(a, s));
    public void EnqueueReplace<T1, T2>(Action<T1, T2> a, T1 s1, T2 s2) => EnqueueReplace(new TCommand<T1, T2>(a, s1, s2));
    public void EnqueueReplace<T1, T2, T3>(Action<T1, T2, T3> a, T1 s1, T2 s2, T3 s3) => EnqueueReplace(new TCommand<T1, T2, T3>(a, s1, s2, s3));
    //    void ClearQueue() => TQueue.Clear();
    #region implementing Dispose

    bool disposed = false;

    protected void Dispose(bool disposing)
    {
      if (disposed) return;

      if (disposing) {
        insts.Remove(Name);
        TQueueExit = true;
        TSuspend?.Set();

        thread?.Join();
        TSuspend?.Dispose();
      }
      disposed = true;
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }

  public static class EventWaitHandlePool
  {
    static Dictionary<EventWaitHandle, bool> handles = new Dictionary<EventWaitHandle, bool>();
    static object local_lock = new object();
    public static EventWaitHandle GetHandle()
    {
      lock (local_lock) {
        if (handles.ContainsValue(false)) {
          foreach (var ewh in handles.Keys) {
            if (!handles[ewh]) {
              handles[ewh] = true;
              return ewh;
            }
          }
          throw new Exception();
        } else {
          EventWaitHandle ewh = new ManualResetEvent(false);
          handles.Add(ewh, true);
          return ewh;
        }
      }
    }
    public static void ReturnHandle(EventWaitHandle ewh)
    {
      lock (local_lock) {
        ewh.Reset();
        handles[ewh] = false;
      }
    }
  }

}