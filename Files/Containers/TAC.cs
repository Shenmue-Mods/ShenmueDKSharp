using ShenmueDKSharp.Core;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// TAC file container.
    /// Used to create an filestream of the TAC file and making access to the files inside it simple.
    /// Not deriving from the BaseFile.
    /// </summary>
    public class TAC : IProgressable
    {
        private Stream m_tacStream;
        private bool m_abort;

        public TAD TAD;

        public bool IsAbortable => true;
        public event FinishedEventHandler Finished;
        public event ProgressChangedEventHandler ProgressChanged;
        public event DescriptionChangedEventHandler DescriptionChanged;
        public event LoadErrorEventHandler Error;

        public TAC() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TAC"/> class.
        /// </summary>
        /// <param name="tadFile">The TAD file partner.</param>
        /// <param name="tacFilepath">The TAC filepath.</param>
        public TAC(TAD tadFile, string tacFilepath = "")
        {
            TAD = tadFile;
            TAD.AssignFileNames();
            if (String.IsNullOrEmpty(tacFilepath))
            {
                tacFilepath = Path.ChangeExtension(TAD.FilePath, ".tac");
            }
            m_tacStream = new FileStream(tacFilepath, FileMode.Open);
        }

        /// <summary>
        /// Unpacks all the files.
        /// </summary>
        /// <param name="verbose">True for outputing every filename.</param>
        /// <param name="raymonf">True for using raymonf's wulinshu database else the cached database is used which is faster.</param>
        /// <param name="folder">The output folder. When empty it will be extracted in a folder next to the TAD file.</param>
        public void Unpack(bool verbose = false, bool raymonf = false, string folder = "")
        {
            m_abort = false;
            if (String.IsNullOrEmpty(folder))
            {
                folder = Path.GetDirectoryName(TAD.FilePath) + "\\_" + TAD.FileName + "_";
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (DescriptionChanged != null) {
                DescriptionChanged(this, new DescriptionChangedArgs("Unpacking tac..."));
            }

            int counter = 0;
            foreach (TADEntry entry in TAD.Entries)
            {
                if (m_abort) break;
                if (ProgressChanged != null) {
                    ProgressChanged(this, new ProgressChangedArgs(counter, TAD.Entries.Count));
                }

                if (raymonf)
                {
                    entry.FileName = Wulinshu.GetFilenameFromHash(entry.FirstHash);
                }
                else
                {
                    entry.FileName = FilenameDatabase.GetFilename(entry.FirstHash, entry.SecondHash);
                }

                if (verbose)
                {
                    Console.WriteLine("[TAC/TAD] Offset: {1}, Size: {2} -> Filepath: {0}", entry.FileName, entry.FileOffset, entry.FileSize);
                }

                m_tacStream.Seek(entry.FileOffset, SeekOrigin.Begin);
                byte[] fileBuffer = new byte[entry.FileSize];
                m_tacStream.Read(fileBuffer, 0, fileBuffer.Length);

                entry.FilePath = "";
                if (String.IsNullOrEmpty(entry.FileName))
                {
                    entry.FilePath = String.Format("{0}\\{1}", folder, counter.ToString());
                }
                else
                {
                    entry.FilePath = entry.FileName.Replace('/', '\\');
                    entry.FilePath = folder + "\\" + entry.FilePath;

                    string dir = Path.GetDirectoryName(entry.FilePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                using (FileStream entryStream = File.Create(entry.FilePath))
                {
                    entryStream.Write(fileBuffer, 0, fileBuffer.Length);
                }
                counter++;
            }
            if (ProgressChanged != null) {
                Finished(this, new FinishedArgs(true));
            }
        }

        /// <summary>
        /// Packs the given entries to the given TAC file based on the TAD file this instance has.
        /// </summary>
        public void Pack(string tacFilepath, List<TADEntry> entries = null)
        {
            m_abort = false;
            if (entries == null)
            {
                entries = TAD.Entries;
            }

            DescriptionChanged(this, new DescriptionChangedArgs("Packing tac..."));

            using (FileStream stream = File.Create(tacFilepath))
            {
                TAD.FileCount = 0;
                foreach (TADEntry entry in entries)
                {
                    if (m_abort) break;

                    ProgressChanged(this, new ProgressChangedArgs((int)TAD.FileCount, entries.Count));

                    TAD.FileCount++;
                    if (String.IsNullOrEmpty(entry.FilePath))
                    {
                        throw new ArgumentException("TAD entry was missing the source filepath!");
                    }

                    byte[] buffer;
                    using (FileStream entryStream = File.Open(entry.FilePath, FileMode.Open))
                    {
                        buffer = new byte[entryStream.Length];
                        entryStream.Read(buffer, 0, buffer.Length);

                        entry.FileOffset = (uint)stream.Position;
                        entry.FileSize = (uint)buffer.Length;
                    }
                    stream.Write(buffer, 0, buffer.Length);
                }
                TAD.TacSize = (uint)stream.Length;
                TAD.UnixTimestamp = DateTime.UtcNow + TimeSpan.FromDays(365 * 5);
            }
            Finished(this, new FinishedArgs(true));
        }

        /// <summary>
        /// Returns the file buffer of the given TAD entry.
        /// </summary>
        public byte[] GetFileBuffer(TADEntry entry)
        {
            if (m_tacStream == null)
            {
                throw new InvalidOperationException("The TAC filestream was already closed!");
            }

            byte[] result = new byte[entry.FileSize];
            m_tacStream.Seek(entry.FileOffset, SeekOrigin.Begin);
            m_tacStream.Read(result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// Trys to get the file buffer of the given filename.
        /// Returns the first found entries buffer that contains the given filename.
        /// Returns null when no file could be found.
        /// </summary>
        /// <param name="filename">The filename that will be searched for inside the TAD file.</param>
        public byte[] GetFileBuffer(string filename)
        {
            filename = filename.ToLower();
            foreach (TADEntry entry in TAD.Entries)
            {
                if (entry.FileName.Contains(filename))
                {
                    return GetFileBuffer(entry);
                }
            }
            return null;
        }

        /// <summary>
        /// Closes the filestream resulting in an useless TAC object.
        /// </summary>
        public void Close()
        {
            m_tacStream.Close();
            m_tacStream = null;
        }

        public void Abort()
        {
            m_abort = true;
        }

        ~TAC()
        {
            if (m_tacStream != null)
            {
                Close();
            }
        }
    }
}
