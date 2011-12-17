﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
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
            using (var directory = new LuceneFileSystem(LuceneCommon.IndexPath))
            {
                var analyzer = new StandardPackageAnalyzer();

                var indexCreatedTime = GetCreatedTime();
                if (indexCreatedTime != null && ((indexCreatedTime - DateTime.UtcNow) > TimeSpan.FromDays(2)))
                {
                    ClearIndex();
                }

                var dateTime = GetLastWriteTime();
                bool recreateIndex = dateTime == _minDateValue;
                using (var context = new EntitiesContext())
                {
                    string sql = @"Select p.[Key], pr.Id, p.Title, p.Description, p.Tags, p.FlattenedAuthors as Authors, pr.DownloadCount
                               from Packages p join PackageRegistrations pr on p.PackageRegistrationKey = pr.[Key]
                               where p.Listed = 1 
                               and (p.IsLatestStable = 1 or (p.IsLatest = 1 and not exists (Select 1 from Packages iP where iP.PackageRegistrationKey = p.PackageRegistrationKey and p.IsLatestStable = 1)))
                               and p.Published > @PublishedDate";
                    var packages = context.Database.SqlQuery<PackageIndexEntity>(sql, new SqlParameter("PublishedDate", dateTime))
                                                   .ToList();

                    if (packages.Any() || recreateIndex)
                    {
                        var indexWriter = new IndexWriter(directory, analyzer, create: recreateIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);
                        AddPackages(indexWriter, packages);
                        indexWriter.Close();
                    }
                }
            }
            UpdateLastWriteTime();
        }

        private static void AddPackages(IndexWriter indexWriter, List<PackageIndexEntity> packages)
        {
            var totalDownloadCount = packages.Sum(p => p.DownloadCount) + 1;
            foreach (var package in packages)
            {
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

                document.SetBoost((float)Math.Pow(2, (package.DownloadCount / totalDownloadCount)));

                indexWriter.UpdateDocument(new Term("Id", package.Id), document);
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

        private static DateTime GetLastWriteTime()
        {
            if (!File.Exists(LuceneCommon.IndexMetadataPath))
            {
                if (!Directory.Exists(LuceneCommon.IndexPath))
                {
                    Directory.CreateDirectory(LuceneCommon.IndexPath);
                }
                File.WriteAllBytes(LuceneCommon.IndexMetadataPath, new byte[0]);
                return _minDateValue;
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