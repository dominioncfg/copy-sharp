using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace CopyCore
{
    public static class FileComparison
    {
        /// <summary>
        /// Comparar a fondo los archivos
        /// </summary>
        /// <param name="FileA"></param>
        /// <param name="FileB"></param>
        /// <returns></returns>
        public static bool DeepCompare(string FileA, string FileB)
        {
            using (HashAlgorithm hashAlg = HashAlgorithm.Create())
            {
                using (FileStream fsA = new FileStream(FileA, FileMode.Open), fsB = new FileStream(FileB, FileMode.Open))
                {
                    // Calculate the hash for the files.
                    byte[] hashBytesA = hashAlg.ComputeHash(fsA);
                    byte[] hashBytesB = hashAlg.ComputeHash(fsB);
                    // Compare the hashes.
                    if (BitConverter.ToString(hashBytesA) == BitConverter.ToString(hashBytesB))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Comparar de manera rapida los archivos.
        /// </summary>
        /// <param name="FileA"></param>
        /// <param name="FileB"></param>
        /// <returns></returns>
        public static bool ShallowCompare(string FileA, string FileB)
        {
            using (FileStream fsA = new FileStream(FileA, FileMode.Open), fsB = new FileStream(FileB, FileMode.Open))
            {
                if (fsA.Length!=fsB.Length)
                {
                    return false;
                }

                for (int i = 0; i < 101; i++)
                {
                    int byteA = fsA.ReadByte();
                    int byteB = fsB.ReadByte();

                    if (byteA!=byteB)
                    {
                        return false;
                    }
                }
                for (int offset = 1; offset <= 101; offset++)
                {
                    fsA.Seek(-offset, SeekOrigin.End);
                    fsB.Seek(-offset, SeekOrigin.End);
                    int byteA = fsA.ReadByte();
                    int byteB = fsA.ReadByte();
                    if (byteA!=byteB)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
