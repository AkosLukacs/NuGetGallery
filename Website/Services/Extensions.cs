using System.Linq;
using Ninject;

namespace NuGetGallery
{
    public static class Extensions
    {
        public static PackageSearchResults Search(this IQueryable<Package> source, string searchTerm)
        {
            var service = Container.Kernel.Get<IIndexingService>();
            var ids = service.Search(searchTerm);
            var results = source.Where(s => ids.Contains(s.Key));

            return new PackageSearchResults { Packages = results, RankedKeys = ids };
        }


        public static IQueryable<Package> SortByRelevance(this PackageSearchResults packageSearchResults)
        {
            var dict = packageSearchResults.Packages.ToDictionary(p => p.Key, p => p);

            return packageSearchResults.RankedKeys.Select(k => dict[k])
                                                  .AsQueryable();
        }
    }
}