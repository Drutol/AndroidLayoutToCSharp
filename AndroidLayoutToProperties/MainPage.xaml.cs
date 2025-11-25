

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AndroidLayoutToProperties
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private List<RecentFolderResolutionEntry> _recentResolutionFolders;

        private StorageFolder _resolutionFolder;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            //settings.Remove("RecentResolutionFolders");
            if (settings.ContainsKey("RecentResolutionFolders"))
                _recentResolutionFolders =
                    JsonConvert.DeserializeObject<List<RecentFolderResolutionEntry>>(settings["RecentResolutionFolders"]
                        .ToString());
            else
                _recentResolutionFolders = new List<RecentFolderResolutionEntry>();

            RecentResolutionFoldersList.ItemsSource = new List<RecentFolderResolutionEntry>(_recentResolutionFolders);

            if (settings.ContainsKey("AddNewLinesBetweenProperties"))
                AddNewLinesBetweenProperties.IsChecked = (bool)settings["AddNewLinesBetweenProperties"];

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem("ResolutionFolder"))
            {
                _resolutionFolder =
                    await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("ResolutionFolder");
                ResolutionPath.Text = _resolutionFolder.Path;
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
                    builder.AppendLine("\t}\n");
                    builder.AppendLine($"\t{outputFields.Replace("\n", "\n\t")}");
                    builder.AppendLine("\t" + outputProperties.Replace("FindViewById", "_view.FindViewById")
                        .Replace("\n", "\n\t")
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
                        if (_resolutionFolder != null)
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
                        else if (_resolutionFolder != null)
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

        private string GetIncludedLayout(XElement includeElement)
        {
            if (includeElement.Attributes().All(attribute => attribute.Name != "layout"))
                return null;

            string xml;
            var file = _resolutionFolder.GetFileAsync(
                    // ReSharper disable once PossibleNullReferenceException
                    $"{includeElement.Attribute("layout").Value.Replace("@layout/", "")}.xml").AsTask()
                .Result;
            using (var fs = file.OpenReadAsync().AsTask().Result)
            {
                using (var reader = new StreamReader(fs.AsStreamForRead()))
                {
                    xml = reader.ReadToEnd();
                }
            }

            return xml;
        }

        private string FirstToLower(string input)
        {
            return input.Substring(0, 1).ToLower() + input.Substring(1);
        }

        private string FirstToUpper(string input)
        {
            return input.Substring(0, 1).ToUpper() + input.Substring(1);
        }

        private async void SelectFolderOnClick(object sender, RoutedEventArgs ev)
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

                if (_recentResolutionFolders.Any(e => e.Path == folder.Path))
                    return;

                var guid = Guid.NewGuid();
                _recentResolutionFolders.Insert(0, new RecentFolderResolutionEntry
                {
                    Guid = guid,
                    Path = folder.Path
                });
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(guid.ToString(), folder);

                if (_recentResolutionFolders.Count > 10)
                {
                    var last = _recentResolutionFolders.Last();
                    _recentResolutionFolders.Remove(last);
                    StorageApplicationPermissions.FutureAccessList.Remove(last.Guid.ToString());
                }

                ApplicationData.Current.LocalSettings.Values["RecentResolutionFolders"] =
                    JsonConvert.SerializeObject(_recentResolutionFolders);

                RecentResolutionFoldersList.ItemsSource =
                    new List<RecentFolderResolutionEntry>(_recentResolutionFolders);
            }
        }

        private void AddNewLinesBetweenPropertiesOnChecked(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["AddNewLinesBetweenProperties"] =
                AddNewLinesBetweenProperties.IsChecked.Value;
        }

        private async void RecentResolutionFoldersListOnItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedItem = (RecentFolderResolutionEntry)e.ClickedItem;
            var index = _recentResolutionFolders.IndexOf(selectedItem);
            _recentResolutionFolders.RemoveAt(index);
            _recentResolutionFolders.Insert(0, selectedItem);

            var folder =
                await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(selectedItem.Guid.ToString());
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("ResolutionFolder", folder);
            _resolutionFolder = folder;
            ResolutionPath.Text = folder.Path;


            ApplicationData.Current.LocalSettings.Values["RecentResolutionFolders"] =
                JsonConvert.SerializeObject(_recentResolutionFolders);

            RecentResolutionFoldersList.ItemsSource = new List<RecentFolderResolutionEntry>(_recentResolutionFolders);
        }

        private void RemoveRecentResolutionFolderOnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = (RecentFolderResolutionEntry)(sender as Button).CommandParameter;
            var index = _recentResolutionFolders.IndexOf(selectedItem);
            _recentResolutionFolders.RemoveAt(index);

            StorageApplicationPermissions.FutureAccessList.Remove(selectedItem.Guid.ToString());
            ApplicationData.Current.LocalSettings.Values["RecentResolutionFolders"] =
                JsonConvert.SerializeObject(_recentResolutionFolders);

            RecentResolutionFoldersList.ItemsSource = new List<RecentFolderResolutionEntry>(_recentResolutionFolders);
        }
    }

    public class RecentFolderResolutionEntry
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