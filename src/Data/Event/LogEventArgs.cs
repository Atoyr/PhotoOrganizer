using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoOrganizer.Data.Event
{
  public class LogEventArgs : EventArgs
  {
    public LogLevel LogLevel { set; get; }

    public string Message { set; get; }

    public LogEventArgs(string message)
    {
      this.Message = message;
    }

    public LogEventArgs(LogLevel logLevel, string message)
    {
      this.LogLevel = logLevel;
      this.Message = message;
    }
  }

  public enum LogLevel
  {
    Info,
    Warning,
    Error
  }
}

