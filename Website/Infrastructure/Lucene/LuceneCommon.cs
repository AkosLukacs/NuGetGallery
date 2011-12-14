using System.IO;
using System.Web;
using Lucene.Net.Store;

namespace NuGetGallery
{
    internal static class LuceneCommon
    {
        internal static readonly string IndexPath = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "Lucene");
        internal static readonly string IndexMetadataPath = Path.Combine(IndexPath, "index.metadata");
    }
}