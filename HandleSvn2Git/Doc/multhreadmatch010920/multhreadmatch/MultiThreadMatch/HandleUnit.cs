using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace MultiThreadMatch
{
    // 一个线程处理一个单元.
    class HandleUnit
    {
        // 一个小图的区域.
        public int nStart;
        public int nEnd;

        // 大图矩阵.
        public double[,] mBasePic;
        // 匹配矩阵.
        public double[,] mMatchPic;

        public MatrixCiCui mPiPeiSize;
        



        
        // 记录结果
        // key是位置.value是比较值.
        // 最后来综合.
        public Hashtable mHT = new Hashtable();

        // 匹配函数应该写在这个位置.
        public void Matching()
        {
            int nHorLength = GetHorLength(mMatchPic);
            int nVerLength = GetVerLength(mMatchPic);

            double[,] tempM = new double[nHorLength, nVerLength];

            // 循环处理.
            for (int i = nStart; i <= nEnd; i++)
            {
                // 根据一维位置，输出二维矩阵位置.
                // 用坐标位置来换算 一维->二维

                int nVer = i / mPiPeiSize.verSize;
                int nHor = i % mPiPeiSize.horSize;

                
                // Ver 垂直
                // Hor 水平.

                SetMatrix(tempM, mBasePic, nVer, nHor);

                // 这个l究竟是哪个l
                Location l = new Location(nVer, nHor);

                double dEachResult = MatchGuiYiHuaCompare(tempM, mMatchPic);

                mHT.Add(l, dEachResult);
            }

        }

        /// <summary>
        /// 归一化方法 两个矩阵对比得值
        /// </summary>
        /// <param name="tempM">S</param>
        /// <param name="PicMatch">r</param>
        /// <returns></returns>
        private double MatchGuiYiHuaCompare(double[,] matrixS, double[,] matrixR)
        {
            //
            double dRtn = 0.0;

            // 矩阵求平均值.
            double dMeanS = BasePicHandle.MatrixMean(matrixS);
            double dMeanR = BasePicHandle.MatrixMean(matrixR);

            int nRow = BasePicHandle.GetROW(matrixR);
            int nCol = BasePicHandle.GetCOL(matrixR);


            // 

            double dsOff = 0.0;
            double drOff = 0.0;
            double dFenzi = 0.0;
            double dFenziTemp = 0.0;

            double dsPingFang = 0.0;
            double drPingFang = 0.0;
            double dFront = 0.0;
            double dBack = 0.0;

            for (int i = 0; i < nRow; i++)
            {
                for (int j = 0; j < nCol; j++)
                {
                    dsOff = matrixS[i, j] - dMeanS;
                    drOff = matrixR[i, j] - dMeanR;
                    dFenziTemp = dsOff * drOff;
                    dFenzi = dFenzi + dFenziTemp;


                    dsPingFang = dsOff * dsOff;
                    dFront = dFront + dsPingFang;

                    drPingFang = drOff * drOff;
                    dBack = dBack + drPingFang;
                }
            }



            // 分母
            double dFenmu = Math.Sqrt(dFront * dBack);


            dRtn = dFenzi / dFenmu;


            return dRtn;
        }


        /// <summary>
        /// 根据位置从source中拷贝数据到destination
        /// </summary>
        /// <param name="tempM">destination</param>
        /// <param name="PicBase">source</param>
        /// <param name="offVer">垂直位置</param>
        /// <param name="offHor">水平位置</param>
        private void SetMatrix(double[,] tempM, double[,] PicBase, int offVer, int offHor)
        {
            int nRow = GetVerLength(tempM);
            int nCol = GetHorLength(tempM);

            // 先从上到下，再从左到右的顺序
            for (int i = 0; i < nRow; i++)
            {
                for (int j = 0; j < nCol; j++)
                {
                    tempM[i, j] = PicBase[i + offVer, j + offHor];
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

    }
}
