using System.Collections.Generic;

namespace NuGetGallery
{
    public interface IIndexingService
    {
        void UpdateIndex();

        IEnumerable<int> Search(string term);
    }
}