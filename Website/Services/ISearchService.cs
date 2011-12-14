using System.Collections.Generic;

namespace NuGetGallery
{
    public interface ISearchService
    {
        IEnumerable<int> Search(string term);
    }
}