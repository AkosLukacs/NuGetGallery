using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NuGetGallery
{
    public class LuceneSearchService : ISearchService
    {
        public IQueryable<Package> Search(IQueryable<Package> packages, string searchTerm)
        {
            if (String.IsNullOrEmpty(searchTerm))
            {
                return packages;
            }
            var keys = SearchCore(searchTerm);
            return SearchByKeys(packages, keys);
        }

        public IQueryable<Package> SearchWithRelevance(IQueryable<Package> packages, string searchTerm)
        {
            if (String.IsNullOrEmpty(searchTerm))
            {
                return packages;
            }

            var keys = SearchCore(searchTerm);
            if (!keys.Any())
            {
                return Enumerable.Empty<Package>().AsQueryable();
            }
            var results = SearchByKeys(packages, keys);

            var dict = results.ToDictionary(p => p.Key, p => p);
            return keys.Select(key => LookupPackage(dict, key))
                       .Where(p => p != null)
                       .AsQueryable();
        }

        private static Package LookupPackage(Dictionary<int, Package> dict, int key)
        {
            Package package; 
            dict.TryGetValue(key, out package); 
            return package;
        }

        private static IQueryable<Package> SearchByKeys(IQueryable<Package> packages, IEnumerable<int> keys)
        {
            return packages.Where(p => keys.Contains(p.Key));
        }

        private static IEnumerable<int> SearchCore(string searchTerm)
        {
            if (!Directory.Exists(LuceneCommon.IndexPath))
            {
                return Enumerable.Empty<int>();
            }

            using (var directory = new LuceneFileSystem(LuceneCommon.IndexPath))
            {
                var searcher = new IndexSearcher(directory, readOnly: true);

                var booleanQuery = new BooleanQuery();
                foreach (var term in searchTerm.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    booleanQuery.Add(new TermQuery(new Term("Id", term)), BooleanClause.Occur.SHOULD);
                    booleanQuery.Add(new TermQuery(new Term("Title", term)), BooleanClause.Occur.SHOULD);
                    booleanQuery.Add(new TermQuery(new Term("Author", term)), BooleanClause.Occur.SHOULD);
                    booleanQuery.Add(new FuzzyQuery(new Term("Tags", term), 0.8f, 2), BooleanClause.Occur.SHOULD);

                    var desc = new FuzzyQuery(new Term("Description", term), 0.8f, 3);
                    desc.SetBoost(0.7f);
                    booleanQuery.Add(desc, BooleanClause.Occur.SHOULD);
                }
                var results = searcher.Search(booleanQuery, filter: null, n: 1000, sort: Sort.RELEVANCE);
                return results.scoreDocs.Select(c => Int32.Parse(searcher.Doc(c.doc).Get("Key"), CultureInfo.InvariantCulture));
            }
        }
    }
}