using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebBackgrounder;

namespace NuGetGallery
{
    public class LuceneIndexerJob : Job
    {
        public LuceneIndexerJob(TimeSpan interval, TimeSpan timeout)
            : base(typeof(LuceneIndexerJob).Name, interval, timeout)
        {

        }

        public override Task Execute()
        {
            var packageService = DependencyResolver.Current.GetService<IPackageService>();
            var luceneService = new LuceneService(packageService);
            return new Task(luceneService.UpdateIndex);
        }
    }
}
