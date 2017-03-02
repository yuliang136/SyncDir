using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HandleSvn2Git
{
    // 比较结构.
    public struct CompareStruct
    {
        public string sourceFileName;
        public string destFileName;

        public CompareStruct(string strSourceFileName, string strDestFileName)
        {
            this.sourceFileName = strSourceFileName;
            this.destFileName = strDestFileName;
        }
    }

    class HandleSvn2Git
    {
        public string m_svnPath = string.Empty;             // svn目录路径.
        public string m_gitPath = string.Empty;             // git目录路径.
        public string m_gitBranchName = string.Empty;       // git分支名称.
        public string m_compareName = string.Empty;         // BeyondCompare Session Name.
        public int m_threadNum = 0;                         // 线程总数.

        public Int32 m_fileNum = 0;                         // 文件总数.
        public Int32 m_curFile = 1;                         // 当前处理文件Index.

        // 比较总数.
        public List<CompareStruct> m_TotalCompareFiles = new List<CompareStruct>();

        public void Logout(string strMessage, string strType)
        {
            string strShow = string.Format("{0} {1}", strType, strMessage);
            Console.WriteLine(strShow);
        }

        public void Run(string[] strParams)
        {
            //Console.WriteLine(strParams[1]);
            //Console.WriteLine(strParams[2]);

            //Console.WriteLine("Here");
            m_svnPath = strParams[1];
            m_gitPath = strParams[2];
            m_gitBranchName = strParams[3];
            m_compareName = strParams[4];
            m_threadNum = Convert.ToInt32(strParams[5]);

            //Console.WriteLine(m_svnPath);
            //Console.WriteLine(m_gitPath);
            //Console.WriteLine(m_gitBranchName);
            //Console.WriteLine(m_compareName);



            HandleSvnPath(m_svnPath);
            HandleGitPath(m_gitPath, m_gitBranchName);

            // 同步目录.
            SyncFolder(m_svnPath, m_gitPath);

            HandleGitCommit(m_gitPath);

            if (m_compareName != "none")
            {
                OpenCompareTool(m_compareName);
            }
        }

        /// <summary>
        /// 打开比较工具.
        /// </summary>
        /// <param name="strCompareName"></param>
        private void OpenCompareTool(string strCompareName)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;            //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;       //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;      //由调用程序获取输出信息
            p.StartInfo.CreateNoWindow = true;              //不显示程序窗口
            p.Start();                                      //启动程序

            string strGitPush = string.Format("BComp {0}", strCompareName);
            p.StandardInput.WriteLine("{0} ", strGitPush);

            p.StandardInput.WriteLine("exit");
            p.StandardInput.AutoFlush = true;

            string output = p.StandardOutput.ReadToEnd();

            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();

            Console.WriteLine(output);
        }

        private void HandleGitCommit(string strGitPath)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;            //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;       //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;      //由调用程序获取输出信息
            p.StartInfo.CreateNoWindow = true;              //不显示程序窗口
            p.Start();                                      //启动程序

            string strChangeDir = string.Format("cd /d {0}", strGitPath);
            p.StandardInput.WriteLine("{0} ", strChangeDir);

            string strGitAdd = string.Format("git add .");
            p.StandardInput.WriteLine("{0} ", strGitAdd);

            // 取得当前系统时间.
            DateTime currentTime = DateTime.Now;
            string strShowTime = currentTime.ToString();
            string strComment = string.Format("update from svn at {0}", strShowTime);
            string strGitCommit = string.Format("git commit -m \"{0}\"", strComment);
            p.StandardInput.WriteLine("{0} ", strGitCommit);

            string strGitPush = string.Format("git push");
            p.StandardInput.WriteLine("{0} ", strGitPush);

            p.StandardInput.WriteLine("exit");
            p.StandardInput.AutoFlush = true;

            string output = p.StandardOutput.ReadToEnd();

            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();

            Console.WriteLine(output);
        }

        private void SyncFolder(string strSourcePath, string strDestPath)
        {
            // Calculate Total File Number
            Console.WriteLine("Calculate Total File Number....");



            CalculateTotalNum(strSourcePath);
            CalculateTotalNum(strDestPath);
            //Console.WriteLine(m_fileNum.ToString());

            //sw.Stop();
            //TimeSpan ts2 = sw.Elapsed;
            //Console.WriteLine("Stopwatch cost {0}ms.", ts2.TotalMilliseconds);

            IterateSourcePath(strSourcePath, strDestPath);
            IterateDestPath(strSourcePath, strDestPath);

            // 只统计多线程运行时间.
            Console.WriteLine("Start Compare Files....");
            Stopwatch sw = new Stopwatch();
            sw.Start();


            int nThreadNum = m_threadNum;

            List<HandleUnit> HUnitList = new List<HandleUnit>();
            // 生成多个HandleUnit分配处理数据.
            for (int i = 0; i < nThreadNum; i++)
            {
                HandleUnit hu = new HandleUnit();
                HUnitList.Add(hu);
            }

            int nIndex = 0;
            for (int i = 0; i < m_TotalCompareFiles.Count; i++)
            {
                // 求余数.
                nIndex = (i % nThreadNum);
                CompareStruct cs = new CompareStruct(
                                                        m_TotalCompareFiles[i].sourceFileName,
                                                        m_TotalCompareFiles[i].destFileName
                                                    );

                HUnitList[nIndex].m_CompareData.Add(cs);
            }

            // 设置线程.
            List<Thread> threadList = new List<Thread>();
            for (int i = 0; i < HUnitList.Count; i++)
            {
                Thread HandleThread;          // 发送数据的线程对象.

                HandleUnit each = HUnitList[i];

                HandleThread = new Thread(new ThreadStart(each.StartCompare));

                threadList.Add(HandleThread);
            }
            
            // 同时启动线程.
            for (int i = 0; i < HUnitList.Count; i++)
            {
                Thread each = threadList[i];

                each.Start();
            }

            bool bExit = false;
            while (false == bExit)
            {
                bExit = CheckStop(threadList);
            }

            sw.Stop();
            TimeSpan ts2 = sw.Elapsed;
            Console.WriteLine("FileCompare finished cost {0}s.", ts2.TotalSeconds);

            //Console.WriteLine("Finished");

            // Git Commit操作.

        }

        /// <summary>
        /// 检测线程是否结束
        /// </summary>
        /// <param name="threadList"></param>
        /// <returns></returns>
        private bool CheckStop(List<Thread> threadList)
        {
            bool bRtn = true;

            int num = threadList.Count;
            for (int i = 0; i < num; i++)
            {
                Thread each = threadList[i];
                bool bLive = each.IsAlive;

                if (true == bLive)
                {
                    bRtn = false;
                    break;
                }
            }
            return bRtn;
        }

        private void HandleDestFile(string strSourceFileName, string strDestFileName)
        {
            UpdateHandleTime();

            // 判断同步目标里面的文件 在源文件里是否还存在
            // 如果在源文件里不存在 则删除目标文件
            if (!File.Exists(strSourceFileName))
            {
                File.Delete(strDestFileName);
                Logout(strDestFileName, "Del");
            }
        }

        private void IterateDestPath(string strSourcePath, string strDestPath)
        {
            string destFileName = string.Empty;
            string sourceFileName = string.Empty;
            string sourcePath = string.Empty;

            // 遍历Dest目录进行处理.
            DirectoryInfo diDest = new DirectoryInfo(strDestPath);

            // 处理子目录.
            DirectoryInfo[] diDestInfos = diDest.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var eachDestDi in diDestInfos)
            {
                // 过滤文件夹.
                if (
                    eachDestDi.Name == ".svn" ||
                    eachDestDi.Name == ".git"
                    )
                {
                    continue;
                }

                // 这里可能sourcePath目录整体都没有 需要删除destFullPath.
                // 组合出sourcePath子目录
                sourcePath = Path.Combine(strSourcePath, eachDestDi.Name);
                if (!Directory.Exists(sourcePath))
                {
                    // 不存在时 删除destPath.
                    Directory.Delete(eachDestDi.FullName, true);
                }
                else
                {
                    // 递归目录.
                    IterateDestPath(sourcePath, eachDestDi.FullName);
                }
            }

            // 处理所有文件.
            FileInfo[] destFileInfos = diDest.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach (var eachDestFileInfo in destFileInfos)
            {
                // 过滤.gitignore README.md
                if (
                    eachDestFileInfo.Name == ".gitignore" ||
                    eachDestFileInfo.Name == "README.md"
                    )
                {
                    continue;
                }

                // 组合sourceFileName. 测试这个文件在source里是否存在.
                sourceFileName = Path.Combine(strSourcePath, eachDestFileInfo.Name);
                HandleDestFile(sourceFileName, eachDestFileInfo.FullName);
            }
        }

        private void IterateSourcePath(string strSourcePath, string strDestPath)
        {
            string destFileName = string.Empty;
            string sourceFileName = string.Empty;
            string destPath = string.Empty;

            // 遍历Source目录下所有文件. 替换Dest外层目录进行查找.
            // 没有文件时进行复制. 文件不同时 进行复制覆盖.
            DirectoryInfo diSource = new DirectoryInfo(strSourcePath);

            // 处理子目录.
            DirectoryInfo[] diSources = diSource.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var eachDiSourceInfo in diSources)
            {
                if (
                    eachDiSourceInfo.Name == ".svn" ||
                    eachDiSourceInfo.Name == ".git"
                    )
                {
                    continue;
                }

                // 判断destPath是否为空,创建目录.
                destPath = Path.Combine(strDestPath, eachDiSourceInfo.Name);
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }

                IterateSourcePath(eachDiSourceInfo.FullName, destPath);
            }

            // 处理所有文件.
            FileInfo[] sourceFileInfos = diSource.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach (var eachSourceFileInfo in sourceFileInfos) 
            {
                // 生成目的文件地址.
                destFileName = Path.Combine(strDestPath, eachSourceFileInfo.Name);
                HandleSourceFile(eachSourceFileInfo.FullName, destFileName);
            }


        }

        private void UpdateHandleTime()
        {
            //string strShow = string.Format("{0}/{1}", m_curFile, m_fileNum);

            //double dPercent = (double)(m_curFile) / m_fileNum;
            //dPercent = dPercent * 100;
            //dPercent = Math.Round(dPercent,0);

            //string strShow = string.Format("{0}%", dPercent);

            //Console.WriteLine(strShow);

            //m_curFile++;
        }

        private void HandleSourceFile(string strSourceFileName, string strDestFileName)
        {
            UpdateHandleTime();

            // 判断strDestFileName是否存在.
            if (!File.Exists(strDestFileName))
            {
                File.Copy(strSourceFileName, strDestFileName);
                Logout(strDestFileName, "Add");
            }
            else
            {
                // 先不比较文件 记录下来 统计计算时间.
                CompareStruct cs = new CompareStruct(strSourceFileName, strDestFileName);
                m_TotalCompareFiles.Add(cs);

                //// 判断两个文件是否一致.
                //if (!FileCompareByte(strSourceFileName, strDestFileName))
                //{
                //    File.Copy(strSourceFileName, strDestFileName, true);
                //    Logout(strDestFileName, "Update");
                //}
            }

        }

        private void CalculateTotalNum(string strCalculatePath)
        {
            DirectoryInfo di = new DirectoryInfo(strCalculatePath);


            // 处理目录下所有目录. 过滤掉 .svn .git两个目录.
            DirectoryInfo[] dis = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var directoryInfo in dis)
            {
                if (
                    directoryInfo.Name == ".svn" ||
                    directoryInfo.Name == ".git"
                    )
                {
                    continue;
                }

                CalculateTotalNum(directoryInfo.FullName);
            }


            // 处理目录下所有文件. 过滤掉.gitignore README.md
            FileInfo[] fileInfos = di.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach (var fileInfo in fileInfos)
            {
                if (
                    fileInfo.Name == ".gitignore" ||
                    fileInfo.Name == "README.md")
                {
                    continue;
                }

                m_fileNum++;
            }
        }

        private void HandleGitPath(string strGitPath, string strBranchName)
        {
            // 切换到Git目录.


            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;            //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;       //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;      //由调用程序获取输出信息
            p.StartInfo.CreateNoWindow = true;              //不显示程序窗口
            p.Start();                                      //启动程序


            //string strShowDir = string.Empty;
            //strShowDir = string.Format("dir/w");
            //p.StandardInput.WriteLine(strShowDir);

            string strChangeDir = string.Format("cd /d {0}", strGitPath);
            p.StandardInput.WriteLine("{0} ", strChangeDir);

            //p.StandardInput.WriteLine(strShowDir);

            string strReset = string.Format("git reset --hard HEAD");
            p.StandardInput.WriteLine("{0} ", strReset);

            string strCleanUp = string.Format("git clean -df");
            p.StandardInput.WriteLine("{0} ", strCleanUp);

            string strCheckout = string.Format("git checkout {0}", strBranchName);
            p.StandardInput.WriteLine("{0} ", strCheckout);

            p.StandardInput.WriteLine("exit");
            p.StandardInput.AutoFlush = true;

            string output = p.StandardOutput.ReadToEnd();

            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();

            Console.WriteLine(output);
        }

        private void HandleSvnPath(string strSvnPath)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;            //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;       //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;      //由调用程序获取输出信息
            p.StartInfo.CreateNoWindow = true;              //不显示程序窗口
            p.Start();                                      //启动程序


            string strRevert = string.Format("svn revert -R {0}", strSvnPath);
            p.StandardInput.WriteLine("{0} ",strRevert);

            string strCleanUp = string.Format("svn cleanup --remove-unversioned --remove-ignored {0}", strSvnPath);
            p.StandardInput.WriteLine("{0} ", strCleanUp);

            string strUpdate = string.Format("svn update {0}", strSvnPath);
            p.StandardInput.WriteLine("{0} ", strUpdate);


            p.StandardInput.WriteLine("exit");
            p.StandardInput.AutoFlush = true;

            string output = p.StandardOutput.ReadToEnd();

            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();

            Console.WriteLine(output);
        }
    }
}
