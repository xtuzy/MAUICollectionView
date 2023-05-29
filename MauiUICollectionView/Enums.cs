using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiUICollectionView
{
    /// <summary>
    /// cell大小的几种取值, 可以是固定值, 可以是测量值
    /// </summary>
    public enum SizeStrategy
    {
        /// <summary>
        /// Use a fixed size to measure cell, that be give by <see cref="ITableViewSource.heightForRowAtIndexPath"/>
        /// </summary>
        FixedSize,
        /// <summary>
        /// Use a infinity value to measure cell, the result can be any value. This is the default value
        /// </summary>
        MeasureSelf,
        /// <summary>
        /// Use a infinity value to measure cell, if the measure result  is less than min fixed size, the final result will use min fixed size.
        /// </summary>
        MeasureSelfGreaterThanMinFixedSize,
        /// <summary>
        /// Use max fixed size to measure cell, the measure result will less than max fixed size.
        /// </summary>
        MeasureSelfLessThanMaxFixedSize,
    }
}
