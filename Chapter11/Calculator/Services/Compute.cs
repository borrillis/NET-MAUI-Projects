
namespace Calculator.Services;

internal class Compute
{
    public string Evaluate(string expression)
    {
        System.Data.DataTable dataTable = new System.Data.DataTable();
        var finalResult = dataTable.Compute(expression, string.Empty);
        return finalResult.ToString() ?? string.Empty;
    }
}
