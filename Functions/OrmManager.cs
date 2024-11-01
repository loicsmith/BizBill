using ModKit.ORM;
using SQLite;

namespace MODRP_BizBill.Functions
{
    internal class OrmManager
    {

        public class BizBill_LogsBill : ModEntity<BizBill_LogsBill>
        {
            [AutoIncrement][PrimaryKey] public int Id { get; set; }

            public string CustomerName { get; set; }
            public string EmployeeName { get; set; }
            public float Price { get; set; }
            public int Date { get; set; }
        }

    }
}
