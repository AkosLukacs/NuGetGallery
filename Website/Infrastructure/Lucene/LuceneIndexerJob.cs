﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using WebBackgrounder;

namespace NuGetGallery
{
    public class LuceneIndexingJob : Job
    {
        private static readonly DateTime _minDateValue = new DateTime(1900, 01, 01);
        public LuceneIndexingJob(TimeSpan interval, TimeSpan timeout)
            : base(typeof(LuceneIndexingJob).Name, interval, timeout)
        {
            UpdateIndex();
        }

        public override Task Execute()
        {
            return new Task(UpdateIndex);
        }

        private void UpdateIndex()
        {
            DateTime? lastWriteTime = GetLastWriteTime();
            bool creatingIndex = true;// lastWriteTime == null;
            var analyzer = new StandardPackageAnalyzer();
            using (var context = new EntitiesContext())
            {
                var packages = GetPackages(context, lastWriteTime);
                if (packages.Any())
                {
                    using (var directory = new LuceneFileSystem(LuceneCommon.IndexPath))
                    {
                        var indexWriter = new IndexWriter(directory, analyzer, create: creatingIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);
                        AddPackages(indexWriter, packages);
                        indexWriter.Close();
                    }
                }
            }
            UpdateLastWriteTime();
        }

        private static List<PackageIndexEntity> GetPackages(EntitiesContext context, DateTime? dateTime)
        {
            if (dateTime == null)
            {
                // If we're creating the index for the first time, fetch the new packages.
                string sql = @"Select p.[Key], pr.Id, p.Title, p.Description, p.Tags, p.FlattenedAuthors as Authors, p.[Key] as LatestKey
                         from Packages p join PackageRegistrations pr on p.PackageRegistrationKey = pr.[Key]
                         where p.IsLatestStable = 1 or (p.IsLatest = 1 and Not exists (Select 1 from Packages iP where iP.PackageRegistrationKey = p.PackageRegistrationKey and p.IsLatestStable = 1))";
                return context.Database.SqlQuery<PackageIndexEntity>(sql).ToList();
            }
            else
            {
                string sql = @"Select p.[Key], pr.Id, p.Title, p.Description, p.Tags, p.FlattenedAuthors as Authors, 
                                   LatestKey = CASE When p.IsLatest = 1 then p.[Key] Else (Select pLatest.[Key] from Packages pLatest where pLatest.PackageRegistrationKey = pr.[Key] and pLatest.IsLatest = 1) End
                                   from Packages p join PackageRegistrations pr on p.PackageRegistrationKey = pr.[Key]
                                   where p.LastUpdated > @UpdatedDate";
                return context.Database.SqlQuery<PackageIndexEntity>(sql, new SqlParameter("UpdatedDate", dateTime.Value)).ToList();
            }
        }

        private static void AddPackages(IndexWriter indexWriter, List<PackageIndexEntity> packages)
        {
            foreach (var package in packages)
            {
                if (package.Key != package.LatestKey)
                {
                    indexWriter.DeleteDocuments(new TermQuery(new Term("Key", package.Key.ToString(CultureInfo.InvariantCulture))));
                    continue;
                }

                // If there's an older entry for this package, remove it.
                var document = new Document();

                document.Add(new Field("Key", package.Key.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
                document.Add(new Field("Id-Exact", package.Id, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS));
                document.Add(new Field("Id", package.Id, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS));
                document.Add(new Field("Description", package.Description, Field.Store.NO, Field.Index.ANALYZED));

                if (!String.IsNullOrEmpty(package.Title))
                {
                    document.Add(new Field("Title", package.Title, Field.Store.NO, Field.Index.ANALYZED));
                }

                foreach (var tag in (package.Tags ?? String.Empty).Split())
                {
                    document.Add(new Field("Tags", tag, Field.Store.NO, Field.Index.ANALYZED));
                }

                foreach (var author in package.Authors.Split())
                {
                    document.Add(new Field("Author", author, Field.Store.NO, Field.Index.ANALYZED));
                }

                indexWriter.AddDocument(document);
            }
        }

        private static DateTime? GetCreatedTime()
        {
            if (File.Exists(LuceneCommon.IndexMetadataPath))
            {
                return File.GetCreationTimeUtc(LuceneCommon.IndexMetadataPath);
            }
            return null;
        }

        private static DateTime? GetLastWriteTime()
        {
            if (!File.Exists(LuceneCommon.IndexMetadataPath))
            {
                if (!Directory.Exists(LuceneCommon.IndexPath))
                {
                    Directory.CreateDirectory(LuceneCommon.IndexPath);
                }
                File.WriteAllBytes(LuceneCommon.IndexMetadataPath, new byte[0]);
                return null;
            }
            return File.GetLastWriteTimeUtc(LuceneCommon.IndexMetadataPath);
        }

        private static void UpdateLastWriteTime()
        {
            File.SetLastWriteTimeUtc(LuceneCommon.IndexMetadataPath, DateTime.UtcNow);
        }

        private static void ClearIndex()
        {
            Directory.Delete(LuceneCommon.IndexPath, recursive: true);
        }
    }
}