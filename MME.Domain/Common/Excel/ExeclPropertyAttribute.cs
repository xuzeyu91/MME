using NPOI.SS.UserModel;

namespace MME.Domain.Common.Excel
{
    public class ExeclPropertyAttribute : Attribute
    {
        public ExeclPropertyAttribute()
        {

        }
        public ExeclPropertyAttribute(string displayName, int order, CellType cellType = CellType.String)
        {
            DisplayName = displayName;
            Order = order;
            CellType = cellType;
        }

        public string DisplayName { get; set; }

        public int Order { get; set; }

        public CellType CellType { get; set; }
    }
}
