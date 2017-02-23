﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace FastConHost
{
    /// <summary>
    ///
    /// </summary>
    public class MFTScanner
    {
        private static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const uint GENERIC_READ = 0x80000000;
        private const int FILE_SHARE_READ = 0x1;
        private const int FILE_SHARE_WRITE = 0x2;
        private const int OPEN_EXISTING = 3;
        private const int FILE_READ_ATTRIBUTES = 0x80;
        private const int FILE_NAME_IINFORMATION = 9;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
        private const int FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
        private const int FILE_OPEN_BY_FILE_ID = 0x2000;
        private const int FILE_OPEN = 0x1;
        private const int OBJ_CASE_INSENSITIVE = 0x40;
        private const int FSCTL_ENUM_USN_DATA = 0x900b3;

        [StructLayout(LayoutKind.Sequential)]
        private struct MFT_ENUM_DATA
        {
            public long StartFileReferenceNumber;
            public long LowUsn;
            public long HighUsn;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct USN_RECORD
        {
            public int RecordLength;
            public short MajorVersion;
            public short MinorVersion;
            public long FileReferenceNumber;
            public long ParentFileReferenceNumber;
            public long Usn;
            public long TimeStamp;
            public int Reason;
            public int SourceInfo;
            public int SecurityId;
            public FileAttributes FileAttributes;
            public short FileNameLength;
            public short FileNameOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_STATUS_BLOCK
        {
            public int Status;
            public int Information;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public short Length;
            public short MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public int Attributes;
            public int SecurityDescriptor;
            public int SecurityQualityOfService;
        }

        //// MFT_ENUM_DATA
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(IntPtr hDevice, int dwIoControlCode, ref MFT_ENUM_DATA lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 CloseHandle(IntPtr lpObject);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int NtCreateFile(ref IntPtr FileHandle, int DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes, ref IO_STATUS_BLOCK IoStatusBlock, int AllocationSize, int FileAttribs, int SharedAccess, int CreationDisposition, int CreateOptions, int EaBuffer,
        int EaLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int NtQueryInformationFile(IntPtr FileHandle, ref IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, int Length, int FileInformationClass);

        private IntPtr m_hCJ;
        private IntPtr m_Buffer;
        private int m_BufferSize;

        private string m_DriveLetter;

        private class FSNode
        {
            public long FRN;
            public long ParentFRN;
            public string FileName;
            public string FullDirectory;

            public bool IsFile;
            public FSNode(long lFRN, long lParentFSN, string sFileName, bool bIsFile)
            {
                FRN = lFRN;
                ParentFRN = lParentFSN;
                FileName = sFileName;
                IsFile = bIsFile;
            }
        }

        private IntPtr OpenVolume(string szDriveLetter)
        {

            IntPtr hCJ = default(IntPtr);
            //// volume handle

            m_DriveLetter = szDriveLetter;
            hCJ = CreateFile(@"\\.\" + szDriveLetter, 
                GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, 
                IntPtr.Zero, OPEN_EXISTING, 
                0, 
                IntPtr.Zero);

            return hCJ;

        }


        private void Cleanup()
        {
            if (m_hCJ != IntPtr.Zero)
            {
                // Close the volume handle.
                CloseHandle(m_hCJ);
                m_hCJ = INVALID_HANDLE_VALUE;
            }

            if (m_Buffer != IntPtr.Zero)
            {
                // Free the allocated memory
                Marshal.FreeHGlobal(m_Buffer);
                m_Buffer = IntPtr.Zero;
            }
        }

        private Dictionary<long, FSNode> dicFRNLookup = new Dictionary<long, FSNode>();
        private Dictionary<long, FileNodeInfo> FileLookUp = new Dictionary<long, FileNodeInfo>();
        private Dictionary<long, DirectoryNodeInfo> DirLookUp = new Dictionary<long, DirectoryNodeInfo>();
        public IEnumerable<String> BuildIndex(string szDriveLetter, HashSet<string> excludeDirectories = null)
        {
            return BuildIndex(null, szDriveLetter, excludeDirectories);
        }

        public IEnumerable<String> BuildIndex(BackgroundWorker bw, string szDriveLetter, HashSet<string> excludeDirectories = null)
        {
            var ci = CultureInfo.CurrentCulture;
            try
            {
                var usnRecord = default(USN_RECORD);
                var mft = default(MFT_ENUM_DATA);
                var dwRetBytes = 0;
                var cb = 0;
                var bIsFile = false;

                // This shouldn't be called more than once.
                if (m_Buffer.ToInt32() != 0)
                {
                    throw new Exception("invalid buffer");
                }

                // Assign buffer size
                m_BufferSize = 65536;
                //64KB

                // Allocate a buffer to use for reading records.
                m_Buffer = Marshal.AllocHGlobal(m_BufferSize);

                // correct path
                szDriveLetter = szDriveLetter.TrimEnd('\\');

                // Open the volume handle 
                m_hCJ = OpenVolume(szDriveLetter);

                // Check if the volume handle is valid.
                if (m_hCJ == INVALID_HANDLE_VALUE)
                {
                    string errorMsg = "Couldn't open handle to the volume.";
                    if (!IsAdministrator())
                        errorMsg += "Current user is not administrator";

                    throw new Exception(errorMsg);
                }

                mft.StartFileReferenceNumber = 0;
                mft.LowUsn = 0;
                mft.HighUsn = long.MaxValue;

                string previousDirectory = null;
                do
                {
                    if (DeviceIoControl(m_hCJ, FSCTL_ENUM_USN_DATA, ref mft, Marshal.SizeOf(mft), m_Buffer, m_BufferSize, ref dwRetBytes, IntPtr.Zero))
                    {
                        cb = dwRetBytes;
                        // Pointer to the first record
                        IntPtr pUsnRecord = new IntPtr(m_Buffer.ToInt32() + 8);

                        while ((dwRetBytes > 8))
                        {
                            // Copy pointer to USN_RECORD structure.
                            usnRecord = (USN_RECORD)Marshal.PtrToStructure(pUsnRecord, usnRecord.GetType());

                            // The filename within the USN_RECORD.
                            string FileName = Marshal.PtrToStringUni(new IntPtr(pUsnRecord.ToInt32() + usnRecord.FileNameOffset), usnRecord.FileNameLength / 2);

                            bIsFile = !usnRecord.FileAttributes.HasFlag(FileAttributes.Directory);
                            FSNode fs = new FSNode(usnRecord.FileReferenceNumber, usnRecord.ParentFileReferenceNumber, FileName, bIsFile);
                            String topPar = null;
                            FSNode pa = fs;
                            FSNode temp = fs;
                            String partPath = null;

                            while (dicFRNLookup.TryGetValue(temp.ParentFRN, out temp))
                            {
                                pa = temp;
                                partPath = string.Concat(temp.FileName, @"\", partPath);
                            }

                            if (!pa.IsFile)
                            {
                                if (string.IsNullOrEmpty(partPath))
                                {
                                    partPath = pa.FileName;
                                    topPar = pa.FileName;
                                }
                                else
                                {
                                    topPar = pa.FileName;
                                }
                            }

                            string fullDirectory = string.Concat(szDriveLetter, @"\", partPath);
                            if (bw != null && !fullDirectory.Equals(previousDirectory))
                            {
                                previousDirectory = fullDirectory;
                                bw.ReportProgress(0, fullDirectory);
                            }

                            long refnum = usnRecord.FileReferenceNumber;


                            // Pointer to the next record in the buffer.
                            pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usnRecord.RecordLength);

                            dwRetBytes -= usnRecord.RecordLength;
                            if ((excludeDirectories != null && !string.IsNullOrEmpty(partPath) && isExcluding(excludeDirectories, partPath)))
                                continue;
                            if (!string.IsNullOrEmpty(partPath) && partPath.StartsWith("$"))
                                continue;

                            fs.FullDirectory = fullDirectory;
                            dicFRNLookup.Add(refnum, fs);

                            var sFullPath = string.Concat(fs.FullDirectory, fs.FileName);
                            if (fs.IsFile)
                            {
                                if (!File.Exists(sFullPath))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if(!Directory.Exists(sFullPath))
                                {
                                    continue;
                                }
                            }

                            yield return sFullPath;
                        }

                        // The first 8 bytes is always the start of the next USN.
                        mft.StartFileReferenceNumber = Marshal.ReadInt64(m_Buffer, 0);

                    }
                    else
                    {
                        break; // TODO: might not be correct. Was : Exit Do

                    }

                } while (!(cb <= 8));
            }
            finally
            {
                //// cleanup
                Cleanup();
            }
        }

        private bool isExcluding(HashSet<string> excludeDirectories, string sFullPath)
        {
            //if (sFullPath.StartsWith("$RECYCLE")) {
            //    Trace.WriteLine(sFullPath);
            //}
            foreach (var item in excludeDirectories)
            {
                if (sFullPath.Contains(item))
                    return true;
            }
            return false;
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static class DriveInfoExtension
    {
        public static IEnumerable<String> EnumerateFiles(this DriveInfo drive, HashSet<string> excludeDirectories = null)
        {
            return (new MFTScanner()).BuildIndex(drive.Name, excludeDirectories);
        }
        public static IEnumerable<String> EnumerateFiles(this DriveInfo drive, BackgroundWorker bw, HashSet<string> excludeDirectories = null)
        {
            return (new MFTScanner()).BuildIndex(bw, drive.Name, excludeDirectories);
        }

        public static IEnumerable<string> EnumerateFiles(this DirectoryInfo self, DirectoryInfo[] direvers, HashSet<string> excludeDirectories = null)
        {
            List<string> total = new List<string>();
            foreach (DirectoryInfo dr in direvers)
            {
                try
                {
                    total.AddRange((new MFTScanner()).BuildIndex(dr.Name, excludeDirectories));
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
            return total.AsEnumerable();
        }
    }
}
