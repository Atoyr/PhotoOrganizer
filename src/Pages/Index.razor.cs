using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PhotoOrganizer.Data;
using PhotoOrganizer.Data.Electron;

namespace PhotoOrganizer.Pages
{
    public partial class Index
    {
        [Inject]
        public NavigationManager Navigation { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        private int currentCount = 0;

        private string FromPath { get; set; }
        private string ToPath { get; set; }
        private bool IsCopy { get; set; }
        private List<string> Logs { get; set; } = new List<string>();

        private void IncrementCount()
        {
            currentCount++;
        }

        async void OpenFromFolderPath() {
           if (JSRuntime == null ) return;
           var result = await JSRuntime.ShowOpenDialog(new OpenDialogOption {
               Title = "フォルダを開く",
               ButtonLabel = "開く",
               DefaultPath = @"C:\",
               Properties = new string[] { "openDirectory" },
           });
           if (!result.Canceled) {
               FromPath = result.FilePaths.FirstOrDefault();
               StateHasChanged();
           }
        }

        async void OpenToFolderPath() {
           if (JSRuntime == null ) return;
           var result = await JSRuntime.ShowOpenDialog(new OpenDialogOption {
               Title = "フォルダを開く",
               ButtonLabel = "開く",
               DefaultPath = @"C:\",
               Properties = new string[] { "openDirectory" },
           });
           if (!result.Canceled) {
               ToPath = result.FilePaths.FirstOrDefault();
               StateHasChanged();
           }
        }

        async void Execute()
        {
          Executor.Instance.FromPath = FromPath;
          Executor.Instance.ToPath = ToPath;
          Executor.Instance.IsCopy = IsCopy;
          Executor.Instance.FolderItems.Add(FolderItem.Extension);
          Executor.Instance.FolderItems.Add(FolderItem.YearMonth);
          Executor.Instance.LogEvent += (s,e) => {
            InvokeAsync(() => 
            {
              this.Logs.Add(e.Message);
              StateHasChanged();
              });
            Console.WriteLine(e.Message);
          };

          var cancelTokenSource = new CancellationTokenSource();

          await Executor.Instance.RunAsync(cancelTokenSource.Token);
        }
        void CheckboxClicked(object value)
        {
            switch(((string)value).ToLower())
            {
              case "copy":
                IsCopy = true;
                break;
              case "move":
                IsCopy = false;
                break;
            }
        }
    }
}
