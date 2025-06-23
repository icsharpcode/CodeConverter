using System.Collections.Generic;
using System.Linq;

public partial class ConversionTest6
{
    private partial class MyEntity
    {
        public string Name { get; set; }
        public string FavoriteString { get; set; }
    }
    public void BugRepro()
    {

        var entities = new List<MyEntity>(); // If this was a DbSet from EFCore, then the 'is' below needs to be converted to == to avoid an error. Instead of detecting dbset, we'll just do this for all queries

        var data = (from e in entities
                    where e.Name == null || e.FavoriteString != null
                    select e).ToList();
    }
}