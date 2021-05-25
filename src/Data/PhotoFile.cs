using System;
using System.IO;

namespace PhotoOrganizer.Data
{
    public class PhotoFile
    {
        private string path;
        public string Path { set; get; }

        public string FileName { private set; get; }
        public string FileNameWithoutExtention { private set; get; }
        public string FullName { private set; get; }
        public string Extension { private set; get; }
        public long Length { private set; get; }
        public DateTime LastWriteTime { private set; get; }
        public DateTime CreationTime { private set; get; }

        public string CameraModel { private set; get; }
        public int Iso { private set; get; }

        private PhotoFile() { }

        public PhotoFile(string path)
        {
          Initialize(path);
        }

        private void Initialize(string path)
        {
          try{
            var fi = new System.IO.FileInfo(path);

            FileName = System.IO.Path.GetFileName(fi.FullName);
            FileNameWithoutExtention = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
            FullName = fi.FullName;
            Extension = fi.Extension;
            Length = fi.Length;
            LastWriteTime = fi.LastWriteTime;
            CreationTime = fi.CreationTime;
          }catch{
            return;
          }
          this.path = path;
        }

    }
}
