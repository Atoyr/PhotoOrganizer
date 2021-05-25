using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoOrganizer.Data.Event;

namespace PhotoOrganizer.Data
{
  public class Executor
  {
    private readonly object lockObj = new object();

    private Executor() 
    { 
      Extentions = new List<string>();
      FolderItems = new List<FolderItem>();
    }

    private static Executor instance;
    public static Executor Instance
    {
      get
      {
        if(Executor.instance == null)
        {
          Executor.instance = new Executor();
        }
        return Executor.instance;
      }
      private set
      {
        instance = value;
      }
    }

    private string fromPath;
    public string FromPath 
    {
      set
      {
        lock(lockObj)
        {
          fromPath = value;
        }
      }
      get
      {
        return fromPath;
      }
    }
    private string toPath;
    public string ToPath
    { 
      set
      {
        lock(lockObj)
        {
          toPath = value;
        }
      }
      get
      {
        return toPath;
      }
    }

    private bool isCopy;
    public bool IsCopy 
    { 
      set
      {
        lock(lockObj)
        {
          isCopy = value;
        }
      }
      get
      {
        return isCopy;
      }
    }

    public IEnumerable<string> Extentions { set; get; }
    public IList<FolderItem> FolderItems { set; get; }

    public event EventHandler<LogEventArgs> LogEvent;

    public void Clear()
    {
      FromPath = string.Empty;
      ToPath = string.Empty;
      IsCopy = false;
      lock(lockObj)
      {
        Extentions = new List<string>();
        FolderItems.Clear();
      }
    }

    public async Task RunAsync(CancellationToken ct)
    {
      await Task.Run(() => this.Run(ct));
    }

    public void Run()
    {
      var cancelTokenSource = new CancellationTokenSource();
      Run(cancelTokenSource.Token);
    }

    private void Run(CancellationToken ct)
    {
      lock(lockObj)
      {
        var extentions = Extentions.ToList();
        var folderItems = FolderItems.ToList();

        // Validate
        OnInfoLogEvent(new LogEventArgs("パラメータチェック"));
        if (!System.IO.Directory.Exists(Path.GetFullPath(this.FromPath)))
        {
          OnErrorLogEvent(new LogEventArgs("参照元パスが有効ではありません。"));
          return;
        }

        if (!System.IO.Directory.Exists(Path.GetFullPath(this.ToPath)))
        {
          OnErrorLogEvent(new LogEventArgs("参照先パスが有効ではありません。"));
          return;
        }

        // 非同期キャンセル処理
        if (IsCanceled(ct)) return;

        // Get File List
        OnInfoLogEvent(new LogEventArgs("参照元ファイル内容取得"));
        var tempFileList = Directory.EnumerateFiles(Path.GetFullPath(this.FromPath), "*", SearchOption.AllDirectories);
        var fileList = new List<string>();
        if (extentions.Count > 0)
        {
          foreach(var ext in extentions)
          {
            OnInfoLogEvent(new LogEventArgs($"拡張子 [{ext}] のリスト取得"));
            fileList.AddRange(tempFileList.Where(x => x.EndsWith("." + ext, StringComparison.CurrentCultureIgnoreCase)));
          }
        }
        else
        {
          OnInfoLogEvent(new LogEventArgs("全拡張子のリスト取得"));
          fileList.AddRange(tempFileList);
        }

        // 非同期キャンセル処理
        if (IsCanceled(ct)) return;

        // Create PhotoFiles
        OnInfoLogEvent(new LogEventArgs("ファイルの情報取得"));
        List<PhotoFile> photoFiles = fileList.Select(x => new PhotoFile(x)).ToList();

        // 非同期キャンセル処理
        if (IsCanceled(ct)) return;

        // TODO Sort File List
        photoFiles = photoFiles.OrderBy(x => x.CreationTime).ToList();

        // 非同期キャンセル処理
        if (IsCanceled(ct)) return;

        // フォルダ作成してファイル移動
        foreach(var pf in photoFiles)
        {
          // 非同期キャンセル処理
          if (IsCanceled(ct)) return;
          var dir = toPath;

          if (string.IsNullOrEmpty(pf.FileName)) continue;

          foreach(var fi in folderItems)
          {
            switch(fi)
            {
              case FolderItem.Extension:
                dir = Path.Combine(dir, pf.Extension.Substring(1).ToLower());
                break;
              case FolderItem.Year:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("yyyy"));
                break;
              case FolderItem.YearMonth:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("yyyyMM"));
                break;
              case FolderItem.YearMonthDay:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("yyyyMMdd"));
                break;
              case FolderItem.Month:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("MM"));
                break;
              case FolderItem.MonthDay:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("MMdd"));
                break;
              case FolderItem.Day:
                dir = Path.Combine(dir, pf.LastWriteTime.ToString("dd"));
                break;
            }

            if(!Directory.Exists(dir))
            {
              OnInfoLogEvent(new LogEventArgs($"ディレクトリ [{dir}] の作成"));
              try
              {
                Directory.CreateDirectory(dir);
              }
              catch(Exception e)
              {
                OnErrorLogEvent(new LogEventArgs($"ディレクトリ [{dir}] の作成に失敗しました : {e.Message}"));
                return;
              }
            }
          }

          // 非同期キャンセル処理
          if (IsCanceled(ct)) return;

          var fileInfo = new System.IO.FileInfo(pf.FullName);
          var fileExists = true;
          var fileName = pf.FileName;
          var inc = 0;

          while(fileExists)
          {
            if (File.Exists(Path.Combine(dir, fileName)))
            {
              // 同一ファイルが存在する場合
              var toFileInfo = new System.IO.FileInfo(Path.Combine(dir, fileName));

              // 同一ハッシュの場合はコピーしない
              if (fileInfo.GetHashCode() == toFileInfo.GetHashCode() )
              {
                OnInfoLogEvent(new LogEventArgs($"ファイル [{Path.Combine(dir, fileName)}] がすでに存在しましたが、ハッシュ値が一致したため処理対象外とします"));
                break;
              }

              OnInfoLogEvent(new LogEventArgs($"ファイル [{Path.Combine(dir, fileName)}] がすでに存在するため、ファイル名を変更します"));
              // ファイル名を設定して再判定
              inc++;
              fileName = pf.FileNameWithoutExtention + inc.ToString();
              fileName = fileName + pf.Extension;
            }
            else
            {
              fileExists = false;
            }
          }

          if (fileExists)
          {
            continue;
          }

          // 非同期キャンセル処理
          if (IsCanceled(ct)) return;

          try
          {
            if (IsCopy)
            {
              OnInfoLogEvent(new LogEventArgs($"ファイル [{fileInfo.FullName}] を [{Path.Combine(dir, fileName)}] にコピーします"));
              fileInfo.CopyTo(Path.Combine(dir, fileName));
            }
            else
            {
              OnInfoLogEvent(new LogEventArgs($"ファイル [{fileInfo.FullName}] を [{Path.Combine(dir, fileName)}] に移動します"));
              fileInfo.MoveTo(Path.Combine(dir, fileName));
            }
          }
          catch(Exception e)
          {
            OnErrorLogEvent(new LogEventArgs($"ファイル [{dir}] の操作に失敗しました : {e.Message}"));
          }
        }
      }
    }

    private bool IsCanceled(CancellationToken ct)
    {
      if (ct.IsCancellationRequested)
      {
        OnInfoLogEvent(new LogEventArgs("処理を中断します"));
        return true;
      }
      return false;
    }

    protected virtual void OnLogEvent(LogEventArgs e)
    {
      var handler = LogEvent;
      if (handler != null)
      {
        handler(this, e);
      }
    }

    protected virtual void OnInfoLogEvent(LogEventArgs e)
    {
      e.LogLevel = LogLevel.Info;
      OnLogEvent(e);
    }

    protected virtual void OnWarningLogEvent(LogEventArgs e)
    {
      e.LogLevel = LogLevel.Warning;
      OnLogEvent(e);
    }

    protected virtual void OnErrorLogEvent(LogEventArgs e)
    {
      e.LogLevel = LogLevel.Error;
      OnLogEvent(e);
    }
  }
}

