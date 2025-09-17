using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Operations
{
    public class ChainReorderService
    {
        private readonly string[] _projectOrder = {
            "framework", "repository", "olap", "modeling", "depmservice", "consolidation",
            "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration",
            "content", "deployment", "tests"
        };

        public bool ReorderChain(ChainModel chain)
        {
            if (chain.Sections == null || chain.Sections.Count == 0)
                return false;

            var originalOrder = chain.Sections.Select(s => s.Name).ToList();
            var orderedSections = new List<Section>();

            // Add sections in template order
            foreach (var projectName in _projectOrder)
            {
                var section = chain.Sections.FirstOrDefault(s => s.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (section != null)
                    orderedSections.Add(section);
            }

            // Add any extra sections not in template
            var extraSections = chain.Sections.Where(s => !_projectOrder.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            orderedSections.AddRange(extraSections);

            chain.Sections = orderedSections;

            // Check if order changed
            var newOrder = chain.Sections.Select(s => s.Name).ToList();
            return !originalOrder.SequenceEqual(newOrder);
        }
    }
}