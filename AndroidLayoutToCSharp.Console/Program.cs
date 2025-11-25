using System.Text;
using System.Xml.Linq;

namespace AndroidLayoutToCSharp.Console;

internal class Program
{
    private static string? _resolutionFolderPath;
    
    private static async Task Main(string[] args)
    {
        args =
        [
            //"/mnt/shared/DotConnect/cityscannercore/Apps/ControlApp/CityScanner.ControlApp.Net/Resources/layout/page_scanning.xml"
            "/mnt/shared/DotConnect/cityscannercore/Apps/ControlApp/CityScanner.ControlApp.Net/Resources/layout/page_lapComplete.xml"
        ];
        string output = null;
        if (args.Length == 2)
        {
            output = await DoTheThing(File.ReadAllText(args[0]), args[1]);
        }   
        else if (args.Length == 1)
        {
            output = await DoTheThing(File.ReadAllText(args[0]));
        }
        else
        {
            output = "No path provided.";
        }
        
        System.Console.WriteLine(output);
    }

    private static async Task<string> DoTheThing(
        string xmlLayout, 
        string? resolutionFolderPath = null,
        bool onlyUppercase = true,
        bool isViewHolder = false, 
        bool addRegions = true,
        bool addNewLinesBetweenProperties = false)
    {
        _resolutionFolderPath = resolutionFolderPath;
        
        try
        {
            var layout = XDocument.Parse(xmlLayout);
            IEnumerable<ElementEntry> nodes = null;
            await Task.Run(() => { nodes = GetNodesWithId(layout.Root).Select(element => new ElementEntry(element)); });

            var groups = nodes.GroupBy(entry => entry.Name);
            if (groups.Any(entries => entries.Count() > 1))
            {
                throw new Exception("Duplicate IDs!\n" + "Multiple controls referenced with the same ID found: " +
                                    $"{string.Join(",", groups.Where(entries => entries.Count() > 1).Select(entries => entries.Key))}\n\n");
                
                var newNodes = groups.Select(g => g.Take(1).First()).ToList();
                nodes = newNodes;
            }

            var outputFields = new StringBuilder();
            var outputProperties = new StringBuilder();

            if (onlyUppercase == true)
                nodes = nodes.Where(entry => char.IsUpper(entry.Name[0]));

            foreach (var elementEntry in nodes)
            {
                var field = FirstToLower(elementEntry.Name);
                outputFields.AppendLine($"private {elementEntry.Type} _{field};");
                outputProperties.AppendLine(
                    $"public {elementEntry.Type} {FirstToUpper(elementEntry.Name)} => _{field} ?? (_{field} = FindViewById<{elementEntry.Type}>(Resource.Id.{elementEntry.Name}));{(addNewLinesBetweenProperties == true ? "\n" : "")}");
            }

            if (!isViewHolder == true)
            {
                var builder = new StringBuilder();
                if (addRegions == true)
                    builder.AppendLine("#region Views\n");
                builder.AppendLine(outputFields.ToString());
                builder.AppendLine(outputProperties.ToString().Trim());
                if (addRegions == true)
                    builder.Append("\n#endregion");

                return builder.ToString();
            }
            else
            {
                var builder = new StringBuilder();
                builder.AppendLine("class NAME : RecyclerView.ViewHolder");
                builder.AppendLine("{");
                builder.AppendLine("\tprivate readonly View _view;");
                builder.AppendLine("");
                builder.AppendLine("\tpublic NAME(View view) : base(view)");
                builder.AppendLine("\t{");
                builder.AppendLine("\t\t_view = view;");
                builder.AppendLine("\t}\n");
                builder.AppendLine($"\t{outputFields.Replace("\n", "\n\t")}");
                builder.AppendLine("\t" + outputProperties.Replace("FindViewById", "_view.FindViewById")
                    .Replace("\n", "\n\t")
                    .ToString().Trim());
                builder.AppendLine("}");

                return builder.ToString();
            }
        }
        catch (Exception e)
        {
            throw; // $"{e}\n\n{e.Message}\n\n{e.StackTrace}";
        }
    }

    private static IEnumerable<XElement> GetNodesWithId(XElement rootElement)
    {
        if (rootElement.HasElements)
            foreach (var xElement in rootElement.Elements())
            foreach (var element in GetNodesWithId(xElement))
                yield return element;

        if (rootElement.HasAttributes)
        {
            var attr = rootElement.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id");

            if (rootElement.Name == "include")
            {
                var skipRecursiveResolutionAttr = rootElement.Attributes()
                    .FirstOrDefault(attribute => attribute.Name.LocalName == "skipRecursion");
                if (skipRecursiveResolutionAttr == null || skipRecursiveResolutionAttr.Value == "false")
                    if (_resolutionFolderPath != null)
                    {
                        var addPrefixAttr = rootElement.Attributes()
                            .FirstOrDefault(attribute => attribute.Name.LocalName == "innerPrefix");

                        string xml = null;
                        try
                        {
                            xml = GetIncludedLayout(rootElement);
                        }
                        catch
                        {
                            //filesystem may troll us here
                        }

                        if (xml != null)
                            foreach (var element in GetNodesWithId(XDocument.Parse(xml).Root))
                            {
                                if (addPrefixAttr != null)
                                {
                                    var idAttr = element.Attributes()
                                        .FirstOrDefault(attribute => attribute.Name.LocalName == "id");

                                    idAttr.Value = "@+id/" + (addPrefixAttr.Value == "default"
                                        ? $"{idAttr.Value.Substring(5)}_"
                                        : addPrefixAttr.Value) + idAttr.Value.Substring(5);
                                }

                                yield return element;
                            }
                    }
            }


            if (attr != null)
            {
                if (rootElement.Name == "include")
                {
                    if (rootElement.Attributes().Any(attribute => attribute.Name.LocalName == "managedTypeName"))
                    {
                        if (attr.Value.StartsWith("@+id/"))
                            yield return rootElement;
                    }
                    else if (_resolutionFolderPath != null)
                    {
                        //let's try to get the root element class name from the file
                        string xml = null;
                        try
                        {
                            xml = GetIncludedLayout(rootElement);
                        }
                        catch
                        {
                            //no dice
                        }

                        if (xml != null)
                        {
                            var doc = XDocument.Parse(xml);
                            if (doc.Root != null)
                            {
                                rootElement.SetAttributeValue("managedTypeName", doc.Root.Name.LocalName);
                                yield return rootElement;
                            }
                        }
                    }
                }
                else
                {
                    if (attr.Value.StartsWith("@+id/"))
                        yield return rootElement;
                }
            }
        }
    }

    private static string GetIncludedLayout(XElement includeElement)
    {
        if (includeElement.Attributes().All(attribute => attribute.Name != "layout"))
            return null;

        return File.ReadAllText($"{includeElement.Attribute("layout").Value.Replace("@layout/", "")}.xml");
    }

    private static string FirstToLower(string input)
    {
        return input.Substring(0, 1).ToLower() + input.Substring(1);
    }

    private static string FirstToUpper(string input)
    {
        return input.Substring(0, 1).ToUpper() + input.Substring(1);
    }


    public  class RecentFolderResolutionEntry
    {
        public Guid Guid { get; set; }
        public string Path { get; set; }
    }

    public class ElementEntry
    {
        public ElementEntry(XElement source)
        {
            var attr = source.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "managedTypeName");
            Type = attr != null ? attr.Value : source.Name.LocalName.Split('.').Last();

            Name = source.Attributes().First(attribute => attribute.Name.LocalName == "id").Value.Substring(5);
        }

        public string Type { get; set; }
        public string Name { get; set; }
    }
}