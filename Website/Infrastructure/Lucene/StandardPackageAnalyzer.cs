using System;
using System.Collections;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using LuceneVersion = Lucene.Net.Util.Version;

namespace NuGetGallery
{
    public class StandardPackageAnalyzer : StandardAnalyzer
    {
        private static readonly LuceneVersion AnalyzerVersion = LuceneVersion.LUCENE_29;
        public StandardPackageAnalyzer()
            : base(AnalyzerVersion)
        {
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            if (fieldName.Equals("Description", StringComparison.OrdinalIgnoreCase))
            {
                var tokenizer = new StandardTokenizer(AnalyzerVersion, reader);
                var stopFilter = new StopFilter(enablePositionIncrements: false, in_Renamed: new StandardFilter(tokenizer), stopWords: StopAnalyzer.ENGLISH_STOP_WORDS_SET);
                return new PorterStemFilter(new LowerCaseFilter(stopFilter));
            }
            if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                var tokenizer = new StandardTokenizer(AnalyzerVersion, reader);
                var hashTable = new Hashtable();
                hashTable.Add(".", ".");
                hashTable.Add("-", "-");
                var stopFilter = new StopFilter(enablePositionIncrements: false, in_Renamed: new StandardFilter(tokenizer), stopWords: hashTable);
                return new LowerCaseFilter(stopFilter);
            }
            return base.TokenStream(fieldName, reader);
        }
    }
}