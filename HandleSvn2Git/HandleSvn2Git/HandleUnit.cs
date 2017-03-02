using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HandleSvn2Git
{
    class HandleUnit
    {
        // 处理的数据块.
        public List<CompareStruct> m_CompareData = new List<CompareStruct>();

        public void StartCompare()
        {
            //Console.WriteLine("StartCompare()");

            //// 判断两个文件是否一致.
            //if (!FileCompareByte(strSourceFileName, strDestFileName))
            //{
            //    File.Copy(strSourceFileName, strDestFileName, true);
            //    Logout(strDestFileName, "Update");
            //}
            string strSourceFileName = string.Empty;
            string strDestFileName = string.Empty;
            string strShow = string.Empty;

            for (int i = 0; i < m_CompareData.Count; i++)
            {
                strShow = string.Format("{0} / {1}", i, m_CompareData.Count);
                Console.WriteLine(strShow);

                CompareStruct eachCompareData = m_CompareData[i];
                strSourceFileName = eachCompareData.sourceFileName;
                strDestFileName = eachCompareData.destFileName;

                if (!FileCompareByte(strSourceFileName, strDestFileName))
                {
                    File.Copy(strSourceFileName, strDestFileName, true);
                    //Logout(strDestFileName, "Update");
                }
            }

            //foreach (var eachCompareData in m_CompareData)
            //{
            //    // 每个线程单独打印自己的进度.


            //    strSourceFileName = eachCompareData.sourceFileName;
            //    strDestFileName = eachCompareData.destFileName;

            //    if (!FileCompareByte(strSourceFileName, strDestFileName))
            //    {
            //        File.Copy(strSourceFileName, strDestFileName, true);
            //        //Logout(strDestFileName, "Update");
            //    }
            //}
        }

        private bool FileCompareMD5(string fileA, string fileB)
        {
            bool bRtn = false;

            string fileAMD5 = GetMD5HashFromFile(fileA);
            string fileBMD5 = GetMD5HashFromFile(fileB);

            bRtn = (fileAMD5 == fileBMD5);

            return bRtn;
        }

        private string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            ASCIIEncoding enc = new ASCIIEncoding();
            return enc.GetString(retVal);
        }


        private bool FileCompareByte(string strFileA, string strFileB)
        {
            if (strFileA == strFileB)
            {
                return true;
            }

            int nFileAbyte = 0;
            int nFileBbyte = 0;

            FileStream fsA = new FileStream(strFileA, FileMode.Open);
            FileStream fsB = new FileStream(strFileB, FileMode.Open);

            if (fsA.Length != fsB.Length)
            {
                fsA.Close();
                fsB.Close();

                return false;
            }

            do
            {
                nFileAbyte = fsA.ReadByte();
                nFileBbyte = fsB.ReadByte();

            }
            while ((nFileAbyte == nFileBbyte) && (nFileAbyte != -1));

            fsA.Close();
            fsB.Close();

            return ((nFileAbyte - nFileBbyte) == 0);
        }

    }
}
