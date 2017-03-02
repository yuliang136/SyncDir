using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiThreadMatch
{
    class Matrix
    {
        public double[,] data;

        // 行属性
        public int Row
        {
            get
            {
                return data.GetLength(0);
            }
        }

        // 列属性
        public int Column
        {
            get
            {
                return data.GetLength(1);
            }
        }

        // 默认构造函数,初始值全部为0
        public Matrix(int row, int column)
        {

            data = new double[row, column];

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    data[i, j] = 0.0;
                }
            }
        }

        // 其中一个构造函数，用于在一个大矩阵中切割小矩阵
        // 参数是起始位置
        // 参数是一个原始大矩阵和需要切割的height和width
        public Matrix(  
                        double[,] D,
                        Location location,
                        mySize size
                     )
        {
            data = new double[size.ROW, size.COL];

            // 根据开始位置开始读取值
            int height = size.ROW;
            int width = size.COL;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // 从开始点向右和向下 切割矩阵

                    // 在大矩阵中寻找需要切割的元素坐标
                    int SplitRow = location.row + i;
                    int SplitColumn = location.column + j;

                    data[i, j] = D[SplitRow, SplitColumn];
                }
            }
            //return data;
        }
    }
}
