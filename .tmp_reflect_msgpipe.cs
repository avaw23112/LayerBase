using System;
using System.Linq;
using System.Reflection;
var asm = Assembly.LoadFrom(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages\messagepipe\1.8.1\lib\netstandard2.0\MessagePipe.dll"));
var types = asm.GetTypes().Where(t => t.Name.Contains("ServiceCollection") || t.Name.Contains("MessagePipeBuilder") || t.Name.Contains("Extensions"));
foreach (var t in types.OrderBy(x => x.FullName))
{
    Console.WriteLine($"TYPE: {t.FullName}");
    foreach (var m in t.GetMethods(BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).OrderBy(m => m.Name))
    {
        var ps = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({ps})");
    }
}
