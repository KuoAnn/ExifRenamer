using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExifLibrary;

namespace ExifRenamer
{
    public class FileReader
    {
        private string filePath = string.Empty;
        private int possibleMaxYear = DateTime.Now.Year;
        private int possibleMinYear = 1988;
        private bool isForceFileName = false;

        public FileReader(string filePath, bool isForceFileName)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                this.filePath = filePath;
                this.isForceFileName = isForceFileName;
            }
            else
            {
                throw new ArgumentNullException("filePath");
            }
        }

        /// <summary>
        /// 取得最小時間
        /// </summary>
        public DateTime GetMinTime()
        {
            var isForceAssign = false;
            DateTime fileNameTime = GetFileNameTime(out isForceAssign);
            if ((isForceAssign || isForceFileName) && fileNameTime != DateTime.MaxValue)
            {
                return fileNameTime;
            }

            var minTime = new DateTime();
            FileInfo fi = new FileInfo(filePath);
            DateTime createTime = fi.CreationTime;
            DateTime editTime = fi.LastWriteTime;
            DateTime accessTime = fi.LastAccessTime;

            DateTime photoTime = GetPhotoTime();

            if (photoTime != DateTime.MaxValue)
            {
                //拍攝日期優先
                minTime = photoTime;
            }
            else
            {
                //拍攝日期無效
                var datetimes = new List<DateTime>();
                datetimes.Add(createTime);
                datetimes.Add(editTime);
                datetimes.Add(accessTime);
                if (fileNameTime != DateTime.MaxValue && fileNameTime != DateTime.MinValue)
                    datetimes.Add(fileNameTime);

                minTime = datetimes.Min(x => x);
            }

            Console.WriteLine($"{filePath}");

            var isNeedCheck = false;
            if (minTime.CompareTo(photoTime) == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"P={photoTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }
            else if (photoTime != DateTime.MaxValue)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"P={photoTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }

            if (minTime.CompareTo(fileNameTime) == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"F={fileNameTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }
            else if (fileNameTime != DateTime.MaxValue)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"F={fileNameTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }

            if (minTime.CompareTo(createTime) == 0 && createTime.CompareTo(photoTime) != 0 && createTime.CompareTo(fileNameTime) != 0)
            {
                isNeedCheck = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"C={createTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"C={createTime:yyyy/MM/dd HH:mm:ss}");
            }

            if (minTime.CompareTo(editTime) == 0 && editTime.CompareTo(photoTime) != 0 && editTime.CompareTo(fileNameTime) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"E={editTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"E={editTime:yyyy/MM/dd HH:mm:ss}");
            }
            if (minTime.CompareTo(accessTime) == 0 && accessTime.CompareTo(photoTime) != 0 && accessTime.CompareTo(fileNameTime) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"A={accessTime:yyyy/MM/dd HH:mm:ss}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"A={accessTime:yyyy/MM/dd HH:mm:ss}");
            }

            if (isNeedCheck)
            {
                Console.Write($"Use Which? (Enter to Use Default) ... ");
                var keyIn = Console.ReadKey();
                Console.WriteLine();

                switch (keyIn.Key.ToString().ToUpper())
                {
                    case "P":
                        minTime = photoTime;
                        break;

                    case "F":
                        minTime = fileNameTime;
                        break;
                }
            }

            return minTime;
        }

        /// <summary>
        /// 依檔案名稱取得時間
        /// </summary>
        public DateTime GetFileNameTime(out bool isForceAssign)
        {
            isForceAssign = false;
            var fileTime = DateTime.MaxValue;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.StartsWith("!") && fileName.Length >= 16)
                {
                    //!Force assign
                    fileName = fileName.Substring(1);
                    isForceAssign = true;
                }

                //custom time format yyyyMMdd_HHmmss
                MatchCollection matches = new Regex(@"(?:19|20)\d{6}_\d{6}").Matches(fileName);
                foreach (Match match in matches)
                {
                    var formatString = "yyyyMMdd_HHmmss";
                    try
                    {
                        fileTime = DateTime.ParseExact(match.Value, formatString, null);
                        return fileTime;
                    }
                    catch (Exception) { }
                }

                //Replace [alphbet_] pattern
                fileName = new Regex(@"[^0-9,]+_+").Replace(fileName, string.Empty);

                var arr = fileName.Split(new char[] { '_', '-', ' ', '(' });
                var sb = new StringBuilder();

                foreach (var d in arr)
                {
                    if ((d.Length == 10 || d.Length == 13) && d.StartsWith("1"))
                    {
                        //timestamp format
                        double t;
                        if (double.TryParse(d, out t))
                        {
                            if (UnixTimeStampToDateTime(t, out fileTime))
                            {
                                break;
                            };
                        }
                    }

                    sb.Append(d.Trim());
                    var tmpStr = sb.ToString();
                    var maybeFormat = string.Empty;

                    switch (tmpStr.Length)
                    {
                        case 8:
                            maybeFormat = "yyyyMMdd";
                            break;
                        case 14:
                            maybeFormat = "yyyyMMddHHmmss";
                            break;
                        case 17:
                            maybeFormat = "yyyyMMddHHmmssfff";
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(maybeFormat))
                    {
                        try
                        {
                            fileTime = DateTime.ParseExact(tmpStr, maybeFormat, null);
                        }
                        catch (Exception) { }
                    }
                }

            }
            catch (Exception) { }
            finally
            {
                if (fileTime.Year > possibleMaxYear || fileTime.Year < possibleMinYear)
                {
                    fileTime = DateTime.MaxValue;
                }
            }

            return fileTime;
        }

        /// <summary>
        /// 獲取Exif中的照片拍攝日期
        /// </summary>
        /// <returns>拍攝日期</returns>
        public DateTime GetPhotoTime()
        {
            var photoTime = DateTime.MaxValue;

            try
            {
                var file = ImageFile.FromFile(filePath);
                var photoTimeProp = file.Properties.Get<ExifDateTime>(ExifTag.DateTimeOriginal);
                if (photoTimeProp != null && photoTimeProp.Value != DateTime.MinValue)
                    photoTime = photoTimeProp.Value;
            }
            catch (Exception) { }

            return photoTime;
        }
        public string GetGpsInfo()
        {
            var gpsInfo = string.Empty;

            try
            {
                var file = ImageFile.FromFile(filePath);
                // 經度 Longitude 緯度 Latitude
                var gpsLatitude = file.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLatitude);
                var gpsLatitudeRef = file.Properties.Get(ExifTag.GPSLatitudeRef);
                var gpsLongitude = file.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLongitude);
                var gpsLongitudeRef = file.Properties.Get(ExifTag.GPSLongitudeRef);

                if (gpsLatitude != null)
                    gpsInfo = gpsLatitude.Value.ToString();
            }
            catch (Exception) { }

            return gpsInfo;
        }

        private bool UnixTimeStampToDateTime(double unixTimeStamp, out DateTime dt)
        {
            var rst = false;
            dt = DateTime.MaxValue;

            try
            {
                if (unixTimeStamp.ToString().Length == 13)
                {
                    var str = unixTimeStamp.ToString().Substring(0, 10);
                    unixTimeStamp = double.Parse(str);
                }

                // Unix timestamp is seconds past epoch
                var firstDt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dt = firstDt.AddSeconds(unixTimeStamp).ToLocalTime();
                rst = true;
            }
            catch (Exception) { }

            return rst;
        }
    }
}