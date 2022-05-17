using System.IO;

namespace Xpand.Extensions.Tracing {


    /// <summary>
    /// Interface facade representing the file system and the operations of System.IO.File and System.IO.Directory.
    /// </summary>
    public interface IFileSystem {
        /// <summary>
        /// Opens a System.IO.FileStream on the specified path, 
        /// having the specified mode with read, write, or read/write access
        /// and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A value specifying the type of access other threads have to the file.</param>
        /// <returns></returns>
        Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
    }

    /// <summary>
    /// Adapter that wraps System.IO.File and System.IO.Directory, allowing them to be substituted.
    /// </summary>
    public class FileSystem : IFileSystem {
        /// <summary>
        /// Opens a System.IO.FileStream on the specified path, 
        /// having the specified mode with read, write, or read/write access
        /// and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A value specifying the type of access other threads have to the file.</param>
        /// <returns></returns>
        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share) {
            return File.Open(path, mode, access, share);
        }
    }
}