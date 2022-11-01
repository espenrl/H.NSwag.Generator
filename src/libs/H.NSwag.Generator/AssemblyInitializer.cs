using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace H.Generators;

#nullable disable

/// <summary>
/// From https://github.com/dotnet/arcade/blob/main/src/Common/Internal/AssemblyResolver.cs
/// </summary>
internal static class AssemblyInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        // apply any existing policy
        var referenceName = new AssemblyName(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
        var fileName = referenceName.Name + ".dll";

        string probingPath;
        Assembly assembly;

        // look next to requesting assembly
        var assemblyPath = args.RequestingAssembly.Location;
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);
            Debug.WriteLine($"Considering {probingPath} based on RequestingAssembly");
            if (Probe(probingPath, referenceName.Version, out assembly))
            {
                return assembly;
            }
        }

        // look in AppDomain base directory
        probingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        Debug.WriteLine($"Considering {probingPath} based on BaseDirectory");
        if (Probe(probingPath, referenceName.Version, out assembly))
        {
            return assembly;
        }

        // look next to the executing assembly
        assemblyPath = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);

            Debug.WriteLine($"Considering {probingPath} based on ExecutingAssembly");
            if (Probe(probingPath, referenceName.Version, out assembly))
            {
                return assembly;
            }
        }

        // look in current directory
        Debug.WriteLine($"Considering {fileName}");
        if (Probe(fileName, referenceName.Version, out assembly))
        {
            return assembly;
        }

        return null;
    }

    /// <summary>
    /// Considers a path to load for satisfying an assembly ref and loads it
    /// if the file exists and version is sufficient.
    /// </summary>
    /// <param name="filePath">Path to consider for load</param>
    /// <param name="minimumVersion">Minimum version to consider</param>
    /// <param name="assembly">loaded assembly</param>
    /// <returns>true if assembly was loaded</returns>
    private static bool Probe(string filePath, Version minimumVersion, out Assembly assembly)
    {
        if (File.Exists(filePath))
        {
            var name = AssemblyName.GetAssemblyName(filePath);

            if (name.Version >= minimumVersion)
            {
                assembly = Assembly.Load(name);
                return true;
            }
        }

        assembly = null;
        return false;
    }
}