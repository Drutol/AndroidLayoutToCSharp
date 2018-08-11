using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AndroidLayoutToProperties
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFolder _resolutionFolder;

        public MainPage()
        {
            this.InitializeComponent();  
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            AddNewLinesBetweenProperties.IsChecked =
                (bool) ApplicationData.Current.LocalSettings.Values["AddNewLinesBetweenProperties"];

            try
            {
                _resolutionFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("ResolutionFolder");
                ResolutionPath.Text = _resolutionFolder.Path;
            }
            catch (Exception)
            {
                //shrug
            }
        }

        private async void DoTheThing(object sender, RoutedEventArgs ee)
        {
            try
            {
                var layout = XDocument.Parse(InputBox.Text);
                IEnumerable<ElementEntry> nodes = null;
                await Task.Run(() =>
                {
                    nodes = GetNodesWithId(layout.Root).Select(element => new ElementEntry(element));
                });

                var groups = nodes.GroupBy(entry => entry.Name);
                if (groups.Any(entries => entries.Count() > 1))
                {
                    var dialog = new MessageDialog("Multiple controls referenced with the same ID found: " +
                                                   $"{string.Join(",", groups.Where(entries => entries.Count() > 1).Select(entries => entries.Key))}\n\n",
                        "Duplicate IDs!");
                    await dialog.ShowAsync();

                    var newNodes = groups.Select(g => g.Take(1).First()).ToList();
                    nodes = newNodes;
                }

                var outputFields = new StringBuilder();
                var outputProperties = new StringBuilder();

                if (OnlyUppercaseCheckBox.IsChecked == true)
                    nodes = nodes.Where(entry => char.IsUpper(entry.Name[0]));

                foreach (var elementEntry in nodes)
                {
                    var field = FirstToLower(elementEntry.Name);
                    outputFields.AppendLine($"private {elementEntry.Type} _{field};");
                    outputProperties.AppendLine(
                        $"public {elementEntry.Type} {FirstToUpper(elementEntry.Name)} => _{field} ?? (_{field} = FindViewById<{elementEntry.Type}>(Resource.Id.{elementEntry.Name}));{(AddNewLinesBetweenProperties.IsChecked == true ? "\n" : "")}");
                }

                if (!ViewHolderCheckbox.IsChecked == true)
                {
                    var builder = new StringBuilder();
                    if (RegionCheckBox.IsChecked == true)
                        builder.AppendLine("#region Views\n");
                    builder.AppendLine(outputFields.ToString());
                    builder.AppendLine(outputProperties.ToString().Trim());
                    if (RegionCheckBox.IsChecked == true)
                        builder.Append("\n#endregion");

                    OutputBox.Text = builder.ToString();
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
                    builder.AppendLine("\t}");
                    builder.AppendLine($"\t{outputFields.Replace("\n", "\n\t")}");
                    builder.AppendLine("\t" + outputProperties.Replace("FindViewById", "_view.FindViewById").Replace("\n", "\n\t")
                        .ToString().Trim());
                    builder.AppendLine("}");

                    OutputBox.Text = builder.ToString();
               }
            }
            catch (Exception e)
            {
                OutputBox.Text = $"{e}\n\n{e.Message}\n\n{e.StackTrace}";
            }
        }

        private IEnumerable<XElement> GetNodesWithId(XElement rootElement)
        {
            if (rootElement.HasElements)
            {
                foreach (var xElement in rootElement.Elements())
                {
                    foreach (var element in GetNodesWithId(xElement))
                    {
                        yield return element;
                    }
                }
            }

            if (rootElement.HasAttributes)
            {
                if (rootElement.Name == "include")
                {
                    if (_resolutionFolder != null)
                    {
                        string xml = null;
                        try
                        {

                            var file = _resolutionFolder.GetFileAsync(
                                $"{rootElement.Attribute("layout").Value.Replace("@layout/", "")}.xml").AsTask().Result;
                            using (var fs = file.OpenReadAsync().AsTask().Result)
                            {
                                using (var reader = new StreamReader(fs.AsStreamForRead()))
                                {
                                    xml = reader.ReadToEnd();
                                }
                            }
                        }
                        catch
                        {
                            //filesystem may troll us here
                        }
                        if (xml != null)
                        {
                            foreach (var element in GetNodesWithId(XDocument.Parse(xml).Root))
                            {
                                yield return element;
                            }
                        }
                    }
                }
                else
                {
                    var attr = rootElement.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id");
                    if (attr != null)
                    {
                        if (attr.Value.StartsWith("@+id/"))
                            yield return rootElement;
                    }
                }
 
            }

        }

        private string FirstToLower(string input)
        {
            return input.Substring(0, 1).ToLower() + input.Substring(1);
        }

        private string FirstToUpper(string input)
        {
            return input.Substring(0, 1).ToUpper() + input.Substring(1);
        }

        private async void SelectFolderOnClick(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(
                    "ResolutionFolder", folder);
                _resolutionFolder = folder;
                ResolutionPath.Text = folder.Path;
            }
        }

        private void AddNewLinesBetweenPropertiesOnChecked(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["AddNewLinesBetweenProperties"] = AddNewLinesBetweenProperties.IsChecked.Value;
        }
    }

    public class ElementEntry
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public ElementEntry(XElement source)
        {
            var attr = source.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "managedTypeName");
            Type = attr != null ? 
                attr.Value : 
                source.Name.LocalName.Split('.').Last();
       
            Name = source.Attributes().First(attribute => attribute.Name.LocalName == "id").Value.Substring(5);
        }
    }
}
