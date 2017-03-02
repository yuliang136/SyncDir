using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading;
using System.Drawing;

namespace MultiThreadMatch
{
    class SetFile
    {
        public String mBase;  // Base大图
        public String mDir;   // 小图目录
        public String mLog;   // Log文件

        public int mThreadNUM;

        // 每一个组运行
        public void Run()
        {
            // 对一个目录进行遍历， 对每一个文件输出一个txt
            // 并且对一个目录也尝试输出txt

            String[] files = Directory.GetFiles(mDir);
            int nFiles = files.Length;

            for (int i = 0; i < nFiles; i++)
            {
                String eachFile = files[i];

                // 对一个大图和一个小图进行匹配，并输出结果
                String strResult = myRun(mBase, eachFile, mThreadNUM);

                // 写Log
                WriteLog(strResult, mLog);

                // 删除小文件. 可以在异常状态下处理.
                File.Delete(eachFile);
            }
        }


        public String myRun(String strBase, 
                            String strMatch, 
                            int nSetThread)
        {
            String strRtn = string.Empty;
            
            // 提取文件的Row和Col组合
            String fileLocation = GetLocationForm(strMatch);

            // 按照层次位置读取文件.
            mySize BaseSize = GetPicSize(strBase);
            double[,] PicBase = new double[BaseSize.ROW, BaseSize.COL];

            mySize matchSize = GetPicSize(strMatch);
            double[,] PicMatch = new double[matchSize.ROW, matchSize.COL];

            // 把图片数字设置到分配的内存里
            SetPicData(strBase, PicBase);
            SetPicData(strMatch, PicMatch);

            // 计算移动一个像素点时 需要匹配的次数.
            MatrixCiCui nPiPeiSize = GetPiPeiTime(PicMatch, PicBase);


            // 为每个线程对象分配 区域匹配任务.
            // 计算是否单独多设置一个线程
            int HandleUnitNUM = GetThreadNum(nSetThread, nPiPeiSize);
            List<HandleUnit> HUnitList = new List<HandleUnit>();
            for (int i = 0; i < HandleUnitNUM; i++)
            {
                HandleUnit hu = new HandleUnit();
                HUnitList.Add(hu);
            }

            // 为每一个处理单元分配区域. 这里的值只是个数，从0计数
            int TotalNum = nPiPeiSize.horSize * nPiPeiSize.verSize;
            int baseTask = TotalNum / nSetThread;
            for (int i = 0; i < HandleUnitNUM; i++)
            {
                HandleUnit each = HUnitList[i];
                each.nStart = i * baseTask;
                each.nEnd = each.nStart + baseTask - 1;
                // 设置最后一个特殊值.
                if (each.nEnd > (TotalNum - 1))
                {
                    each.nEnd = TotalNum - 1;
                }


                each.mBasePic = PicBase;
                each.mMatchPic = PicMatch;
                each.mPiPeiSize = nPiPeiSize;
            }


            //设置线程.
            List<Thread> threadList = new List<Thread>();
            for (int i = 0; i < HandleUnitNUM; i++)
            {
                Thread HandleThread;          // 发送数据的线程对象.

                HandleUnit each = HUnitList[i];

                HandleThread = new Thread(new ThreadStart(each.Matching));

                threadList.Add(HandleThread);
            }



            // 同时启动线程
            for (int i = 0; i < HandleUnitNUM; i++)
            {
                Thread each = threadList[i];
                each.Start();
            }

            Console.WriteLine("Here");

            bool bExit = false;
            while (false == bExit)
            {

                bExit = CheckStop(threadList);
            }

            // 在分线程全部做完操作后 还可以整合资源

            // 从HUnitList里找出最大的值 并输出文件结果

            // 组合集合.

            Hashtable TotalHT = GetTotalHT(HUnitList);

            Location lResult = GetLocation(TotalHT);

            String strLo = string.Format("{0},{1}",lResult.row, lResult.column);

            // 写文件. 文件内容.
            strRtn = string.Format("{0} {1}", fileLocation, strLo);

            // 文件名字 大图+小图+小图位置.
            String StrLogName = GetLogName(strBase, strMatch, fileLocation);

            WriteLog(strRtn, StrLogName);

            return strRtn;
        }

        private void WriteLog(string logInfo, string StrLogName)
        {
            FileStream fs = new FileStream(StrLogName, FileMode.Append);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(logInfo);
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// 获得log文件名字 记录一个小文件和大图匹配的结果
        /// </summary>
        /// <param name="strBase"></param>
        /// <param name="strMatch"></param>
        /// <param name="fileLocation"></param>
        /// <returns></returns>
        private string GetLogName(string strBase, string strMatch, string fileLocation)
        {
            // 获得大图数字.
            String BaseName = GetFileName(strBase);


            // 获得小图数字.
            String MatchName = GetFileName(strMatch);

            // 组合
            String strRtn = string.Format("{0}-{1}.txt",BaseName, MatchName);

            return strRtn;
        }

        private string GetFileName(string strEach)
        {
            String[] parts = strEach.Split('/');


            // 用_获取 row 和 col
            String fileName = parts[parts.Length - 1];
            String[] withOutparts = fileName.Split('.');
            fileName = withOutparts[0];

            return fileName;
        }



        /// <summary>
        /// 从Log文件形成项
        /// </summary>
        /// <param name="strEach"></param>
        /// <returns></returns>
        private string GetLocationForm(string strEach)
        {
            String[] parts = strEach.Split('/');


            // 用_获取 row 和 col
            String fileName = parts[parts.Length - 1];
            String[] withOutparts = fileName.Split('.');
            fileName = withOutparts[0];

            String[] Smallparts = fileName.Split('_');


            String fileLocation = String.Format("{0},{1}",
                                                Smallparts[3],
                                                Smallparts[4]);
            return fileLocation;
        }

        /// <summary>
        /// 在Hash表里找出最大的值，返回Location.
        /// </summary>
        /// <param name="ht"></param>
        /// <returns></returns>
        private Location GetLocation(Hashtable ht)
        {
            Location lRtn = new Location();
            // 找到最大的值.
            int nNum = ht.Count;
            double dMax = 0.0;

            foreach (DictionaryEntry de in ht)
            {
                double dvalue = Convert.ToDouble(de.Value);
                if (dvalue > dMax)
                {
                    dMax = dvalue;
                }
            }
            // 返回位置.
            foreach (DictionaryEntry de in ht)
            {
                double dvalue = Convert.ToDouble(de.Value);
                if (dMax == dvalue)
                {
                    lRtn = (Location)de.Key;
                    break;
                }
            }
            return lRtn;
        }

        /// <summary>
        /// 组合HashTable的元素值.
        /// </summary>
        /// <param name="HUnitList"></param>
        /// <returns></returns>
        private Hashtable GetTotalHT(List<HandleUnit> HUnitList)
        {
            Hashtable totalHT = new Hashtable();

            int ListNUM = HUnitList.Count;
            for (int i = 0; i < ListNUM; i++)
            {
                Hashtable each = HUnitList[i].mHT;
                // 提取每一个Hashtable的值.
                foreach (DictionaryEntry item in each)
                {
                    totalHT.Add(item.Key, item.Value);
                }
            }
            return totalHT;
        }

        /// <summary>
        /// 计算是否需要单独多设置一个线程处理尾数.
        /// </summary>
        /// <param name="nSetThread"></param>
        /// <param name="nPiPeiSize"></param>
        /// <returns></returns>
        private int GetThreadNum(int nSetThread, MatrixCiCui nPiPeiSize)
        {
            int nRtn = 0;

            int nTotal = nPiPeiSize.horSize * nPiPeiSize.verSize;

            if (0 != (nTotal % nSetThread))
            {
                nRtn = nSetThread + 1;
            }
            else
            {
                nRtn = nSetThread;
            }

            return nRtn;
        }

        /// <summary>
        /// 根据小图和大图的尺寸 计算匹配的次数
        /// 用于为各个线程分配
        /// </summary>
        /// <param name="PicMatch"></param>
        /// <param name="PicBase"></param>
        /// <returns></returns>
        private MatrixCiCui GetPiPeiTime(double[,] PicMatch, double[,] PicBase)
        {
            MatrixCiCui nRtn = new MatrixCiCui();

            int BaseHor = GetHorLength(PicBase);
            int MatchHor = GetHorLength(PicMatch);

            nRtn.horSize = BaseHor - MatchHor + 1;


            int BaseVer = GetVerLength(PicBase);
            int MatchVer = GetVerLength(PicMatch);
            nRtn.verSize = BaseVer - MatchVer + 1;



            return nRtn;
        }

        /// <summary>
        /// 获得图片的Row和Colomn值
        /// </summary>
        /// <param name="strBase">文件名字</param>
        /// <returns>返回mySize对象</returns>
        private mySize GetPicSize(string strFile)
        {
            mySize sizeRtn = new mySize(0, 0);

            // 用一个Image.
            Image image = Image.FromFile(strFile);

            // 这里会开辟新内存
            Bitmap bitmap = new Bitmap(image);

            // 如果是Tif 没有RGB分量  不用经过灰度图转换
            sizeRtn.COL = image.Width;
            sizeRtn.ROW = image.Height;

            // 释放资源
            bitmap.Dispose();
            image.Dispose();

            return sizeRtn;
        }

        /// <summary>
        /// 把图片里的值 设置到空白的内存里.
        /// </summary>
        /// <param name="strBase">图片来源</param>
        /// <param name="PicBase">写入的内存</param>
        private void SetPicData(string strFile, double[,] PicBase)
        {
            // 用一个Image.
            Image image = Image.FromFile(strFile);

            // 这里会开辟新内存
            Bitmap bitmap = new Bitmap(image);

            // 如果是Tif 没有RGB分量  不用经过灰度图转换
            int hor = image.Width;
            int ver = image.Height;

            // 放在第一个的也是高度，也就是行数
            // 放在第二个的是宽度，也就是列数
            int[,] I = new int[ver, hor];

            Color c = new Color();
            int[,] r = new int[ver, hor];//存储整幅图像的红色分量的像素信息
            int[,] g = new int[ver, hor];//存储整幅图像的绿色分量的像素信息
            int[,] b = new int[ver, hor];//存储整幅图像的蓝色分量的像素信息

            for (int i = 0; i < ver; i++)  //整幅图像行(高)
            {
                for (int j = 0; j < hor; j++)//整幅图像列（宽）
                {
                    // GetPixel 函数在读取元素时，行列是反着的
                    c = bitmap.GetPixel(j, i);//获取图片每个点灰度


                    r[i, j] = c.R;
                    g[i, j] = c.G;
                    b[i, j] = c.B;

                    //分离出三个分量的值赋给相应的像素矩阵
                    //获取整个图片的灰度图
                    I[i, j] = Convert.ToInt32(0.2989 * r[i, j] + 0.587 * g[i, j] + 0.114 * b[i, j]);

                }
            }
            // I.GetLength

            ChangetoDouble(I, PicBase);

            // 释放文件.

            I = null;
            r = null;
            g = null;
            b = null;

            bitmap.Dispose();
            image.Dispose();
        }

        /// <summary>
        /// 把Uint8转换为double类型
        /// </summary>
        /// <param name="data"></param>
        /// <param name="setData"></param>
        private void ChangetoDouble(int[,] data, double[,] setData)
        {


            int iRow = data.GetLength(0);       // 行数
            int iColumn = data.GetLength(1);    // 列数

            for (int i = 0; i < iRow; i++)
            {
                for (int j = 0; j < iColumn; j++)
                {
                    double dTemp = 0.0;
                    double Uint8 = 255.0;

                    dTemp = data[i, j] / Uint8;
                    setData[i, j] = Math.Round(dTemp, 5);
                }
            }
        }

        /// <summary>
        /// 获得水平长度
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private int GetHorLength(double[,] matrix)
        {
            return matrix.GetLength(1);
        }

        /// <summary>
        /// 获得垂直长度
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private int GetVerLength(double[,] matrix)
        {
            return matrix.GetLength(0);
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
    }
}
