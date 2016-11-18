using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void DoTheThing(object sender, RoutedEventArgs e)
        {
            var layout = XDocument.Parse(InputBox.Text);
            var nodes = GetNodesWithId(layout.Root).Select(element => new ElementEntry(element));
            
            var outputFields = new StringBuilder();
            var outputProperties = new StringBuilder();

            foreach (var elementEntry in nodes)
            {
                var field = FirstToLower(elementEntry.Name);
                outputFields.AppendLine($"private {elementEntry.Type} _{field};");
                outputProperties.AppendLine(
                    $"public {elementEntry.Type} {FirstToUpper(elementEntry.Name)} => _{field} ?? (_{field} = FindViewById<{elementEntry.Type}>(Resource.Id.{elementEntry.Name}));\n");
            }

            OutputBox.Text = outputFields + "\n" + outputProperties;

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
                var attr = rootElement.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id");
                if(attr != null)
                {
                    if(attr.Value.StartsWith("@+id/"))
                        yield return rootElement;
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
    }

    public class ElementEntry
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public ElementEntry(XElement source)
        {
            Type = source.Name.LocalName.Split('.').Last();
            Name = source.Attributes().First(attribute => attribute.Name.LocalName == "id").Value.Substring(5);
        }
    }
}
