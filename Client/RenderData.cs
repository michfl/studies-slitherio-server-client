using System.Collections.Generic;
using System.Windows.Shapes;

namespace Client
{
    /*
     * Consists of data prepared to be rendered.
     */
    public class RenderData
    {
        public string Statistics { get; set; }
        public List<Polyline> Snakes { get; set; }
        public List<Line> Food { get; set; }

        public RenderData()
        {
            Snakes = new List<Polyline>();
            Food = new List<Line>();
        }
    }
}
