using System;
using System.Collections.Generic;
using System.Text;

namespace ExifRenamer.model
{
    class RenameInfo
    {
        public RenameInfo(string newName, DateTime minDate)
        {
            NewFilePath = newName;
            MinDate = minDate;
        }
        public string NewFilePath { get; set; }
        public DateTime MinDate { get; set; }
    }
}
