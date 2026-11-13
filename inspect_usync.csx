#!/usr/bin/env dotnet-script
using System.Reflection;

var dllPath = @"C:\Users\TalhaMalik\.nuget\packages\usync.publisher\17.3.6\lib\net10.0\uSync.Publisher.dll";
var asm = Assembly.LoadFrom(dllPath);

var types = asm.GetExportedTypes()
    .Where(t => t.IsInterface || t.IsClass)
    .OrderBy(t => t.FullName)
    .ToList();

Console.WriteLine("=== All public types in uSync.Publisher 17.3.6 ===");
foreach (var t in types)
{
    Console.WriteLine($"{(t.IsInterface ? "interface" : "class")} {t.FullName}");
}

Console.WriteLine("\n=== Types matching State/Publisher/Process ===");
foreach (var t in types.Where(t => t.Name.Contains("State") || t.Name.Contains("Process") || t.Name.Contains("Publisher")))
{
    Console.WriteLine($"{(t.IsInterface ? "interface" : "class")} {t.FullName}");
    if (t.IsInterface)
    {
        foreach (var m in t.GetMethods())
            Console.WriteLine($"  {m}");
    }
}
