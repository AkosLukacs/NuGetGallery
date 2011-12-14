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
        public IEnumerable<int> Search(string searchTerm)
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
                    var id = new TermQuery(new Term("Id", term));

                    var title = new TermQuery(new Term("Title", term));

                    var tags = new FuzzyQuery(new Term("Tags", term));

                    var desc = new FuzzyQuery(new Term("Description", term));
                    desc.SetBoost(0.8f);
                    var author = new TermQuery(new Term("Author", term));

                    booleanQuery.Add(id.Combine(new Query[] { id, title, tags, desc, author }), BooleanClause.Occur.SHOULD);
                }
                var results = searcher.Search(booleanQuery, filter: null, n: 1000, sort: Sort.RELEVANCE);
                return results.scoreDocs.Select(c => Int32.Parse(searcher.Doc(c.doc).Get("Key"), CultureInfo.InvariantCulture));
            }
        }
    }
}