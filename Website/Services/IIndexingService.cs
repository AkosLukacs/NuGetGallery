using System.Collections.Generic;

namespace NuGetGallery
{
    public interface IIndexingService
    {
        void CreateIndex();

        void UpdateIndex(Package package);

        IEnumerable<int> Search(string term);
    }
}