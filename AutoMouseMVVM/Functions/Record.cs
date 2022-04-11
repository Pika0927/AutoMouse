using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AutoMouseMVVM.Functions
{
    class Record
    {
        public string FilePath { get; set; }
        public Record(string filepath)
        {
            FilePath = filepath;
        }
        public void WriteRecord(string Data)
        {
            if (CheckFile())
            {
                ClearTxt();
                using (StreamWriter file = new StreamWriter(FilePath))
                {
                    file.WriteLine(Data);
                }
            }
        }
        public void WriteRecord(List<string> Data)
        {
            if (CheckFile())
            {
                ClearTxt();
                using (StreamWriter file = new StreamWriter(FilePath))
                {
                    foreach (string item in Data)
                    {
                        file.WriteLine(item);
                    }
                }
            }
        }
        public string ReadRecord()
        {
            string Record = "";
            if (!CheckFile())
            {
                return Record;
            }
            using (StreamReader file = new StreamReader(FilePath))
            {
                Record = file.ReadLine();
            }
            return Record;

        }
        public List<string> ReadRecordList()
        {
            List<string> Record = new List<string>();
            string RecordLine;

            try
            {
                if (!CheckFile())
                {
                    return Record;
                }
                using (StreamReader file = new StreamReader(FilePath))
                {
                    while ((RecordLine = file.ReadLine()) != null)
                    {
                        Record.Add(RecordLine);
                    }
                }
                return Record;
            }
            catch (Exception)
            {               
                return Record;
            }

            

        }

        private bool CheckFile()
        {
            if (!File.Exists(FilePath))
            {
                using (FileStream fs = File.Create(FilePath))
                {

                }
                return false;
            }
            return true;
        }
        private void ClearTxt()
        {
            using (FileStream stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
            }
        }
    }
}
