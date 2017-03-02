using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



using System.ComponentModel;
using System.Data;
using System.Drawing;

namespace MultiThreadMatch
{
    class BasePicHandle
    {
        // 读取图片，输出double矩阵
        public static double[,] GetPicData(Image image)
        {
            double[,] dRtn = null;

            // 读取图像信息， 用1.jpg来做测试
            Bitmap bitmap = new Bitmap(image);
            
            // 如果是Tif 没有RGB分量  不用经过灰度图转换
            int width = image.Width;
            int height = image.Height;

            // 放在第一个的也是高度，也就是行数
            // 放在第二个的是宽度，也就是列数
            int[,] I = new int[height, width];

            Color c = new Color();
            int[,] r = new int[height, width];//存储整幅图像的红色分量的像素信息
            int[,] g = new int[height, width];//存储整幅图像的绿色分量的像素信息
            int[,] b = new int[height, width];//存储整幅图像的蓝色分量的像素信息

            for (int i = 0; i < height; i++)  //整幅图像行(高)
            {
                for (int j = 0; j < width; j++)//整幅图像列（宽）
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

            dRtn = ChangetoDouble(I);

            return dRtn;
        }

        // 根据坐标和小图大小截取Bitmap.
        public static Bitmap CutPic(String strFile, Location location, mySize size)
        {
            Bitmap bitRtn = new Bitmap(size.ROW, size.COL);

            Image image = Image.FromFile(strFile);

            // 抛出 内存不足错误
            Bitmap bitmap = new Bitmap(image);


            Color c = new Color();
            for (int i = 0; i < size.ROW; i++)  //整幅图像行(高)
            {
                for (int j = 0; j < size.COL; j++)//整幅图像列（宽）
                {
                    // GetPixel 函数在读取元素时，行列是反着的
                    c = bitmap.GetPixel(j + location.column, i + location.row);//获取图片每个点灰度


                    // 新的图片里必须做偏移运算
                    bitRtn.SetPixel(j, i, c);
                }
            }

            // new出来地方处理掉.
            bitmap.Dispose();

            return bitRtn;
        }



        // 输入一个大矩阵，返回以8*8基本矩阵为基础的嵌套矩阵
        public static Matrix[,] SplitMatrix(double[,] PicBase, mySize SmallBaseSize)
        {
            Matrix[,] mRtn = null;

            // 判断需要开辟多大的矩阵
            int iBaseSize = SmallBaseSize.ROW;
            int iBigSize = PicBase.GetLength(0);
            int nNeed = iBigSize / iBaseSize;

            mRtn = new Matrix[nNeed, nNeed];

            // 切割矩阵的控制代码，先写死 速度做出来

            Location baseL = new Location(0, 0);
            for (int i = 0; i < nNeed; i++)
            {
                // 在外循环中，首地址一直向下移动8个单元
                // 便利循环下一列
                baseL.row = i * iBaseSize;

                for (int j = 0; j < nNeed; j++)
                {
                    // 在内循环中，首地址一直向右移动8个单元
                    // 在每一行中，一列一列的循环

                    // 偏移指针, 首地址向右根据当前小矩阵j来移动
                    // 比如 第一个地址为（0,0） 第二个为(0,8)
                    baseL.column = j * 8;
                    Matrix eachM = new Matrix(PicBase, baseL, SmallBaseSize);

                    mRtn[i, j] = eachM;
                }
            }

            return mRtn;
        }

        // 把Uint8转换为double类型
        private static double[,] ChangetoDouble(int[,] data)
        {


            int iRow = data.GetLength(0);       // 行数
            int iColumn = data.GetLength(1);    // 列数

            double[,] doubleRtn = new double[iRow, iColumn];

            for (int i = 0; i < iRow; i++)
            {
                for (int j = 0; j < iColumn; j++)
                {
                    double dTemp = 0.0;
                    double Uint8 = 255.0;

                    dTemp = data[i, j] / Uint8;
                    doubleRtn[i, j] = Math.Round(dTemp, 5);
                }
            }

            return doubleRtn;
        }

        // 输入一个嵌套矩阵，输出一个List Matrix集合
        public static List<Matrix> GetMatrixList(Matrix[,] MyBaseM, Location[] Lvector)
        {

            // 通道数量
            int nVector = Lvector.Length;

            // 因为是12个通道 所以暂时固定为4
            const int C_Length = 4;

            // 计算针对同一个通道的矩阵 Height和Width
            int iRow = MyBaseM.GetLength(0) - C_Length;
            int iCol = MyBaseM.GetLength(1) - C_Length;

            // 12个46*46矩阵
            List<Matrix> Mlist = new List<Matrix>();

            // 遍历12个通道, 组成为12个 Matrix数组 46*46
            // 3，3起始位置是个很特殊的值
            int baseRow = 2;
            int baseColumn = 2;

            for (int z = 0; z < nVector; z++)
            {
                // 为每一个通道做一层，根据输入矩阵大小 - 一个常量来计算
                Matrix eachM = new Matrix(iRow, iCol);


                // 遍历12个通道时，对46*46 相对于通道的矩阵做处理
                for (int i = 0; i < iRow; i++)
                {
                    for (int j = 0; j < iCol; j++)
                    {
                        // 取得当前row column
                        // 在大矩阵中获得相应矩阵
                        int curRow = i + baseRow;
                        int curCol = j + baseColumn;
                        Matrix curM = MyBaseM[curRow, curCol];

                        // 参考值矩阵
                        Location curL = Lvector[z];
                        int calRow = curRow + curL.row;
                        int calCol = curCol + curL.column;
                        Matrix calM = MyBaseM[calRow, calCol];


                        // 当前矩阵和参考矩阵做运算 得到一个值，放到i,j这个
                        // 位置，
                        double Rtn = Calucate1(curM, calM);
                        eachM.data[i, j] = Rtn;

                    }
                }

                // 加入到一个List里
                Mlist.Add(eachM);
            }

            return Mlist;
        }

        // 求2个矩阵相差的乘方和
        public static double Calucate1(Matrix curM, Matrix calM)
        {
            double dRtn = 0.0;

            int row = curM.Row;
            int column = curM.Column;
            double sum = 0.0;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    double dTemp = curM.data[i, j] - calM.data[i, j];
                    dTemp = Math.Pow(dTemp, 2);

                    sum = sum + dTemp;

                }
            }

            dRtn = 0 - sum;

            return dRtn;
        }

        // 输入两个矩阵List, 输出一个匹配矩阵
        public static Matrix PiPeiMatrix(List<Matrix> MBiglist, List<Matrix> MSmalist, Location[] Lvector)
        {
            // MBiglist * * 12
            // MSmalist * * 12
            // MFinallist * * 12

            int nVector = Lvector.Length;

            // 小图在大图上做移动匹配
            List<Matrix> MFinallist = new List<Matrix>();

            double dE = 0.0;

            for (int z = 0; z < nVector; z++)
            {
                //mylist[i].name
                Matrix MBig = MBiglist[z];
                Matrix MSmall = MSmalist[z];

                ABSMatrix(MBig);
                ABSMatrix(MSmall);

                // 输入2个矩阵，导出一个矩阵.         
                Matrix Mfinal = PiPei(MSmall, MBig);
                ABSMatrix(Mfinal);


                // 从Mfinal中找个最大值和第二大值
                double dMax = FindMax(Mfinal);
                double dSecondMax = FindSecondMax(Mfinal);

                // 算出主峰与次峰比值
                double dBiZhi = dMax / dSecondMax;

                // 得出一个新矩阵
                MatrixMulti(Mfinal, dBiZhi);

                // 记录新矩阵
                MFinallist.Add(Mfinal);

                // 记录主次峰比值

                dE = dE + dBiZhi;

            }
            // dE 之后会用到

            nVector = MFinallist.Count;

            // GetPiPeiSize
            mySize PiPeiSize = GetPiPeiSize(MBiglist[0], MSmalist[0]);

            Matrix MaxtrixSum = new Matrix(PiPeiSize.ROW, PiPeiSize.COL);
            for (int i = 1; i < nVector; i++)
            {
                Matrix eachM = MFinallist[i];

                // 求和
                MaxtrixAdd(MaxtrixSum, eachM);

            }


            // 矩阵除法
            Matrix MatrixG = MaxtrixDiv(MaxtrixSum, dE);

            return MatrixG;
        }

        // ABS 矩阵元素
        public static void ABSMatrix(Matrix A)
        {
            int row = A.Row;
            int column = A.Column;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    double dTemp = A.data[i, j];
                    A.data[i, j] = Math.Abs(dTemp);
                }
            }

        }

        // 2个矩阵匹配，输出一个匹配结果
        public static Matrix PiPei(Matrix MSmall, Matrix MBig)
        {
            // 算出新的匹配矩阵的大小
            // 小矩阵在大的矩阵上移动
            int iNewRow = MBig.Row - MSmall.Row + 1;
            int iNewCol = MBig.Column - MSmall.Column + 1;
            Matrix Mrtn = new Matrix(iNewRow, iNewCol);

            int iSmallRow = MSmall.Row;
            int iSmallCol = MSmall.Column;
            int offsetValue = 1;


            // 偏移指针
            Location baseL = new Location(0, 0);
            mySize size = new mySize(iSmallRow, iSmallCol); // 小匹配图大小

            for (int i = 0; i < iNewRow; i++)
            {

                // Big bug
                // i没有做偏移.
                baseL.row = i * offsetValue;

                for (int j = 0; j < iNewCol; j++)
                {
                    // 每次在大基准图切割一个和匹配小图一样大小的图
                    // 然后和匹配小图进行运算得到一个值
                    // 输出到矩阵的位置

                    // 切割.
                    // 在内循环中，首地址一直向右移动1个单元
                    // 在每一行中，一列一列的循环
                    // 偏移指针, 首地址向右根据当前小矩阵j来移动
                    // 比如 第一个地址为（0,0） 第二个为(0,1)

                    baseL.column = j * offsetValue;  // 每次偏移一个像素位置
                    Matrix baseM = new Matrix(MBig.data, baseL, size);


                    // 比较.
                    double dRtn = CompareMatrix(baseM, MSmall);


                    // 放置值.
                    Mrtn.data[i, j] = dRtn;

                }



            }


            return Mrtn;
        }

        // Find Max value.
        public static double FindMax(Matrix Mfinal)
        {
            double dMax = 0.0;

            for (int i = 0; i < Mfinal.Row; i++)
            {
                for (int j = 0; j < Mfinal.Column; j++)
                {
                    double dCur = Mfinal.data[i, j];
                    if (dCur > dMax)
                    {
                        dMax = dCur;
                    }
                }
            }

            return dMax;
        }

        // Find second value.
        public static double FindSecondMax(Matrix Mfinal)
        {
            double dSecondMax = 0.0;
            double dMax = FindMax(Mfinal);

            for (int i = 0; i < Mfinal.Row; i++)
            {
                for (int j = 0; j < Mfinal.Column; j++)
                {
                    double dCur = Mfinal.data[i, j];

                    if (dCur == dMax)
                    {
                        continue;
                    }
                    if (dCur > dSecondMax)
                    {
                        dSecondMax = dCur;
                    }
                }
            }

            return dSecondMax;
        }

        // 矩阵乘法
        public static void MatrixMulti(Matrix Mfinal, double dBiZhi)
        {
            for (int i = 0; i < Mfinal.Row; i++)
            {
                for (int j = 0; j < Mfinal.Column; j++)
                {
                    Mfinal.data[i, j] = Mfinal.data[i, j] * dBiZhi;
                }
            }
        }

        // 获得小图在大图中的匹配数量
        public static mySize GetPiPeiSize(Matrix MBig, Matrix MSmall)
        {
            int iNewRow = MBig.Row - MSmall.Row + 1;
            int iNewCol = MBig.Column - MSmall.Column + 1;

            mySize sizeRtn = new mySize(iNewRow, iNewCol);


            return sizeRtn;
        }

        // 矩阵除法
        public static Matrix MaxtrixDiv(Matrix MaxtrixSum, double dE)
        {
            if (0.0 == dE)
            {
                return null;
            }

            Matrix Mrtn = new Matrix(MaxtrixSum.Row, MaxtrixSum.Column);

            for (int i = 0; i < Mrtn.Row; i++)
            {
                for (int j = 0; j < Mrtn.Column; j++)
                {
                    Mrtn.data[i, j] = MaxtrixSum.data[i, j] / dE;
                }
            }

            return Mrtn;
        }

        // 矩阵加法
        public static void MaxtrixAdd(Matrix MaxtrixSum, Matrix eachM)
        {
            for (int i = 0; i < MaxtrixSum.Row; i++)
            {
                for (int j = 0; j < MaxtrixSum.Column; j++)
                {
                    MaxtrixSum.data[i, j] = MaxtrixSum.data[i, j] + eachM.data[i, j];
                }
            }
        }

        // 公式2
        public static double CompareMatrix(Matrix baseM, Matrix MSmall)
        {
            double dRtn = 0.0;

            double baseMMean = MatrixMean(baseM);
            double smallMean = MatrixMean(MSmall);

            double dSumTop = 0.0;
            double dSumBelow1 = 0.0;
            double dSumBelow2 = 0.0;


            for (int i = 0; i < baseM.Row; i++)
            {
                for (int j = 0; j < baseM.Column; j++)
                {
                    // Top
                    double dTop = (baseM.data[i, j] - baseMMean) * (MSmall.data[i, j] - smallMean);
                    dSumTop = dSumTop + dTop;

                    // Below1
                    double dBelow1 = Math.Pow((baseM.data[i, j] - baseMMean), 2);
                    dSumBelow1 = dSumBelow1 + dBelow1;

                    // Below2
                    double dBelow2 = Math.Pow((MSmall.data[i, j] - smallMean), 2);
                    dSumBelow2 = dSumBelow2 + dBelow2;
                }
            }


            dRtn = dSumTop / Math.Sqrt(dSumBelow1 * dSumBelow2);

            return dRtn;
        }



        // 求矩阵均值
        public static double MatrixMean(Matrix baseM)
        {
            double dRtn = 0.0;

            double dSum = 0.0;

            for (int i = 0; i < baseM.Row; i++)
            {
                for (int j = 0; j < baseM.Column; j++)
                {
                    dSum = dSum + baseM.data[i, j];
                }
            }

            dRtn = dSum / (baseM.Row * baseM.Column);


            return dRtn;
        }

        // 找最大值位置, 已知最大值
        public static Location FindLocation(Matrix MatrixG, double dMax)
        {
            Location lRtn = new Location(0, 0);

            for (int i = 0; i < MatrixG.Row; i++)
            {
                for (int j = 0; j < MatrixG.Column; j++)
                {
                    double dValue = MatrixG.data[i, j];
                    if (dMax == dValue)
                    {
                        lRtn.row = i;
                        lRtn.column = j;
                        return lRtn;
                    }
                }
            }
            return lRtn;
        }

        // 返回ROW(Height) 统一为ROW
        public static int GetROW(byte[,] matrix)
        {
            return matrix.GetLength(0);
        }

        // 返回COL(Width) 统一为COL
        public static int GetCOL(byte[,] matrix)
        {
            return matrix.GetLength(1);
        }

        // 返回ROW(Height) 统一为ROW
        public static int GetROW(double[,] matrix)
        {
            return matrix.GetLength(0);
        }

        // 返回COL(Width) 统一为COL
        public static int GetCOL(double[,] matrix)
        {
            return matrix.GetLength(1);
        }


        public static double[,] GetPicData(string strFile)
        {
            double[,] dRtn = null;

            // 用一个Image.
            Image image = Image.FromFile(strFile);

            // 这里会开辟新内存
            Bitmap bitmap = new Bitmap(image);

            // 如果是Tif 没有RGB分量  不用经过灰度图转换
            int width = image.Width;
            int height = image.Height;

            // 放在第一个的也是高度，也就是行数
            // 放在第二个的是宽度，也就是列数
            int[,] I = new int[height, width];

            Color c = new Color();
            int[,] r = new int[height, width];//存储整幅图像的红色分量的像素信息
            int[,] g = new int[height, width];//存储整幅图像的绿色分量的像素信息
            int[,] b = new int[height, width];//存储整幅图像的蓝色分量的像素信息

            for (int i = 0; i < height; i++)  //整幅图像行(高)
            {
                for (int j = 0; j < width; j++)//整幅图像列（宽）
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

            dRtn = ChangetoDouble(I);

            // 释放文件.
            image.Dispose();

            return dRtn;
        }

        public static double PiPeiXieFangCha(List<Matrix> MBiglist, List<Matrix> MSmalist, Location[] Lvector)
        {
            // MBiglist * * 12
            // MSmalist * * 12
            // MFinallist * * 12

            int nVector = Lvector.Length;

            // 小图在大图上做移动匹配
            List<Matrix> MFinallist = new List<Matrix>();
            for (int z = 0; z < nVector; z++)
            {
                //mylist[i].name
                Matrix MBig = MBiglist[z];
                Matrix MSmall = MSmalist[z];

                ABSMatrix(MBig);
                ABSMatrix(MSmall);

                // 输入2个矩阵，导出一个矩阵.
                // 匹配的协方差矩阵,理论上值在0-1之间
                Matrix Mfinal = PiPei(MSmall, MBig);
                ABSMatrix(Mfinal);

                // 记录新矩阵
                MFinallist.Add(Mfinal);
            }



            int nCount = MFinallist.Count;
            Matrix any = MFinallist[0];
            Matrix MaxtrixSum = new Matrix(any.Row, any.Column);

            for (int i = 1; i < nCount; i++)
            {
                Matrix eachM = MFinallist[i];

                // 求和
                MaxtrixAdd(MaxtrixSum, eachM);

            }


            // 矩阵除法求均值
            double dNum = MFinallist.Count;
            Matrix MatrixG = MaxtrixDiv(MaxtrixSum, dNum);

            // 找个最大值并返回
            double dMax = FindMax(MatrixG);

            return dMax;
        }

        // 为矩阵赋值一个值
        public static void SetMatrixValue(byte[,] whitePic, byte nColor)
        {
            int nRow = BasePicHandle.GetROW(whitePic);
            int nCol = BasePicHandle.GetCOL(whitePic);

            for (int i = 0; i < nRow; i++)
            {
                for (int j = 0; j < nCol; j++)
                {
                    whitePic[i, j] = nColor;
                }
            }
        }

        public static int GetRowFromFile(string strEachFile)
        {
            int nRtn = 0;

            String[] Firststr = strEachFile.Split('\\');
            // 最后一个是名字
            String fileName = Firststr[Firststr.Length - 1];

            String[] Secondstr = fileName.Split('.');

            // 去掉后缀名
            String fileNameWithout = Secondstr[0];

            String[] Thirdstr = fileNameWithout.Split('_');


            // 第二个元素是i的值
            String strI = Thirdstr[1];

            nRtn = Convert.ToInt32(strI);

            return nRtn;
        }

        public static int GetColFromFile(string strEachFile)
        {
            int nRtn = 0;

            String[] Firststr = strEachFile.Split('\\');
            // 最后一个是名字
            String fileName = Firststr[Firststr.Length - 1];

            String[] Secondstr = fileName.Split('.');

            // 去掉后缀名
            String fileNameWithout = Secondstr[0];

            String[] Thirdstr = fileNameWithout.Split('_');


            // 第三个元素是j的值
            String strI = Thirdstr[2];

            nRtn = Convert.ToInt32(strI);

            return nRtn;
        }

        // 根据图片的i和j的值， 大图大小和小图大小， 获得左上角坐标值
        public static Location GetTopLeftLocation(  int nRowSmall, 
                                                    int nColSmall, 
                                                    mySize BigSize, 
                                                    mySize SmallSize,
                                                    byte[,] bCsharp)
        {
            Location lRtn = new Location(0, 0);

            //Math.Ceiling(3.1) = 4;
            //Math.Floor(3.9) = 3;

            // 行数为0 列数为1 Height为行数 Width为列数
            int iBigRow = BigSize.ROW;
            int iBigCol = BigSize.COL;

            double iSmallRow = Convert.ToDouble(SmallSize.ROW);
            double iSmallCol = Convert.ToDouble(SmallSize.COL);

            int iSplitHeight = Convert.ToInt32(Math.Ceiling(iBigRow / iSmallRow));
            int iSplitWidth = Convert.ToInt32(Math.Ceiling(iBigCol / iSmallCol));


            // 基准点参考值 偏移指针

            Location curL = new Location(0, 0);
            mySize size = new mySize(SmallSize.ROW, SmallSize.COL);

            // 循环遍历， 分割出矩阵后，直接输出小文件
            // 需要分割的矩阵数量已经计算好
            for (int i = 0; i < iSplitHeight; i++)
            {
                curL.row = i * SmallSize.ROW;
                for (int j = 0; j < iSplitWidth; j++)
                {
                    // 类似10*10的矩阵 [0,0] [0,10] [0,]


                    //  [0,0]   [0,10]  [0,20]  
                    //  [10,0]  [10,10] [10,20] 
                    //  [20,0]  [20,10] [20,20] 


                    curL.column = j * SmallSize.COL;

                    // 输入偏移指针，返回一个左上角坐标值
                    // 有异常情况 需要调整位置，都在这个函数里处理
                    Location handlePoint = HandlePoint(curL, bCsharp, size);

                    // 获得byte小矩阵
                    // byte[,] bSmall = GetSmallM(handlePoint, bCsharp, size);

                    if ((nRowSmall == i) && (nColSmall == j))
                    {
                        // 输出左上角坐标.
                        lRtn.row = handlePoint.row;
                        lRtn.column = handlePoint.column;
                        return lRtn;
                    }
                }
            }



            return lRtn;
        }

        public static Location HandlePoint(Location curL, byte[,] bCsharp, mySize size)
        {
            // 只对Point里的 行和列值做越界判断，然后改变这个值
            Location LRtn = new Location();
            LRtn.row = curL.row;
            LRtn.column = curL.column;

            // 行
            // 内框右边的值
            int nSmallRow = curL.row + size.ROW - 1;

            // 外框的坐标值
            int nOutRow = bCsharp.GetLength(0) - 1;

            if (nSmallRow > nOutRow)
            {
                // Row 越界 外框矩阵坐标 - 矩阵长度 + 1
                int nReturnRow = nOutRow - size.ROW + 1;
                LRtn.row = nReturnRow;
            }


            // 列
            // 内框右边的值
            int nSmallCol = curL.column + size.COL - 1;

            // 外框的坐标值
            int nOutCol = bCsharp.GetLength(1) - 1;

            if (nSmallCol > nOutCol)
            {
                // Row 越界 外框矩阵坐标 - 矩阵长度 + 1
                int nReturnCol = nOutCol - size.COL + 1;
                LRtn.column = nReturnCol;
            }

            return LRtn;
        }

        // Copy数据 从一部分拷贝
        public static void CopyMatrixValue(byte[,] whiteIR, byte[,] ccd, Location TopLeft, mySize SmallSize)
        {
            // 循环遍历

            int nRow = SmallSize.ROW;
            int nCol = SmallSize.COL;

            for (int i = 0; i < nRow; i++)
            {
                for (int j = 0; j < nCol; j++)
                {
                    int nCurRow = i + TopLeft.row;
                    int nCurCol = j + TopLeft.column;

                    whiteIR[nCurRow, nCurCol] = ccd[nCurRow, nCurCol];
                }
            }
        }

        /// <summary>
        /// 从文件获取Row和Col
        /// </summary>
        /// <param name="strFile"></param>
        /// <returns></returns>
        public static mySize GetSize(string strFile)
        {
            mySize size = new mySize(0, 0);

            Image imageTemp = Image.FromFile(strFile);
            // 根据事实设定
            size.ROW = imageTemp.Height;
            size.COL = imageTemp.Width;

            imageTemp.Dispose();

            return size;
        }

        /// <summary>
        /// 对JPG图片改变像素来达到画线条的目的
        /// </summary>
        /// <param name="tempImage"></param>
        /// <param name="MatchPic"></param>
        /// <param name="getLocation"></param>
        public static void DrawRetangle(Image tempImage, double[,] MatchPic, Location getLocation)
        {
            // 从这个点画一个小图大小的白框(灰度时 值为255) 在大图Basepic上面画小图
            int nSizeDrawRow = BasePicHandle.GetROW(MatchPic);
            int nSizeDrawCol = BasePicHandle.GetCOL(MatchPic);


            // 偏移指针 基准点的值
            int OFFRow = getLocation.row;
            int OFFCol = getLocation.column;

            // Bitmap bmp = new Bitmap("ir.tif");
            Graphics g = Graphics.FromImage(tempImage);

            int matchRow = BasePicHandle.GetROW(MatchPic);
            int matchCol = BasePicHandle.GetCOL(MatchPic);

            g.DrawRectangle(new Pen(Color.White), OFFCol, OFFRow, matchRow, matchCol);
            
        }

        /// <summary>
        /// 求矩阵平均值.
        /// </summary>
        /// <param name="tempM"></param>
        public static double MatrixMean(double[,] tempM)
        {
            double dRtn = 0.0;

            int nRow = BasePicHandle.GetROW(tempM);
            int nCol = BasePicHandle.GetCOL(tempM);

            for (int i = 0; i < nRow; i++)
            {
                for (int j = 0; j < nCol; j++)
                {
                    dRtn = dRtn + tempM[i, j];
                }
            }

            dRtn = dRtn / (nRow * nCol);

            return dRtn;
        }
    }
}
