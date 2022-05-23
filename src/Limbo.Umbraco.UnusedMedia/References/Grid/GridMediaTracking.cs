using System.Collections.Generic;
using System.Text.RegularExpressions;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Composing;
using UmbracoConstants = Umbraco.Core.Constants;
using Limbo.Umbraco.UnusedMedia.Constants;

namespace Limbo.Umbraco.UnusedMedia.References.Grid
{
    internal class GridMediaTracking : IDataValueReferenceFactory, IDataValueReference
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
                foreach (Match image in Regex.Matches(value.ToString(), RegexPatterns.MatchUmbracoMediaLink))
                {
                    var isValid = Udi.TryParse(image.Value, out var udi);
                    if (isValid)
                    {
                        references.Add(new UmbracoEntityReference(udi));
                    }
                }
                foreach (Match image in Regex.Matches(value.ToString(), RegexPatterns.MatchMediaPathPattern))
                {
                    AddReferenceFromMediaPath(references, image);
                }
            }
            return references;
        }
        private void AddReferenceFromMediaPath(List<UmbracoEntityReference> references, Match image)
        {
            var mediaId = Current.Services.MediaService.GetMediaByPath(image.Value).Key;
            var isVaild = Udi.TryParse($"umb://media/{mediaId.ToString().Replace("-", "")}", out var udi);
            if (isVaild)
            {
                references.Add(new UmbracoEntityReference(udi));
            }
        }

        public bool IsForEditor(IDataEditor dataEditor)
        {
            return dataEditor.Alias.InvariantEquals(UmbracoConstants.PropertyEditors.Aliases.Grid);
        }
    }
}