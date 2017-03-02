using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiThreadMatch
{
    struct Location
    {
        public int row;
        public int column;

        /// <summary>
        /// 第一个数值是垂直数值，第二个数值是水平数值
        /// </summary>
        /// <param name="row">向下的垂直数值</param>
        /// <param name="column">向右的水平数值</param>
        public Location(int row, int column)
        {
            this.row = row;
            this.column = column;
        }
    }
}
