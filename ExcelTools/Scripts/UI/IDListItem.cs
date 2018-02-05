
using System.Collections.Generic;

namespace ExcelTools.Scripts.UI
{
    class IDListItem
    {
        public string ID { get; set; }

        public int Row { get; set; }

        public List<string> States {
            get { return States; }
            set {
                TrunkState = value[0];
                StudioState = value[1];
                TFState = value[2];
                ReleaseState = value[3];
                }
        }

        public string TrunkState { get; set; }

        public string StudioState { get; set; }

        public string TFState { get; set; }

        public string ReleaseState { get; set; }
    }
}
