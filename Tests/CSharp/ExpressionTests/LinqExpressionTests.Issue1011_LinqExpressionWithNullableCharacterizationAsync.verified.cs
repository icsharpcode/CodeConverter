using System.Collections.Generic;
using System.Linq;

public partial class ConversionTest2
{
    private partial class MyEntity
    {
        public int? FavoriteNumber { get; set; }
        public string Name { get; set; }
    }
    private void BugRepro()
    {

        var entities = new List<MyEntity>();

        string result = (from e in entities
                         where e.FavoriteNumber == 123
                         select e.Name).Single();

    }
}