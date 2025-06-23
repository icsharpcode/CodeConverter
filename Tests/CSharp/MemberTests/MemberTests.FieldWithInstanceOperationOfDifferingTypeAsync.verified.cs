using System.Threading.Tasks;

public partial class DoesNotNeedConstructor
{
    private readonly ParallelOptions ClassVariable1 = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
}