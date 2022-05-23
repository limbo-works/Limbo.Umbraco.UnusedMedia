using System.Collections.Generic;
using System.Text.RegularExpressions;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core;
using UmbracoConstants = Umbraco.Core.Constants;
using Limbo.Umbraco.UnusedMedia.Constants;
using Umbraco.Web.Composing;

namespace Limbo.Umbraco.UnusedMedia.References.Grid
{
    internal class GridContentTracking : IDataValueReferenceFactory, IDataValueReference
    {
        public IDataValueReference GetDataValueReference()
        {
            return this;
        }

        public IEnumerable<UmbracoEntityReference> GetReferences(object value)
        {
            var references = new List<UmbracoEntityReference>();
            if (value != null)
            {
                foreach (Match content in Regex.Matches(value.ToString(), RegexPatterns.MatchUmbracoContentLink))
                {
                    var isValid = Udi.TryParse(content.Value, out var udi);
                    if (isValid)
                    {
                        references.Add(new UmbracoEntityReference(udi));
                    }
                }
            }
            return references;
        }

        public bool IsForEditor(IDataEditor dataEditor)
        {
            return dataEditor.Alias.InvariantEquals(UmbracoConstants.PropertyEditors.Aliases.Grid);
        }
    }
}