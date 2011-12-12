using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using WebBackgrounder;

namespace NuGetGallery
{
    public class LuceneService : IIndexingService
    {
        private static readonly string _indexPath = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "Lucene");
        private static readonly string _indexMetadataPath = Path.Combine(_indexPath, "index.metadata");
        private readonly IPackageService packageSvc;

        public LuceneService(IPackageService packageSvc)
        {
            this.packageSvc = packageSvc;
        }

        public void UpdateIndex()
        {
            var directory = GetIndexDirectory();

            var analyzer = new StandardPackageAnalyzer();
            var dateTime = GetLastWriteTime();
            IEnumerable<Package> packages = packageSvc.GetLatestPackageVersions(allowPrerelease: true)
                                                      .Where(p => p.Published > dateTime)
                                                      .ToList();
            if (packages.Any())
            {
                var indexWriter = new IndexWriter(directory, analyzer, create: dateTime == DateTime.MinValue, mfl: IndexWriter.MaxFieldLength.UNLIMITED);
                AddPackages(indexWriter, packages);
                indexWriter.Close();
            }

            UpdateLastWriteTime();
        }

        public IEnumerable<int> Search(string searchTerm)
        {
            var directory = GetIndexDirectory();
            var searcher = new IndexSearcher(directory, readOnly: true);

            var booleanQuery = new BooleanQuery();
            foreach (var term in searchTerm.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var id = new TermQuery(new Term("Id", term));
                id.SetBoost(20.0f);

                var title = new TermQuery(new Term("Title", term));
                title.SetBoost(7.5f);

                var tags = new FuzzyQuery(new Term("Tags", term));
                tags.SetBoost(5.0f);

                var desc = new FuzzyQuery(new Term("Description", term));
                var author = new TermQuery(new Term("Author", term));

                booleanQuery.Add(id.Combine(new Query[] { id, title, tags, desc, author }), BooleanClause.Occur.MUST);
            }
            var results = searcher.Search(booleanQuery, filter: null, n: 1000, sort: Sort.RELEVANCE);
            return results.scoreDocs.Select(c => Int32.Parse(searcher.Doc(c.doc).Get("Key"), CultureInfo.InvariantCulture));
        }

        private static void AddPackages(IndexWriter indexWriter, IEnumerable<Package> packages)
        {
            foreach (var package in packages)
            {
                // If there's an older entry for this package, remove it.
                var query = new TermQuery(new Term("Id", package.PackageRegistration.Id));
                indexWriter.DeleteDocuments(query);

                var document = new Document();

                document.Add(new Field("Key", package.Key.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
                document.Add(new Field("Id", package.PackageRegistration.Id, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS));
                document.Add(new Field("Description", package.Description, Field.Store.NO, Field.Index.ANALYZED));

                if (!String.IsNullOrEmpty(package.Title))
                {
                    document.Add(new Field("Title", package.Title, Field.Store.NO, Field.Index.ANALYZED));
                }

                foreach (var tag in (package.Tags ?? String.Empty).Split())
                {
                    document.Add(new Field("Tags", tag, Field.Store.NO, Field.Index.ANALYZED));
                }

                foreach (var author in package.Authors)
                {
                    document.Add(new Field("Author", author.Name, Field.Store.NO, Field.Index.ANALYZED));
                }
                indexWriter.AddDocument(document);
            }
        }

        private static Lucene.Net.Store.Directory GetIndexDirectory()
        {
            return new SimpleFSDirectory(new DirectoryInfo(_indexPath));
        }

        private static DateTime GetLastWriteTime()
        {
            if (!File.Exists(_indexMetadataPath))
            {
                File.WriteAllBytes(_indexMetadataPath, new byte[0]);
                return DateTime.MinValue;
            }
            return File.GetLastWriteTimeUtc(_indexMetadataPath);
        }

        private static void UpdateLastWriteTime()
        {
            File.SetLastWriteTimeUtc(_indexMetadataPath, DateTime.UtcNow);
        }
    }
}