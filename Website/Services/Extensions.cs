using System.Linq;
using Ninject;

namespace NuGetGallery
{
    public static class Extensions
    {
        //public static PackageSearchResults Search(this IQueryable<Package> source, ISearchService searchSvc, string searchTerm)
        //{
        //    var ids = searchSvc.Search(searchTerm);
        //    var results = source.Where(s => ids.Contains(s.Key));

        //    return new PackageSearchResults { Packages = results, RankedKeys = ids };
        //}


        //public static IQueryable<Package> SortByRelevance(this PackageSearchResults packageSearchResults)
        //{
        //    var packages = packageSearchResults.Packages;
        //    if (!packages.Any())
        //    {
        //        return Enumerable.Empty<Package>()
        //                         .AsQueryable();
        //    }

        //    var dict = packages.ToDictionary(p => p.Key, p => p);
        //    return packageSearchResults.RankedKeys.Select(k => dict[k])
        //                                          .AsQueryable();
        //}
    }
}