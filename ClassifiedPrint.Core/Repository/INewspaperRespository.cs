using ClassifiedPrint.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClassifiedPrint.Core.Repository
{
    public interface INewspaperRespository:IRepository<Newspaper>
    {
        void ExecuteBulkOperation(Command cmd);
        Task ExecuteBulkOperationAsync(Command command);
    }
}
