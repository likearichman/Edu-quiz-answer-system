using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselineSystem
{
    class MySimilarity : DefaultSimilarity
    {
        public override float Tf(float freq)
        {
            //return (float)Math.Sqrt(freq);
            return 1;
        }
        //public override float Coord(int overlap, int maxOverlap)
        //{
        //    return base.Coord(overlap, maxOverlap);
        //    return 1;
        //}
    }
}
