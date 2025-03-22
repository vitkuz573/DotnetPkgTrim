using Microsoft.Build.Locator;
using System.CommandLine;
using System.Xml.Linq;

namespace PkgTrim.Tool;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        MSBuildLocator.RegisterDefaults();

        var rootCommand = new RootCommand("PkgTrim");

        var solutionDirectoryOption = new Option<string>("--solution-directory", "--sln-dir")
        {
            Description = "Path to the solution directory. If not provided, the directory of the executable is used.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory()
        };

        var fixOption = new Option<bool>("--fix")
        {
            Description = "Automatically remove unused packages from Directory.Packages.props",
            DefaultValueFactory = _ => false
        };

        rootCommand.Options.Add(solutionDirectoryOption);
        rootCommand.Options.Add(fixOption);

        rootCommand.SetAction((parseResult) =>
        {
            var solutionDirectory = parseResult.GetValue(solutionDirectoryOption);
            var fix = parseResult.GetValue(fixOption);

            if (solutionDirectory is null)
            {
                Console.WriteLine("Unable to determine solution directory.");

                return;
            }

            Console.WriteLine($"Solution directory: {solutionDirectory}");

            var usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var csprojFiles = Directory.EnumerateFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories);
            
            foreach (var csproj in csprojFiles)
            {
                try
                {
                    var doc = XDocument.Load(csproj);
                    var packageRefs = doc.Descendants("PackageReference")
                                         .Select(pr => pr.Attribute("Include")?.Value)
                                         .Where(val => !string.IsNullOrEmpty(val))
                                         .ToList();

                    Console.WriteLine($"File: {csproj}");

                    if (packageRefs.Count != 0)
                    {
                        foreach (var pkg in packageRefs)
                        {
                            if (pkg != null)
                            {
                                Console.WriteLine($"  Found package: {pkg}");

                                usedPackages.Add(pkg);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No package references found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {csproj}: {ex.Message}");
                }
            }

            var buildPropsFiles = Directory.EnumerateFiles(solutionDirectory, "Directory.Build.props", SearchOption.AllDirectories);
           
            foreach (var propsFile in buildPropsFiles)
            {
                try
                {
                    var doc = XDocument.Load(propsFile);
                    var packageRefs = doc.Descendants("PackageReference")
                                         .Select(pr => pr.Attribute("Include")?.Value)
                                         .Where(val => !string.IsNullOrEmpty(val))
                                         .ToList();

                    Console.WriteLine($"File: {propsFile}");
                    
                    if (packageRefs.Count != 0)
                    {
                        foreach (var pkg in packageRefs)
                        {
                            if (pkg != null)
                            {
                                Console.WriteLine($"  Found package: {pkg}");

                                usedPackages.Add(pkg);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No package references found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {propsFile}: {ex.Message}");
                }
            }

            var packagesPropsPath = Path.Combine(solutionDirectory, "Directory.Packages.props");

            if (!File.Exists(packagesPropsPath))
            {
                Console.WriteLine("Directory.Packages.props not found in solution directory.");
               
                return;
            }

            var packagesProps = XDocument.Load(packagesPropsPath);
            var packageVersionElements = packagesProps.Descendants("PackageVersion").ToList();

            var unusedPackages = packageVersionElements
                .Where(el =>
                {
                    var include = el.Attribute("Include")?.Value;

                    return !string.IsNullOrEmpty(include) && !usedPackages.Contains(include);
                })
                .ToList();

            if (unusedPackages.Count == 0)
            {
                Console.WriteLine("No unused packages found. Directory.Packages.props remains unchanged.");
               
                return;
            }

            Console.WriteLine("The following unused packages were identified:");
            
            foreach (var unused in unusedPackages)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  {unused.Attribute("Include")?.Value}");
                Console.ResetColor();
            }

            if (fix)
            {
                foreach (var unused in unusedPackages)
                {
                    unused.Remove();
                }

                packagesProps.Save(packagesPropsPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Unused packages removed. Directory.Packages.props updated.");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("Run with --fix to remove them automatically.");
            }
        });

        var parseResult = rootCommand.Parse(args);
        var commandResult = parseResult.CommandResult;

        var exitCode = await parseResult.InvokeAsync();

        return exitCode;
    }
}
