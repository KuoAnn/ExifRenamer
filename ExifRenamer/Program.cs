using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExifLibrary;
using ExifRenamer.model;

namespace ExifRenamer
{
    internal class Program
    {
        public static readonly string bakDirName = "_bak";
        public static bool isRootFolder = false;
        public static bool isNoUseTitle = false;
        public static bool isForceFileName = false;
        public static bool isNoSetPhotoTime = false;

        private static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    isRootFolder = false;
                    isNoUseTitle = false;
                    isForceFileName = false;
                    isNoSetPhotoTime = false;

                    Console.WriteLine($@"Enter Directory/File Path:
* File Name Start with ! to Force assign photoDate by yyyyMMdd_HHmmss format
* --r root folder recusivly
* --n no import folder title
* --f force fileTime by fileName
* --p no set photo time
");
                    var cmd = Console.ReadLine().TrimEnd('\\');
                    var cmds = cmd.Split(" --");
                    var rootPath = cmds[0].TrimEnd('\\');

                    foreach (var c in cmds)
                    {
                        switch (c.Trim().ToLower())
                        {
                            case "r":
                                isRootFolder = true;
                                break;

                            case "n":
                                isNoUseTitle = true;
                                break;

                            case "f":
                                isForceFileName = true;
                                break;

                            case "p":
                                isNoSetPhotoTime = true;
                                break;

                            default:
                                break;
                        }
                    }

                    //detect whether its a directory or file
                    FileAttributes fileAttributes = File.GetAttributes(rootPath);
                    string[] filePaths = new string[] { };
                    if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        //TopDirectory
                        var searchOption = isRootFolder ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        filePaths = Directory.GetFiles(rootPath, "*.*", searchOption);
                    }
                    else
                    {
                        filePaths = new string[] { rootPath };
                        rootPath = Path.GetDirectoryName(rootPath);
                    }

                    var newFileInfos = GetNewFileInfos(filePaths);

                    //backup
                    var folderName = Path.GetFileName(Path.GetDirectoryName(rootPath));
                    var bakDir = $"D:\\{bakDirName}\\{folderName}";
                    if (Directory.Exists(bakDir))
                    {
                        Directory.Delete(bakDir, true);
                        Directory.CreateDirectory(bakDir);
                    }
                    else
                    {
                        Directory.CreateDirectory(bakDir);
                    }

                    foreach (var newFileInfo in newFileInfos)
                    {
                        var fileName = Path.GetFileName(newFileInfo.Key);

                        var bakFilePath = $"{bakDir}\\{fileName}";

                        File.Copy(newFileInfo.Key, bakFilePath);
                    }
                    //rename
                    SetFileInfo(newFileInfos);

                    Console.WriteLine($"\r\nDone {newFileInfos.Count}/{filePaths.Count()}\r\n========================\r\n");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(ex);
                    Console.ResetColor();
                    Console.ReadLine();
                }
            }
        }

        private static Dictionary<string, RenameInfo> GetNewFileInfos(string[] filePaths)
        {
            var fileNames = new Dictionary<string, RenameInfo>();
            var excludeFileNames = new List<string>();
            var checkCnt = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var folderName = Path.GetFileName(Path.GetDirectoryName(filePath)).Trim();
                    var fileName = Path.GetFileName(filePath).Trim();
                    var subfileName = Path.GetExtension(filePath).ToLower().Trim();

                    if (".jpg.jpeg.png.tif.tiff.mov.mp4.mp3.gif.m4a".Contains(subfileName))
                    {
                        checkCnt++;
                        var fileReader = new FileReader(filePath, isForceFileName);

                        var minDate = fileReader.GetMinTime();

                        //new name
                        var newFileName = new StringBuilder();
                        newFileName.Append($"{minDate:yyyyMMdd_HHmmss}");

                        if (!isNoUseTitle && folderName.Contains("-"))
                        {
                            var newFolderName = folderName.Substring(folderName.LastIndexOf("-") + 1);
                            if (!string.IsNullOrEmpty(newFolderName))
                            {
                                newFileName.Append($"-{newFolderName}");
                            }
                        }

                        newFileName.Append($"{subfileName}");
                        var newFilePath = filePath.Replace(fileName, string.Empty) + newFileName;

                        fileNames.Add(filePath, new RenameInfo(newFilePath, minDate));

                        Console.WriteLine($"{fileName} >> {newFileName}");
                    }
                    else if (subfileName == ".db")
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Delete:[{filePath}]");
                    }
                    else
                    {
                        excludeFileNames.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{filePath} {ex.Message}");
                    Console.ResetColor();
                    Console.ReadLine();
                    break;
                }

                Console.WriteLine();
            }

            if (excludeFileNames.Count > 0)
            {
                Console.WriteLine($"Excluded:\r\n{string.Join("\r\n", excludeFileNames)}");
            }

            Console.WriteLine($"Check {checkCnt}/{filePaths.Length}");

            return fileNames;
        }

        private static void SetFileInfo(Dictionary<string, RenameInfo> filePaths)
        {
            var checkCnt = 0;
            foreach (var d in filePaths)
            {
                checkCnt++;
                var filePath = d.Key;
                var minDate = d.Value.MinDate;
                var newFilePath = d.Value.NewFilePath;

                var folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
                var fileName = Path.GetFileName(filePath);
                var newFileName = Path.GetFileName(newFilePath);
                var subfileName = Path.GetExtension(filePath).ToLower();

                try
                {
                    if (File.Exists(newFilePath) && fileName != newFileName)
                    {
                        //Anti-Duplicate
                        var isDuplicate = false;
                        var tmpNewFilePath = string.Empty;
                        var newMinDate = minDate;

                        do
                        {
                            var tmpFileName = Path.GetFileNameWithoutExtension(newFilePath);
                            newMinDate = newMinDate.AddSeconds(1);

                            tmpNewFilePath = filePath.Replace(fileName, string.Empty) +
                                $"{tmpFileName.Replace(minDate.ToString("yyyyMMdd_HHmmss"), newMinDate.ToString("yyyyMMdd_HHmmss"))}{subfileName}";

                            isDuplicate = File.Exists(tmpNewFilePath);
                        } while (isDuplicate);

                        newFileName = Path.GetFileName(tmpNewFilePath);
                        minDate = newMinDate;
                    }

                    //Set Exif photo datetime
                    if (".jpg,.jpeg,.tiff".Contains(subfileName) && !isNoSetPhotoTime)
                    {
                        var imgFile = ImageFile.FromFile(filePath);
                        imgFile.Properties.Set(ExifTag.DateTimeOriginal, minDate);

                        imgFile.Save(filePath);
                    }

                    //Set File info.
                    File.SetCreationTime(filePath, minDate.ToUniversalTime());
                    File.SetLastWriteTimeUtc(filePath, minDate.ToUniversalTime());

                    //Rename
                    if (fileName != newFileName)
                    {
                        Console.Write($"[{checkCnt}/{filePaths.Count}] {fileName} > {newFileName} ... ");

                        FileInfo file = new FileInfo(filePath);
                        file.Rename(newFileName.ToString());
                        Console.WriteLine("Done");
                    }
                    else
                    {
                        Console.WriteLine($"[{checkCnt}/{filePaths.Count}] {fileName} ... Pass");
                    }


                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[{filePath}] {ex.Message}");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            }
        }
    }

    public static class FileInfoExtensions
    {
        public static void Rename(this FileInfo fileInfo, string newName)
        {
            fileInfo.MoveTo(Path.Combine(fileInfo.Directory.FullName, newName));
        }
    }
}