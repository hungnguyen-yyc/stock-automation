using System.Reflection;
using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {
        var signatures = GetAllMethodSignatures(@"C:\Users\hnguyen\source\repos\Stock.Indicators\src\bin\Debug\netstandard2.1\Skender.Stock.Indicators.dll");
        foreach(var signature in signatures)
        {
            Console.WriteLine(signature.ToString());
        }
    }

    public static IEnumerable<MethodInfo> GetAllMethodSignatures(string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);

        var methodSignatures = new List<MethodInfo>();

        // only get types in Skender.Stock.Indicators namespace
        var types = assembly.GetTypes().Where(t => t.FullName != null && t.FullName.Contains("Skender.Stock.Indicators"));

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            methodSignatures.AddRange(methods);
        }

        return methodSignatures;
    }

}